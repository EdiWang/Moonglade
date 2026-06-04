using Edi.CacheAside.InMemory;
using LiteBus.Commands.Abstractions;
using LiteBus.Events.Abstractions;
using LiteBus.Queries.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Moonglade.ActivityLog;
using Moonglade.Features.Asset;
using Moonglade.Setup;
using Moonglade.Web.Controllers;
using Moq;
using SkiaSharp;
using System.Net;
using System.Security.Claims;

namespace Moonglade.Web.Tests;

public class AssetsControllerTests
{
    private readonly Mock<IEventMediator> _eventMediator = new();
    private readonly Mock<IQueryMediator> _queryMediator = new();
    private readonly Mock<IWebHostEnvironment> _environment = new();
    private readonly Mock<ICacheAside> _cache = new();
    private readonly Mock<ISiteIconBuilder> _siteIconBuilder = new();
    private readonly RecordingCommandMediator _commandMediator = new();

    public AssetsControllerTests()
    {
        _environment.SetupProperty(x => x.WebRootPath, @"E:\webroot");
        _environment.SetupProperty(x => x.ApplicationName, nameof(AssetsControllerTests));
        _environment.SetupProperty(x => x.EnvironmentName, "Development");
        _environment.SetupProperty(x => x.ContentRootPath, @"E:\contentroot");
        _environment.SetupProperty(x => x.ContentRootFileProvider, Mock.Of<IFileProvider>());
        _environment.SetupProperty(x => x.WebRootFileProvider, Mock.Of<IFileProvider>());
    }

    [Fact]
    public async Task Avatar_Get_WhenCacheHasBytes_ReturnsFileContentResult()
    {
        var bytes = new byte[] { 1, 2, 3 };
        _cache
            .Setup(x => x.GetOrCreateAsync(
                BlogCachePartition.General.ToString(),
                "avatar",
                It.IsAny<Func<Task<byte[]>>>()))
            .ReturnsAsync(bytes);

        var controller = CreateController();

        var result = await controller.Avatar(_cache.Object);

        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("image/png", fileResult.ContentType);
        Assert.Equal(bytes, fileResult.FileContents);
    }

    [Fact]
    public async Task Avatar_Get_WhenCacheMissAndAssetMissing_ReturnsDefaultPhysicalFile()
    {
        _cache
            .Setup(x => x.GetOrCreateAsync(
                BlogCachePartition.General.ToString(),
                "avatar",
                It.IsAny<Func<Task<byte[]>>>()))
            .ReturnsAsync((byte[])null!);

        var controller = CreateController();

        var result = await controller.Avatar(_cache.Object);

        var fileResult = Assert.IsType<PhysicalFileResult>(result);
        Assert.Equal("image/png", fileResult.ContentType);
        Assert.Equal(Path.Join(_environment.Object.WebRootPath, "images", "default-avatar.png"), fileResult.FileName);
    }

