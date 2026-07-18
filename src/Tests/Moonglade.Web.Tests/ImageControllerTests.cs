using LiteBus.Commands.Abstractions;
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
using Moq;
using System.Net;
using System.Security.Claims;

namespace Moonglade.Web.Tests;

public class ImageControllerTests
{
    private readonly Mock<IBlogImageStorage> _imageStorage = new();
    private readonly Mock<IFileNameGenerator> _fileNameGenerator = new();
    private readonly RecordingCommandMediator _commandMediator = new();
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
    private CannonService _cannonService = null!;

    [Fact]
    public async Task Image_Get_WhenFilenameContainsInvalidCharacters_ReturnsBadRequest()
    {
        var controller = CreateController();

        var result = await controller.Image("bad" + Path.GetInvalidFileNameChars()[0] + ".png", TestContext.Current.CancellationToken);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("invalid filename", badRequestResult.Value);
        _imageStorage.Verify(x => x.GetInfoAsync(It.IsAny<string>()), Times.Never);
        _imageStorage.Verify(x => x.OpenReadAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Image_Get_WhenOriginImageRequested_ReturnsFileStreamResult()
    {
        var bytes = new byte[] { 1, 2, 3 };
        _imageStorage
            .Setup(x => x.GetInfoAsync("photo-origin.png"))
            .ReturnsAsync(CreateImageInfo("png", bytes.Length));
        _imageStorage
            .Setup(x => x.OpenReadAsync("photo-origin.png"))
            .ReturnsAsync(new MemoryStream(bytes));
        var controller = CreateController(remoteIpAddress: IPAddress.Parse("127.0.0.1"));

        var result = await controller.Image("photo-origin.png", TestContext.Current.CancellationToken);

        var fileResult = Assert.IsType<FileStreamResult>(result);
        Assert.Equal("image/png", fileResult.ContentType);
        _imageStorage.Verify(x => x.GetInfoAsync("photo-origin.png"), Times.Once);
        _imageStorage.Verify(x => x.OpenReadAsync("photo-origin.png"), Times.Once);
    }

    [Fact]
    public async Task Image_Get_WhenCdnRedirectIsEnabled_ReturnsPermanentRedirect()
    {
        var controller = CreateController(new BlogConfig
        {
            ImageSettings = new ImageSettings
            {
                EnableCDNRedirect = true,
                CDNEndpoint = "https://cdn.example.com/images"
            }
        });

        var result = await controller.Image("photo.png", TestContext.Current.CancellationToken);

        var redirectResult = Assert.IsType<RedirectResult>(result);
        Assert.True(redirectResult.Permanent);
        Assert.Equal("https://cdn.example.com/images/photo.png", redirectResult.Url);
        _imageStorage.Verify(x => x.GetInfoAsync(It.IsAny<string>()), Times.Never);
        _imageStorage.Verify(x => x.OpenReadAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Image_Get_WhenImageDoesNotExist_ReturnsNotFound()
    {
        _imageStorage.Setup(x => x.GetInfoAsync("missing.png")).ReturnsAsync((ImageInfo)null!);
        var controller = CreateController();

        var result = await controller.Image("missing.png", TestContext.Current.CancellationToken);

        Assert.IsType<NotFoundResult>(result);
        _imageStorage.Verify(x => x.GetInfoAsync("missing.png"), Times.Once);
        _imageStorage.Verify(x => x.OpenReadAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Image_Get_WhenImageExists_ReturnsFileStreamResultWithCacheHeaders()
    {
        var bytes = new byte[] { 1, 2, 3 };
        var lastModified = new DateTimeOffset(2026, 2, 23, 10, 30, 0, TimeSpan.Zero);
        _imageStorage
            .Setup(x => x.GetInfoAsync("photo.png"))
            .ReturnsAsync(CreateImageInfo("png", bytes.Length, lastModified, "\"photo-etag\""));
        _imageStorage
            .Setup(x => x.OpenReadAsync("photo.png"))
            .ReturnsAsync(new MemoryStream(bytes));
        var controller = CreateController();

        var result = await controller.Image("photo.png", TestContext.Current.CancellationToken);

        var fileResult = Assert.IsType<FileStreamResult>(result);
        Assert.Equal("image/png", fileResult.ContentType);
        Assert.True(fileResult.EnableRangeProcessing);
        Assert.Equal(lastModified, fileResult.LastModified);
        Assert.Equal("\"photo-etag\"", fileResult.EntityTag!.ToString());
        Assert.Equal("public, max-age=300", controller.HttpContext.Response.Headers.CacheControl.ToString());

        using var reader = new MemoryStream();
        await fileResult.FileStream.CopyToAsync(reader, TestContext.Current.CancellationToken);
        Assert.Equal(bytes, reader.ToArray());
    }

    [Fact]
    public async Task Image_Get_WhenCalledTwice_UsesMetadataCacheAndOpensFreshStreams()
    {
        var bytes = new byte[] { 4, 5, 6 };
        _imageStorage
            .Setup(x => x.GetInfoAsync("photo.webp"))
            .ReturnsAsync(CreateImageInfo("webp", bytes.Length));
        _imageStorage
            .Setup(x => x.OpenReadAsync("photo.webp"))
            .Returns(() => Task.FromResult<Stream>(new MemoryStream(bytes)));
        var controller = CreateController();

        await controller.Image("photo.webp", TestContext.Current.CancellationToken);
        await controller.Image("photo.webp", TestContext.Current.CancellationToken);

        _imageStorage.Verify(x => x.GetInfoAsync("photo.webp"), Times.Once);
        _imageStorage.Verify(x => x.OpenReadAsync("photo.webp"), Times.Exactly(2));
    }

    [Fact]
    public async Task Image_Get_WhenIfNoneMatchMatches_ReturnsNotModifiedWithoutOpeningStream()
    {
        _imageStorage
            .Setup(x => x.GetInfoAsync("photo.png"))
            .ReturnsAsync(CreateImageInfo("png", 3, entityTag: "\"photo-etag\""));
        var controller = CreateController();
        controller.HttpContext.Request.Headers.IfNoneMatch = "\"photo-etag\"";

        var result = await controller.Image("photo.png", TestContext.Current.CancellationToken);

        var statusCodeResult = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(StatusCodes.Status304NotModified, statusCodeResult.StatusCode);
        Assert.Equal("\"photo-etag\"", controller.HttpContext.Response.Headers.ETag.ToString());
        Assert.Equal("public, max-age=300", controller.HttpContext.Response.Headers.CacheControl.ToString());
        _imageStorage.Verify(x => x.OpenReadAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Image_Get_WhenIfModifiedSinceIsCurrent_ReturnsNotModifiedWithoutOpeningStream()
    {
        var lastModified = new DateTimeOffset(2026, 2, 23, 10, 30, 0, TimeSpan.Zero);
        _imageStorage
            .Setup(x => x.GetInfoAsync("photo.png"))
            .ReturnsAsync(CreateImageInfo("png", 3, lastModified));
        var controller = CreateController();
        controller.HttpContext.Request.Headers.IfModifiedSince = lastModified.ToString("R");

        var result = await controller.Image("photo.png", TestContext.Current.CancellationToken);

        var statusCodeResult = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(StatusCodes.Status304NotModified, statusCodeResult.StatusCode);
        Assert.Equal(lastModified.ToString("R"), controller.HttpContext.Response.Headers.LastModified.ToString());
        _imageStorage.Verify(x => x.OpenReadAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Image_Post_WhenFileIsEmpty_ReturnsBadRequest()
    {
        var controller = CreateController();
        var file = CreateFormFile("empty.png", []);

        var result = await controller.Image(file);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Image file is empty.", badRequestResult.Value);
        _imageStorage.Verify(x => x.InsertAsync(It.IsAny<string>(), It.IsAny<byte[]>()), Times.Never);
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
        _imageStorage.Verify(x => x.InsertAsync(It.IsAny<string>(), It.IsAny<byte[]>()), Times.Never);
        Assert.Empty(_commandMediator.Commands);
    }

    [Fact]
    public async Task Image_Post_WhenExtensionIsNotAllowed_ReturnsBadRequest()
    {
        var controller = CreateController();
        var file = CreateFormFile("photo.txt", [1, 2, 3]);

        var result = await controller.Image(file);

        Assert.IsType<BadRequestResult>(result);
        _imageStorage.Verify(x => x.InsertAsync(It.IsAny<string>(), It.IsAny<byte[]>()), Times.Never);
        Assert.Empty(_commandMediator.Commands);
    }

    [Fact]
    public async Task Image_Post_WhenValidFileAndWatermarkDisabled_SavesImageAndWritesActivityLog()
    {
        var imageBytes = new byte[] { 1, 2, 3, 4 };
        _fileNameGenerator.Setup(x => x.GetFileName("photo.png", "")).Returns("photo-primary.png");
        _fileNameGenerator.Setup(x => x.GetFileName("photo.png", "origin")).Returns("photo-origin.png");
        _imageStorage.Setup(x => x.InsertAsync("photo-primary.png", It.IsAny<byte[]>())).ReturnsAsync("photo-cdn.png");
        var controller = CreateController(
            new BlogConfig { ImageSettings = new ImageSettings { IsWatermarkEnabled = false } },
            username: "admin",
            remoteIpAddress: IPAddress.Parse("127.0.0.1"),
            userAgent: "unit-test-agent");
        var file = CreateFormFile("C:\\temp\\photo.png", imageBytes);

        var result = await controller.Image(file);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("/image/photo-cdn.png", okResult.Value!.GetType().GetProperty("location")!.GetValue(okResult.Value));
        Assert.Equal("/image/photo-cdn.png", okResult.Value.GetType().GetProperty("filename")!.GetValue(okResult.Value));
        _imageStorage.Verify(x => x.InsertAsync("photo-primary.png", It.Is<byte[]>(bytes => bytes.SequenceEqual(imageBytes))), Times.Once);
        _imageStorage.Verify(x => x.InsertSecondaryAsync(It.IsAny<string>(), It.IsAny<byte[]>()), Times.Never);

        var activityCommand = _commandMediator.Single<CreateActivityLogCommand>();
        Assert.Equal(EventType.ImageUploaded, activityCommand.EventType);
        Assert.Equal("admin", activityCommand.ActorId);
        Assert.Equal("Upload Image", activityCommand.Operation);
        Assert.Equal("photo-cdn.png", activityCommand.TargetName);
        Assert.Equal("127.0.0.1", activityCommand.IpAddress);
        Assert.Equal("unit-test-agent", activityCommand.UserAgent);
        Assert.NotNull(activityCommand.MetaData);
        Assert.Equal("photo-cdn.png", activityCommand.MetaData!.GetType().GetProperty("FileName")!.GetValue(activityCommand.MetaData));
        Assert.Equal(file.Length, activityCommand.MetaData.GetType().GetProperty("FileSize")!.GetValue(activityCommand.MetaData));
        Assert.Equal(false, activityCommand.MetaData.GetType().GetProperty("SkipWatermark")!.GetValue(activityCommand.MetaData));
    }

    [Fact]
    public async Task Image_Post_WhenSkipWatermarkIsTrue_DoesNotStoreOriginalImage()
    {
        _fileNameGenerator.Setup(x => x.GetFileName("photo.png", "")).Returns("photo-primary.png");
        _fileNameGenerator.Setup(x => x.GetFileName("photo.png", "origin")).Returns("photo-origin.png");
        _imageStorage.Setup(x => x.InsertAsync("photo-primary.png", It.IsAny<byte[]>())).ReturnsAsync("photo-primary.png");
        var controller = CreateController(new BlogConfig { ImageSettings = ImageSettings.DefaultValue });
        var file = CreateFormFile("photo.png", [1, 2, 3]);

        await controller.Image(file, skipWatermark: true);

        _imageStorage.Verify(x => x.InsertSecondaryAsync(It.IsAny<string>(), It.IsAny<byte[]>()), Times.Never);
        Assert.True(_commandMediator.Single<CreateActivityLogCommand>().MetaData!.GetType().GetProperty("SkipWatermark")!.GetValue(_commandMediator.Single<CreateActivityLogCommand>().MetaData) as bool?);
    }

    [Fact]
    public async Task Image_Post_WhenWatermarkEnabledAndKeepOriginalEnabled_StoresOriginalImageInBackgroundQueue()
    {
        var imageBytes = new byte[] { 1, 2, 3, 4 };
        _fileNameGenerator.Setup(x => x.GetFileName("photo.svg", "")).Returns("photo.svg");
        _fileNameGenerator.Setup(x => x.GetFileName("photo.svg", "origin")).Returns("photo-origin.svg");
        _imageStorage.Setup(x => x.InsertAsync("photo.svg", It.IsAny<byte[]>())).ReturnsAsync("photo.svg");
        var controller = CreateController(new BlogConfig
        {
            ImageSettings = new ImageSettings
            {
                IsWatermarkEnabled = true,
                KeepOriginImage = true,
                WatermarkFontSize = 20,
                WatermarkText = "Moonglade"
            }
        });
        var file = CreateFormFile("photo.svg", imageBytes);

        await controller.Image(file);
        await StopCannonServiceAsync();

        _imageStorage.Verify(x => x.InsertSecondaryAsync("photo-origin.svg", It.Is<byte[]>(bytes => bytes.SequenceEqual(imageBytes))), Times.Once);
    }

    private ImageController CreateController(
        BlogConfig? blogConfig = null,
        string? username = null,
        IPAddress? remoteIpAddress = null,
        string? userAgent = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton(_imageStorage.Object);
        var serviceProvider = services.BuildServiceProvider();
        _cannonService = new CannonService(Mock.Of<ILogger<CannonService>>(), serviceProvider.GetRequiredService<IServiceScopeFactory>());
        _cannonService.StartAsync(TestContext.Current.CancellationToken);

        var controller = new ImageController(
            _imageStorage.Object,
            Mock.Of<ILogger<ImageController>>(),
            blogConfig ?? new BlogConfig { ImageSettings = new ImageSettings() },
            _cache,
            _fileNameGenerator.Object,
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
        await _cannonService.StopAsync(TestContext.Current.CancellationToken);
    }

    private static FormFile CreateFormFile(string fileName, byte[] bytes)
    {
        return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", fileName);
    }

    private static ImageInfo CreateImageInfo(
        string extension,
        long contentLength,
        DateTimeOffset? lastModified = null,
        string entityTag = "\"test-etag\"")
    {
        return new ImageInfo
        {
            ImageExtensionName = extension,
            ContentType = ImageInfo.GetContentType(extension),
            ContentLength = contentLength,
            LastModifiedUtc = lastModified ?? new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            EntityTag = entityTag
        };
    }

    private sealed class RecordingCommandMediator : ICommandMediator
    {
        public List<ICommand> Commands { get; } = [];

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
