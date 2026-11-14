using LiteBus.Commands.Abstractions;
using LiteBus.Messaging.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.ActivityLog;
using Moonglade.BackgroundServices;
using Moonglade.Configuration;
using Moonglade.ImageStorage;
using Moonglade.Web.Controllers;
using Moonglade.Web.Services;
using Moonglade.Web.Tests.TestSupport;
using Moq;
using System.Net;
using System.Security.Claims;
using System.Text;

namespace Moonglade.Web.Tests.Controllers;

public class ImageControllerTests : IAsyncDisposable
{
    private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), "moonglade-web-image-tests", Guid.NewGuid().ToString("N"));
    private readonly FileSystemImageStorage _imageStorage;
    private readonly DatedGuidFileNameGenerator _fileNameGenerator = new();
    private readonly RecordingCommandMediator _commandMediator = new();
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
    private CannonService? _cannonService;
    private ServiceProvider? _serviceProvider;

    private string PrimaryDirectory => Path.Combine(_tempDirectory, "primary");
    private string OriginalDirectory => Path.Combine(_tempDirectory, "original");

    public ImageControllerTests()
    {
        var configuration = FileSystemImageStorage.ResolveImageStoragePaths(PrimaryDirectory, OriginalDirectory);
        _imageStorage = new FileSystemImageStorage(configuration, Mock.Of<ILogger<FileSystemImageStorage>>());
    }

    [Fact]
    public void Image_Post_UsesGlobalAntiforgeryValidation()
    {
        var method = typeof(ImageController).GetMethods()
            .Single(m =>
                m.Name == nameof(ImageController.Image) &&
                m.GetParameters().Any(p => p.ParameterType == typeof(IFormFile)));

        Assert.Empty(method.GetCustomAttributes(typeof(IgnoreAntiforgeryTokenAttribute), inherit: false));
    }

    [Fact]
    public async Task Image_Get_WhenFilenameContainsInvalidCharacters_ReturnsBadRequest()
    {
        var controller = CreateController();

        var result = await controller.Image("bad" + Path.GetInvalidFileNameChars()[0] + ".png", TestContext.Current.CancellationToken);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("invalid filename", badRequestResult.Value);
    }

    [Fact]
    public async Task Image_Post_WhenOriginalIsKept_PublicEndpointCannotReadOriginalStorage()
    {
        var imageBytes = ValidSvgBytes();
        var primaryName = _fileNameGenerator.GetFileName("photo.svg");
        var originalName = _fileNameGenerator.GetFileName("photo.svg", "origin");
        var controller = CreateController(
            new BlogConfig(new BlogSettingsSnapshot
            {
                ImageSettings = new ImageSettings
                {
                    IsWatermarkEnabled = true,
                    KeepOriginImage = true,
                    WatermarkFontSize = 20,
                    WatermarkText = "Moonglade"
                }
            }));

        var uploadResult = await controller.Image(CreateFormFile("photo.svg", imageBytes));
        await StopCannonServiceAsync();

        var okResult = Assert.IsType<OkObjectResult>(uploadResult);
        Assert.Equal($"/image/{primaryName}", okResult.Value!.GetType().GetProperty("Location")!.GetValue(okResult.Value));
        Assert.Equal(imageBytes, await File.ReadAllBytesAsync(Path.Combine(PrimaryDirectory, primaryName), TestContext.Current.CancellationToken));
        Assert.Equal(imageBytes, await File.ReadAllBytesAsync(Path.Combine(OriginalDirectory, originalName), TestContext.Current.CancellationToken));
        Assert.False(File.Exists(Path.Combine(PrimaryDirectory, originalName)));

        var originalResult = await controller.Image(originalName, TestContext.Current.CancellationToken);
        Assert.IsType<NotFoundResult>(originalResult);
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("HEAD")]
    public async Task Image_WhenCdnRedirectIsEnabled_ReturnsPermanentRedirectWithoutReadingStorage(string httpMethod)
    {
        var controller = CreateController(new BlogConfig(new BlogSettingsSnapshot
        {
            ImageSettings = new ImageSettings
            {
                EnableCDNRedirect = true,
                CDNEndpoint = "https://cdn.example.com/images"
            }
        }));
        controller.Request.Method = httpMethod;

        var result = await controller.Image("photo.png", TestContext.Current.CancellationToken);

        var redirectResult = Assert.IsType<RedirectResult>(result);
        Assert.True(redirectResult.Permanent);
        Assert.Equal("https://cdn.example.com/images/photo.png", redirectResult.Url);
    }

    [Fact]
    public async Task Image_Get_WhenImageDoesNotExist_ReturnsNotFound()
    {
        var controller = CreateController();

        var result = await controller.Image("missing.png", TestContext.Current.CancellationToken);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Image_Get_WhenImageExists_ReturnsFileStreamResultWithCacheHeaders()
    {
        var bytes = new byte[] { 1, 2, 3 };
        var lastModified = new DateTimeOffset(2026, 2, 23, 10, 30, 0, TimeSpan.Zero);
        await _imageStorage.InsertAsync("photo.png", bytes);
        File.SetLastWriteTimeUtc(Path.Combine(PrimaryDirectory, "photo.png"), lastModified.UtcDateTime);
        var expectedEntityTag = $"\"{lastModified.Ticks:x}-{bytes.Length:x}\"";
        var controller = CreateController();

        var result = await controller.Image("photo.png", TestContext.Current.CancellationToken);

        var fileResult = Assert.IsType<FileStreamResult>(result);
        Assert.Equal("image/png", fileResult.ContentType);
        Assert.True(fileResult.EnableRangeProcessing);
        Assert.Equal(lastModified, fileResult.LastModified);
        Assert.Equal(expectedEntityTag, fileResult.EntityTag!.ToString());
        Assert.Equal(expectedEntityTag, controller.Response.Headers.ETag.ToString());
        Assert.Equal(lastModified.ToString("R"), controller.Response.Headers.LastModified.ToString());
        Assert.Equal("public, max-age=300", controller.HttpContext.Response.Headers.CacheControl.ToString());

        using var reader = new MemoryStream();
        await using var imageStream = fileResult.FileStream;
        await imageStream.CopyToAsync(reader, TestContext.Current.CancellationToken);
        Assert.Equal(bytes, reader.ToArray());
    }

    [Fact]
    public async Task Image_Get_WhenCalledTwice_UsesMetadataCacheAndOpensFreshStreams()
    {
        var bytes = new byte[] { 4, 5, 6 };
        await _imageStorage.InsertAsync("photo.webp", bytes);
        var controller = CreateController();

        var firstResult = Assert.IsType<FileStreamResult>(await controller.Image("photo.webp", TestContext.Current.CancellationToken));
        await firstResult.FileStream.DisposeAsync();
        byte[] updatedBytes = [7, 8, 9, 10];
        await _imageStorage.InsertAsync("photo.webp", updatedBytes);
        var secondResult = Assert.IsType<FileStreamResult>(await controller.Image("photo.webp", TestContext.Current.CancellationToken));

        Assert.Equal(firstResult.EntityTag, secondResult.EntityTag);
        Assert.NotEqual((await _imageStorage.GetInfoAsync("photo.webp")).EntityTag, secondResult.EntityTag!.ToString());
        Assert.NotSame(firstResult.FileStream, secondResult.FileStream);
        await using var imageStream = secondResult.FileStream;
        using var reader = new MemoryStream();
        await imageStream.CopyToAsync(reader, TestContext.Current.CancellationToken);
        Assert.Equal(updatedBytes, reader.ToArray());
    }

    [Fact]
    public async Task Image_Get_WhenIfNoneMatchMatches_ReturnsNotModifiedWithoutOpeningStream()
    {
        await _imageStorage.InsertAsync("photo.png", [1, 2, 3]);
        var controller = CreateController();
        var initialResult = Assert.IsType<FileStreamResult>(await controller.Image("photo.png", TestContext.Current.CancellationToken));
        await initialResult.FileStream.DisposeAsync();
        File.Delete(Path.Combine(PrimaryDirectory, "photo.png"));
        var entityTag = initialResult.EntityTag!.ToString();
        controller.HttpContext.Request.Headers.IfNoneMatch = entityTag;

        var result = await controller.Image("photo.png", TestContext.Current.CancellationToken);

        var statusCodeResult = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(StatusCodes.Status304NotModified, statusCodeResult.StatusCode);
        Assert.Equal(entityTag, controller.HttpContext.Response.Headers.ETag.ToString());
        Assert.Equal("public, max-age=300", controller.HttpContext.Response.Headers.CacheControl.ToString());
    }

    [Fact]
    public async Task Image_Get_WhenIfModifiedSinceIsCurrent_ReturnsNotModifiedWithoutOpeningStream()
    {
        var lastModified = new DateTimeOffset(2026, 2, 23, 10, 30, 0, TimeSpan.Zero);
        await _imageStorage.InsertAsync("photo.png", [1, 2, 3]);
        File.SetLastWriteTimeUtc(Path.Combine(PrimaryDirectory, "photo.png"), lastModified.UtcDateTime);
        var controller = CreateController();
        var initialResult = Assert.IsType<FileStreamResult>(await controller.Image("photo.png", TestContext.Current.CancellationToken));
        await initialResult.FileStream.DisposeAsync();
        File.Delete(Path.Combine(PrimaryDirectory, "photo.png"));
        controller.HttpContext.Request.Headers.IfModifiedSince = lastModified.ToString("R");

        var result = await controller.Image("photo.png", TestContext.Current.CancellationToken);

        var statusCodeResult = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(StatusCodes.Status304NotModified, statusCodeResult.StatusCode);
        Assert.Equal(lastModified.ToString("R"), controller.HttpContext.Response.Headers.LastModified.ToString());
    }

    [Fact]
    public async Task Image_Post_WhenFileIsEmpty_ReturnsBadRequest()
    {
        var controller = CreateController();
        var file = CreateFormFile("empty.png", []);

        var result = await controller.Image(file);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Image file is empty.", badRequestResult.Value);
        AssertStorageEmpty();
        Assert.Empty(_commandMediator.Commands);
    }

    [Fact]
    public async Task Image_Post_WhenFileExceedsLimit_ReturnsBadRequest()
    {
        var controller = CreateController();
        var file = CreateFormFile("large.png", new byte[(5 * 1024 * 1024) + 1]);

        var result = await controller.Image(file);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Image file size cannot exceed 5 MB.", badRequestResult.Value);
        AssertStorageEmpty();
        Assert.Empty(_commandMediator.Commands);
    }

    [Fact]
    public async Task Image_Post_WhenExtensionIsNotAllowed_ReturnsBadRequest()
    {
        var controller = CreateController();
        var file = CreateFormFile("photo.txt", [1, 2, 3]);

        var result = await controller.Image(file);

        Assert.IsType<BadRequestResult>(result);
        AssertStorageEmpty();
        Assert.Empty(_commandMediator.Commands);
    }

    [Fact]
    public async Task Image_Post_WhenContentDoesNotMatchExtension_ReturnsBadRequest()
    {
        var controller = CreateController();
        var file = CreateFormFile("photo.png", Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\"></svg>"));

        var result = await controller.Image(file);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Image content does not match file extension.", badRequestResult.Value);
        AssertStorageEmpty();
        Assert.Empty(_commandMediator.Commands);
    }

    [Fact]
    public async Task Image_Post_WhenValidFileAndWatermarkDisabled_SavesImageAndWritesActivityLog()
    {
        var imageBytes = ValidPngBytes();
        var primaryName = _fileNameGenerator.GetFileName("photo.png");
        var controller = CreateController(
            new BlogConfig(new BlogSettingsSnapshot { ImageSettings = new ImageSettings { IsWatermarkEnabled = false } }),
            username: "admin",
            remoteIpAddress: IPAddress.Parse("127.0.0.1"),
            userAgent: "unit-test-agent");
        var file = CreateFormFile("C:\\temp\\photo.png", imageBytes);

        var result = await controller.Image(file);
        await StopCannonServiceAsync();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal($"/image/{primaryName}", okResult.Value!.GetType().GetProperty("Location")!.GetValue(okResult.Value));
        Assert.Equal($"/image/{primaryName}", okResult.Value.GetType().GetProperty("Filename")!.GetValue(okResult.Value));
        Assert.Equal(imageBytes, await File.ReadAllBytesAsync(Path.Combine(PrimaryDirectory, primaryName), TestContext.Current.CancellationToken));
        Assert.Empty(Directory.GetFiles(OriginalDirectory));

        var activityCommand = _commandMediator.Single<CreateActivityLogCommand>();
        Assert.Equal(ActivityEvents.ImageUploaded, activityCommand.EventKey);
        Assert.Equal("admin", activityCommand.ActorDisplayName);
        Assert.Equal(primaryName, activityCommand.TargetDisplayName);
        Assert.Equal(primaryName, activityCommand.TargetId);
        Assert.Equal("127.0.0.1", activityCommand.IpAddress);
        Assert.Equal("unit-test-agent", activityCommand.UserAgent);
        Assert.NotNull(activityCommand.Details);
        Assert.Equal(primaryName, ActivityLogMetaDataAssert.Value<string>(activityCommand, "FileName"));
        Assert.Equal(file.Length, ActivityLogMetaDataAssert.Value<long>(activityCommand, "FileSize"));
        Assert.False(ActivityLogMetaDataAssert.Value<bool>(activityCommand, "SkipWatermark"));
    }

    [Fact]
    public async Task Image_Post_WhenSkipWatermarkIsTrue_DoesNotStoreOriginalImage()
    {
        var controller = CreateController(new BlogConfig(new BlogSettingsSnapshot { ImageSettings = ImageSettings.DefaultValue }));
        var file = CreateFormFile("photo.png", ValidPngBytes());

        await controller.Image(file, skipWatermark: true);
        await StopCannonServiceAsync();

        Assert.Empty(Directory.GetFiles(OriginalDirectory));
        Assert.True(ActivityLogMetaDataAssert.Value<bool>(_commandMediator.Single<CreateActivityLogCommand>(), "SkipWatermark"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Image_Post_WhenWatermarkEnabledAndKeepOriginalEnabled_StoresOriginalImageInBackgroundQueue(bool publishDuringUpload)
    {
        var imageBytes = ValidSvgBytes();
        var originalName = _fileNameGenerator.GetFileName("photo.svg", "origin");
        var config = new BlogConfig(new BlogSettingsSnapshot
        {
            ImageSettings = new ImageSettings
            {
                IsWatermarkEnabled = true,
                KeepOriginImage = true,
                WatermarkFontSize = 20,
                WatermarkText = "Moonglade"
            }
        });
        var file = new Mock<IFormFile>();
        file.SetupGet(upload => upload.FileName).Returns("photo.svg");
        file.SetupGet(upload => upload.Length).Returns(imageBytes.Length);
        file.Setup(upload => upload.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Returns(async (Stream stream, CancellationToken cancellationToken) =>
            {
                await stream.WriteAsync(imageBytes, cancellationToken);
                if (publishDuringUpload) config.Publish(config.Capture() with { ImageSettings = new ImageSettings { IsWatermarkEnabled = false } });
            });
        var controller = CreateController(config);

        await controller.Image(file.Object);
        await StopCannonServiceAsync();

        Assert.Equal(!publishDuringUpload, config.Capture().ImageSettings.IsWatermarkEnabled);
        Assert.Equal(imageBytes, await File.ReadAllBytesAsync(Path.Combine(OriginalDirectory, originalName), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Image_Post_WhenSvgContainsActiveContent_SavesSanitizedSvg()
    {
        var primaryName = _fileNameGenerator.GetFileName("payload.svg");
        var controller = CreateController(new BlogConfig(new BlogSettingsSnapshot
        {
            ImageSettings = new ImageSettings { IsWatermarkEnabled = false }
        }));
        var file = CreateFormFile("payload.svg", MaliciousSvgBytes());

        var result = await controller.Image(file);

        Assert.IsType<OkObjectResult>(result);
        var savedBytes = await File.ReadAllBytesAsync(Path.Combine(PrimaryDirectory, primaryName), TestContext.Current.CancellationToken);
        var sanitizedSvg = Encoding.UTF8.GetString(savedBytes);
        Assert.Contains("<svg", sanitizedSvg, StringComparison.Ordinal);
        Assert.Contains("<rect", sanitizedSvg, StringComparison.Ordinal);
        Assert.DoesNotContain("<script", sanitizedSvg, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("foreignObject", sanitizedSvg, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onclick", sanitizedSvg, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("href=", sanitizedSvg, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("javascript:", sanitizedSvg, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Image_Post_WhenSvgContainsDoctype_ReturnsBadRequest()
    {
        var controller = CreateController();
        var file = CreateFormFile("payload.svg", Encoding.UTF8.GetBytes("""
            <!DOCTYPE svg [
              <!ENTITY xxe SYSTEM "file:///etc/passwd">
            ]>
            <svg xmlns="http://www.w3.org/2000/svg">&xxe;</svg>
            """));

        var result = await controller.Image(file);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid SVG data.", badRequestResult.Value);
        AssertStorageEmpty();
        Assert.Empty(_commandMediator.Commands);
    }

    private ImageController CreateController(
        BlogConfig? blogConfig = null,
        string? username = null,
        IPAddress? remoteIpAddress = null,
        string? userAgent = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton(_imageStorage);
        _serviceProvider = services.BuildServiceProvider();
        _cannonService = new CannonService(Mock.Of<ILogger<CannonService>>(), _serviceProvider.GetRequiredService<IServiceScopeFactory>());
        _cannonService.StartAsync(TestContext.Current.CancellationToken);

        var controller = new ImageController(
            _imageStorage,
            Mock.Of<ILogger<ImageController>>(),
            blogConfig ?? new BlogConfig(new BlogSettingsSnapshot { ImageSettings = new ImageSettings() }),
            _cache,
            _fileNameGenerator,
            new ImageUploadValidator(),
            Options.Create(new ImageStorageSettings { CacheMinutes = 5 }),
            _cannonService,
            _commandMediator);

        var httpContext = new DefaultHttpContext();

        if (!string.IsNullOrWhiteSpace(username))
        {
            httpContext.User = new ClaimsPrincipal(
                new ClaimsIdentity([new Claim(ClaimTypes.Name, username)], "TestAuth"));
        }

        if (remoteIpAddress is not null)
        {
            httpContext.Connection.RemoteIpAddress = remoteIpAddress;
        }

        if (!string.IsNullOrWhiteSpace(userAgent))
        {
            httpContext.Request.Headers.UserAgent = userAgent;
        }

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        return controller;
    }

    private async Task StopCannonServiceAsync()
    {
        if (_cannonService is null) return;

        var drained = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _cannonService.FireAsync<FileSystemImageStorage>(_ =>
        {
            drained.SetResult();
            return Task.CompletedTask;
        });
        await drained.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        await _cannonService.StopAsync(TestContext.Current.CancellationToken);
        _cannonService.Dispose();
        _cannonService = null;
    }

    private void AssertStorageEmpty()
    {
        Assert.Empty(Directory.GetFiles(PrimaryDirectory));
        Assert.Empty(Directory.GetFiles(OriginalDirectory));
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await StopCannonServiceAsync();
        }
        finally
        {
            _serviceProvider?.Dispose();
            _cache.Dispose();
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    private static FormFile CreateFormFile(string fileName, byte[] bytes)
    {
        return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", fileName);
    }

    private static byte[] ValidPngBytes()
    {
        const string base64Png = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+/p9sAAAAASUVORK5CYII=";
        return Convert.FromBase64String(base64Png);
    }

    private static byte[] ValidSvgBytes() =>
        Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 1 1\"><rect width=\"1\" height=\"1\" fill=\"red\" /></svg>");

    private static byte[] MaliciousSvgBytes() =>
        Encoding.UTF8.GetBytes("""
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1 1">
              <script>alert(1)</script>
              <foreignObject><body>bad</body></foreignObject>
              <a href="java&#x09;script:alert(1)"><text>click</text></a>
              <rect width="1" height="1" onclick="alert(1)" style="fill:red" />
            </svg>
            """);

    private sealed class RecordingCommandMediator : ICommandMediator
    {
        public List<ICommand> Commands { get; } = [];

        public Task<MediationResult> TrySendAsync(ICommand command, CommandMediationSettings? settings, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<MediationResult<TCommandResult>> TrySendAsync<TCommandResult>(ICommand<TCommandResult> command, CommandMediationSettings? settings, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<MediationDecision> EvaluateAsync(ICommand command, CommandMediationSettings? settings, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task SendAsync(ICommand command, CommandMediationSettings? settings, CancellationToken cancellationToken)
        {
            Commands.Add(command);
            return Task.CompletedTask;
        }

        public Task<TCommandResult> SendAsync<TCommandResult>(
            ICommand<TCommandResult> command,
            CommandMediationSettings? settings,
            CancellationToken cancellationToken)
        {
            Commands.Add(command);
            return Task.FromException<TCommandResult>(new NotSupportedException("No command results configured for this test."));
        }

        public TCommand Single<TCommand>() where TCommand : ICommand
        {
            return Commands.OfType<TCommand>().Single();
        }
    }
}
