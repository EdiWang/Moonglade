using Edi.CacheAside.InMemory;
using LiteBus.Commands.Abstractions;
using LiteBus.Events.Abstractions;
using LiteBus.Messaging.Abstractions;
using LiteBus.Queries.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Moonglade.ActivityLog;
using Moonglade.Features.Asset;
using Moonglade.Setup;
using Moonglade.Web.Controllers;
using Moonglade.Web.Tests.TestSupport;
using Moq;
using SkiaSharp;
using System.Net;
using System.Security.Claims;

namespace Moonglade.Web.Tests.Controllers;

[Collection("Site icon cache")]
public partial class AssetsControllerTests : IDisposable
{
    private readonly string _testTempDirectory = Path.Combine(Path.GetTempPath(), "moonglade-assets-tests", Guid.NewGuid().ToString("N"));
    private readonly Dictionary<string, string?> _originalTempVariables = new[] { "TEMP", "TMP", "TMPDIR" }
        .ToDictionary(name => name, Environment.GetEnvironmentVariable);
    private readonly Mock<IEventMediator> _eventMediator = new();
    private readonly Mock<IQueryMediator> _queryMediator = new();
    private readonly Mock<IWebHostEnvironment> _environment = new();
    private readonly Mock<ICacheAside> _cache = new();
    private readonly AvatarCacheInvalidator _avatarCache;
    private readonly RecordingCommandMediator _commandMediator = new();

    public AssetsControllerTests()
    {
        _avatarCache = new AvatarCacheInvalidator(_cache.Object);
        Directory.CreateDirectory(_testTempDirectory);
        foreach (var name in _originalTempVariables.Keys)
        {
            Environment.SetEnvironmentVariable(name, _testTempDirectory);
        }
        InMemoryIconGenerator.ClearIcons();
        _environment.SetupProperty(x => x.WebRootPath, @"E:\webroot");
        _environment.SetupProperty(x => x.ApplicationName, nameof(AssetsControllerTests));
        _environment.SetupProperty(x => x.EnvironmentName, "Development");
        _environment.SetupProperty(x => x.ContentRootPath, @"E:\contentroot");
        _environment.SetupProperty(x => x.ContentRootFileProvider, Mock.Of<IFileProvider>());
        _environment.SetupProperty(x => x.WebRootFileProvider, Mock.Of<IFileProvider>());
    }

