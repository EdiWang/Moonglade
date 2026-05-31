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
    private CannonService _cannonService;

    [Fact]
    public async Task Image_Get_WhenFilenameContainsInvalidCharacters_ReturnsBadRequest()
    {
        var controller = CreateController();

        var result = await controller.Image("bad" + Path.GetInvalidFileNameChars()[0] + ".png");

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("invalid filename", badRequestResult.Value);
        _imageStorage.Verify(x => x.GetAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Image_Get_WhenOriginImageRequested_ReturnsForbid()
    {
        var controller = CreateController(remoteIpAddress: IPAddress.Parse("127.0.0.1"));

        var result = await controller.Image("photo-origin.png");

        Assert.IsType<ForbidResult>(result);
        _imageStorage.Verify(x => x.GetAsync(It.IsAny<string>()), Times.Never);
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

        var result = await controller.Image("photo.png");

        var redirectResult = Assert.IsType<RedirectResult>(result);
        Assert.True(redirectResult.Permanent);
        Assert.Equal("https://cdn.example.com/images/photo.png", redirectResult.Url);
        _imageStorage.Verify(x => x.GetAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Image_Get_WhenImageDoesNotExist_ReturnsNotFound()
    {
        _imageStorage.Setup(x => x.GetAsync("missing.png")).ReturnsAsync((ImageInfo)null!);
        var controller = CreateController();

        var result = await controller.Image("missing.png");

        Assert.IsType<NotFoundResult>(result);
        _imageStorage.Verify(x => x.GetAsync("missing.png"), Times.Once);
    }

    [Fact]
    public async Task Image_Get_WhenImageExists_ReturnsFileContentResult()
    {
        var bytes = new byte[] { 1, 2, 3 };
        _imageStorage
            .Setup(x => x.GetAsync("photo.png"))
            .ReturnsAsync(new ImageInfo { ImageBytes = bytes, ImageExtensionName = "png" });
        var controller = CreateController();

        var result = await controller.Image("photo.png");

        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("image/png", fileResult.ContentType);
        Assert.Equal(bytes, fileResult.FileContents);
    }

    [Fact]
    public async Task Image_Get_WhenCalledTwice_UsesCache()
    {
        _imageStorage
            .Setup(x => x.GetAsync("photo.webp"))
            .ReturnsAsync(new ImageInfo { ImageBytes = [4, 5, 6], ImageExtensionName = "webp" });
        var controller = CreateController();

        await controller.Image("photo.webp");
        await controller.Image("photo.webp");

        _imageStorage.Verify(x => x.GetAsync("photo.webp"), Times.Once);
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