    [Fact]
    public async Task Avatar_Post_WhenBase64IsInvalid_ReturnsConflict()
    {
        var controller = CreateController();

        var result = await controller.Avatar("not-base64");

        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal("Bad base64 data", conflictResult.Value);
        _eventMediator.Verify(x => x.PublishAsync(It.IsAny<SaveAssetEvent>(), It.IsAny<EventMediationSettings>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.Empty(_commandMediator.Commands);
    }

    [Fact]
    public async Task Avatar_Post_WhenImageIsNot300By300_ReturnsConflict()
    {
        var controller = CreateController();
        var base64 = Convert.ToBase64String(CreatePngBytes(200, 200));

        var result = await controller.Avatar(base64);

        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal("Image size must be 300x300.", conflictResult.Value);
        _eventMediator.Verify(x => x.PublishAsync(It.IsAny<SaveAssetEvent>(), It.IsAny<EventMediationSettings>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.Empty(_commandMediator.Commands);
    }

    [Fact]
    public async Task Avatar_Post_WhenImageIsValid_PublishesEventAndWritesActivityLog()
    {
        var controller = CreateController("admin", IPAddress.Parse("127.0.0.1"), "unit-test-agent");
        var base64 = Convert.ToBase64String(CreatePngBytes(300, 300));

        var result = await controller.Avatar(base64);

        Assert.IsType<OkResult>(result);
        _eventMediator.Verify(
            x => x.PublishAsync(
                It.Is<SaveAssetEvent>(e => e.AssetId == AssetId.AvatarBase64 && e.AssetBase64 == base64),
                It.IsAny<EventMediationSettings>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        var activityCommand = _commandMediator.Single<CreateActivityLogCommand>();
        Assert.Equal(EventType.AvatarUpdated, activityCommand.EventType);
        Assert.Equal("admin", activityCommand.ActorId);
        Assert.Equal("Update Avatar", activityCommand.Operation);
        Assert.Equal("Avatar Image", activityCommand.TargetName);
        Assert.Equal("127.0.0.1", activityCommand.IpAddress);
        Assert.Equal("unit-test-agent", activityCommand.UserAgent);
    }

    [Fact]
    public void SiteIcon_WhenIconExistsWithPngExtension_ReturnsPngFile()
    {
        InMemoryIconGenerator.ClearIcons();
        var bytes = new byte[] { 9, 8, 7 };
        InMemoryIconGenerator.LoadIcon("favicon-32x32.png", bytes);
        var controller = CreateController();

        var result = controller.SiteIcon("favicon-32x32.png");

        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("image/png", fileResult.ContentType);
        Assert.Equal(bytes, fileResult.FileContents);
    }

    [Fact]
    public void SiteIcon_WhenIconExistsWithIcoExtension_ReturnsIconContentType()
    {
        InMemoryIconGenerator.ClearIcons();
        var bytes = new byte[] { 5, 4, 3 };
        InMemoryIconGenerator.LoadIcon("favicon.ico", bytes);
        var controller = CreateController();

        var result = controller.SiteIcon("favicon.ico");

        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("image/x-icon", fileResult.ContentType);
        Assert.Equal(bytes, fileResult.FileContents);
    }

    [Fact]
    public void SiteIcon_WhenIconDoesNotExist_ReturnsNotFound()
    {
        InMemoryIconGenerator.ClearIcons();
        var controller = CreateController();

        var result = controller.SiteIcon("missing.png");

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task SiteIconOrigin_WhenAssetMissing_ReturnsDefaultPhysicalFile()
    {
        _queryMediator
            .Setup(x => x.QueryAsync(It.IsAny<GetAssetQuery>(), It.IsAny<QueryMediationSettings>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string)null!);

        var controller = CreateController();

        var result = await controller.SiteIconOrigin();

        var fileResult = Assert.IsType<PhysicalFileResult>(result);
        Assert.Equal("image/png", fileResult.ContentType);
        Assert.Equal(Path.Join(_environment.Object.WebRootPath, "images", "siteicon-default.png"), fileResult.FileName);
    }

    [Fact]
    public async Task SiteIconOrigin_WhenAssetIsValidBase64_ReturnsFileContentResult()
    {
        var bytes = new byte[] { 7, 8, 9 };
        _queryMediator
            .Setup(x => x.QueryAsync(It.IsAny<GetAssetQuery>(), It.IsAny<QueryMediationSettings>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Convert.ToBase64String(bytes));

        var controller = CreateController();

        var result = await controller.SiteIconOrigin();

        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("image/png", fileResult.ContentType);
        Assert.Equal(bytes, fileResult.FileContents);
    }

    [Fact]
    public async Task SiteIconOrigin_WhenAssetBase64IsInvalid_ReturnsDefaultPhysicalFile()
    {
        _queryMediator
            .Setup(x => x.QueryAsync(It.IsAny<GetAssetQuery>(), It.IsAny<QueryMediationSettings>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("not-base64");

        var controller = CreateController();

        var result = await controller.SiteIconOrigin();

        var fileResult = Assert.IsType<PhysicalFileResult>(result);
        Assert.Equal("image/png", fileResult.ContentType);
        Assert.Equal(Path.Join(_environment.Object.WebRootPath, "images", "siteicon-default.png"), fileResult.FileName);
    }

    [Fact]
    public async Task UpdateSiteIcon_WhenBase64IsInvalid_ReturnsConflict()
    {
        var controller = CreateController();

        var result = await controller.UpdateSiteIcon("bad");

        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal("Bad base64 data", conflictResult.Value);
        _eventMediator.Verify(x => x.PublishAsync(It.IsAny<SaveAssetEvent>(), It.IsAny<EventMediationSettings>(), It.IsAny<CancellationToken>()), Times.Never);
        _siteIconBuilder.Verify(x => x.RegenerateSiteIcons(It.IsAny<string>()), Times.Never);
        Assert.Empty(_commandMediator.Commands);
    }

    [Fact]
    public async Task UpdateSiteIcon_WhenImageIsNotSquare_ReturnsConflict()
    {
        var controller = CreateController();
        var base64 = Convert.ToBase64String(CreatePngBytes(300, 200));

        var result = await controller.UpdateSiteIcon(base64);

        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal("image height must be equal to width", conflictResult.Value);
        _eventMediator.Verify(x => x.PublishAsync(It.IsAny<SaveAssetEvent>(), It.IsAny<EventMediationSettings>(), It.IsAny<CancellationToken>()), Times.Never);
        _siteIconBuilder.Verify(x => x.RegenerateSiteIcons(It.IsAny<string>()), Times.Never);
        Assert.Empty(_commandMediator.Commands);
    }

    [Fact]
    public async Task UpdateSiteIcon_WhenImageIsValid_PublishesEventRegeneratesIconsAndWritesActivityLog()
    {
        var controller = CreateController("admin", IPAddress.Parse("127.0.0.1"), "unit-test-agent");
        var base64 = Convert.ToBase64String(CreatePngBytes(64, 64));

        var result = await controller.UpdateSiteIcon(base64);

        Assert.IsType<NoContentResult>(result);
        _eventMediator.Verify(
            x => x.PublishAsync(
                It.Is<SaveAssetEvent>(e => e.AssetId == AssetId.SiteIconBase64 && e.AssetBase64 == base64),
                It.IsAny<EventMediationSettings>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _siteIconBuilder.Verify(x => x.RegenerateSiteIcons(base64), Times.Once);

        var activityCommand = _commandMediator.Single<CreateActivityLogCommand>();
        Assert.Equal(EventType.SiteIconUpdated, activityCommand.EventType);
        Assert.Equal("admin", activityCommand.ActorId);
        Assert.Equal("Update Site Icon", activityCommand.Operation);
        Assert.Equal("Site Icon", activityCommand.TargetName);
        Assert.Equal("127.0.0.1", activityCommand.IpAddress);
        Assert.Equal("unit-test-agent", activityCommand.UserAgent);
        Assert.NotNull(activityCommand.MetaData);
        Assert.Equal("64x64", activityCommand.MetaData!.GetType().GetProperty("ImageSize")!.GetValue(activityCommand.MetaData));
    }

    private AssetsController CreateController(
        string? username = null,
        IPAddress? remoteIpAddress = null,
        string? userAgent = null)
    {
        var controller = new AssetsController(
            _eventMediator.Object,
            _queryMediator.Object,
            _environment.Object,
            Mock.Of<ILogger<AssetsController>>(),
            _siteIconBuilder.Object,
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

    private static byte[] CreatePngBytes(int width, int height)
    {
        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Blue);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data!.ToArray();
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