    [Theory]
    [InlineData(new byte[] { 0x89, 0x50, 0x4E, 0x47 }, "image/png")]
    [InlineData(new byte[] { 0xFF, 0xD8, 0xFF }, "image/jpeg")]
    public async Task Avatar_Get_WhenCacheHasBytes_ReturnsFileWithMatchingContentType(byte[] bytes, string contentType)
    {
        _cache
            .Setup(x => x.GetOrCreateAsync(
                BlogCachePartition.Account.ToString(),
                _avatarCache.CacheKey,
                It.IsAny<Func<Task<byte[]>>>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync(bytes);

        using var store = new ConfigurationCommandRecorder();
        var controller = CreateAvatarController(store, commandMediator: _commandMediator);

        var result = await controller.Avatar(TestContext.Current.CancellationToken);

        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal(contentType, fileResult.ContentType);
        Assert.Equal(bytes, fileResult.FileContents);
    }

    [Fact]
    public async Task Avatar_Get_WhenCacheMissAndAssetMissing_ReturnsDefaultPhysicalFile()
    {
        _cache
            .Setup(x => x.GetOrCreateAsync(
                BlogCachePartition.Account.ToString(),
                _avatarCache.CacheKey,
                It.IsAny<Func<Task<byte[]>>>(),
                It.IsAny<TimeSpan?>()))
            .ReturnsAsync((byte[])null!);

        using var store = new ConfigurationCommandRecorder();
        var controller = CreateAvatarController(store, commandMediator: _commandMediator);

        var result = await controller.Avatar(TestContext.Current.CancellationToken);

        var fileResult = Assert.IsType<PhysicalFileResult>(result);
        Assert.Equal("image/png", fileResult.ContentType);
        Assert.Equal(Path.Join(_environment.Object.WebRootPath, "images", "default-avatar.png"), fileResult.FileName);
    }

    [Fact]
    public async Task Avatar_Get_WhenOldReadCompletesAfterUpload_NextReadReturnsNewAvatar()
    {
        var ct = TestContext.Current.CancellationToken;
        using var services = new ServiceCollection().AddInMemoryCacheAside().BuildServiceProvider();
        using var scope = services.CreateScope();
        var cache = scope.ServiceProvider.GetRequiredService<ICacheAside>();
        var oldImageRead = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var resumeOldRead = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var oldBytes = CreatePngBytes(300, 300);
        var newBytes = CreatePngBytes(300, 300, SKColors.Red);
        _cache.Setup(x => x.GetOrCreateAsync(nameof(BlogCachePartition.Account), It.IsAny<string>(),
                It.IsAny<Func<Task<byte[]>>>(), It.IsAny<TimeSpan?>()))
            .Returns((string partition, string key, Func<Task<byte[]>> read, TimeSpan? expiration) =>
                cache.GetOrCreateAsync(partition, key, async () =>
                {
                    var bytes = await read();
                    if (bytes.AsSpan().SequenceEqual(oldBytes))
                    {
                        oldImageRead.SetResult();
                        await resumeOldRead.Task.WaitAsync(ct);
                    }
                    return bytes;
                }, expiration));
        _cache.Setup(x => x.Remove(nameof(BlogCachePartition.Account), It.IsAny<string>()))
            .Callback((string partition, string key) => cache.Remove(partition, key));

        using var store = new ConfigurationCommandRecorder();
        var upload = CreateAvatarController(store);
        Assert.IsType<OkResult>(await upload.Upload(Convert.ToBase64String(oldBytes), ct));

        var pendingRead = CreateAvatarController(store).Avatar(ct);
        try
        {
            await oldImageRead.Task.WaitAsync(TimeSpan.FromSeconds(5), ct);
            Assert.IsType<OkResult>(await upload.Upload(Convert.ToBase64String(newBytes), ct));
        }
        finally
        {
            resumeOldRead.TrySetResult();
        }

        var oldAvatar = Assert.IsType<FileContentResult>(await pendingRead);
        var newAvatar = Assert.IsType<FileContentResult>(await CreateAvatarController(store).Avatar(ct));
        Assert.Equal(oldBytes, oldAvatar.FileContents);
        Assert.Equal(newBytes, newAvatar.FileContents);
        Assert.NotEqual(oldAvatar.EntityTag, newAvatar.EntityTag);
    }

    [Fact]
    public async Task Avatar_Post_WhenBase64IsInvalid_ReturnsConflict()
    {
        using var store = new ConfigurationCommandRecorder();
        var controller = CreateAvatarController(store, commandMediator: _commandMediator);

        var result = await controller.Upload("not-base64", TestContext.Current.CancellationToken);

        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal("Bad base64 data", conflictResult.Value);
        Assert.False(await store.Db.OwnerAvatar.AnyAsync(TestContext.Current.CancellationToken));
        Assert.Empty(_commandMediator.Commands);
    }

    [Fact]
    public async Task Avatar_Post_WhenImageIsNot300By300_ReturnsConflict()
    {
        using var store = new ConfigurationCommandRecorder();
        var controller = CreateAvatarController(store, commandMediator: _commandMediator);
        var base64 = Convert.ToBase64String(CreatePngBytes(200, 200));

        var result = await controller.Upload(base64, TestContext.Current.CancellationToken);

        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal("Image size must be 300x300.", conflictResult.Value);
        Assert.False(await store.Db.OwnerAvatar.AnyAsync(TestContext.Current.CancellationToken));
        Assert.Empty(_commandMediator.Commands);
    }

    [Fact]
    public async Task Avatar_Post_WhenImageIsValid_SavesAvatarAndWritesActivityLog()
    {
        using var store = new ConfigurationCommandRecorder();
        var controller = CreateAvatarController(store, commandMediator: _commandMediator);
        controller.ControllerContext = CreateController("admin", IPAddress.Parse("127.0.0.1"), "unit-test-agent").ControllerContext;
        var base64 = Convert.ToBase64String(CreatePngBytes(300, 300));
        var previousCacheKey = _avatarCache.CacheKey;

        var result = await controller.Upload(base64, TestContext.Current.CancellationToken);

        Assert.IsType<OkResult>(result);
        Assert.Equal(base64, (await store.Db.OwnerAvatar.SingleAsync(TestContext.Current.CancellationToken)).ImageBase64);
        _cache.Verify(cache => cache.Remove(nameof(BlogCachePartition.Account), previousCacheKey), Times.Once);

        var activityCommand = _commandMediator.Single<CreateActivityLogCommand>();
        Assert.Equal(ActivityEvents.AvatarUpdated, activityCommand.EventKey);
        Assert.Equal("admin", activityCommand.ActorDisplayName);
        Assert.Equal("Avatar Image", activityCommand.TargetDisplayName);
        Assert.Null(activityCommand.TargetId);
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
        Assert.Empty(InMemoryIconGenerator.SiteIconDictionary);
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
        Assert.Empty(InMemoryIconGenerator.SiteIconDictionary);
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
        var icon = InMemoryIconGenerator.GetIcon("favicon-32x32.png");
        Assert.NotNull(icon);
        using var bitmap = SKBitmap.Decode(icon);
        Assert.Equal(32, bitmap.Width);
        Assert.Equal(32, bitmap.Height);

        var activityCommand = _commandMediator.Single<CreateActivityLogCommand>();
        Assert.Equal(ActivityEvents.SiteIconUpdated, activityCommand.EventKey);
        Assert.Equal("admin", activityCommand.ActorDisplayName);
        Assert.Equal("Site Icon", activityCommand.TargetDisplayName);
        Assert.Equal(AssetId.SiteIconBase64.ToString(), activityCommand.TargetId);
        Assert.Equal("127.0.0.1", activityCommand.IpAddress);
        Assert.Equal("unit-test-agent", activityCommand.UserAgent);
        Assert.NotNull(activityCommand.Details);
        Assert.Equal("64x64", ActivityLogMetaDataAssert.Value<string>(activityCommand, "ImageSize"));
    }

    public void Dispose()
    {
        foreach (var (name, value) in _originalTempVariables)
        {
            Environment.SetEnvironmentVariable(name, value);
        }
        Directory.Delete(_testTempDirectory, recursive: true);
    }

    private AssetsController CreateController(
        string? username = null,
        IPAddress? remoteIpAddress = null,
        string? userAgent = null,
        ICommandMediator? commandMediator = null,
        ILogger<AssetsController>? logger = null)
    {
        var controller = new AssetsController(
            _eventMediator.Object,
            _queryMediator.Object,
            _environment.Object,
            logger ?? Mock.Of<ILogger<AssetsController>>(),
            new SiteIconBuilder(Mock.Of<ILogger<SiteIconBuilder>>(), _queryMediator.Object, _environment.Object),
            commandMediator ?? _commandMediator);

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

    private static byte[] CreatePngBytes(int width, int height, SKColor? color = null)
    {
        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(color ?? SKColors.Blue);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data!.ToArray();
    }

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

[CollectionDefinition("Site icon cache", DisableParallelization = true)]
public class SiteIconCacheCollection
{
}
