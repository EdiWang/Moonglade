using LiteBus.Events.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moonglade.ActivityLog;
using Moonglade.Configuration;
using Moonglade.Data.Entities;
using Moonglade.Features.Asset;
using Moonglade.ImageStorage;
using Moonglade.Setup;
using Moonglade.Web.Controllers;
using Moonglade.Web.Handlers;
using Moonglade.Web.Tests.TestSupport;
using Moq;
using SkiaSharp;

namespace Moonglade.Web.Tests.Controllers;

public partial class AssetsControllerTests
{
    private const string AvatarFileName = "avatar-0922e4.png";
    private const string OriginalAvatarUrl = "/assets/avatar";

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task Avatar_Post_WhenAuditFails_PreservesSavedAssetAndCdnSettings(bool enableCdn, bool canceledAudit)
    {
        var config = CreateAvatarConfig(enableCdn);
        using var mediator = new ConfigurationCommandRecorder();
        mediator.Initialize(config);
        Exception failure = canceledAudit
            ? new OperationCanceledException("Audit dispatch canceled.")
            : new InvalidOperationException("Audit dispatch unavailable.");
        mediator.ActivityLogFailure = failure;
        var files = new Dictionary<string, byte[]> { [AvatarFileName] = [1, 2, 3] };
        var storage = CreateAvatarStorage(files);
        ConfigureAssetHandlers(mediator, config, storage.Object);
        var logger = new Mock<ILogger<AssetsController>>();
        var controller = CreateController("avatar-admin", commandMediator: mediator, logger: logger.Object);
        controller.HttpContext.TraceIdentifier = "avatar-audit-trace";
        var bytes = CreatePngBytes(300, 300);
        var base64 = Convert.ToBase64String(bytes);
        mediator.Db.BlogAsset.Add(new BlogAssetEntity
        {
            Id = AssetId.AvatarBase64,
            Base64Data = Convert.ToBase64String([1, 2, 3]),
            LastModifiedTimeUtc = DateTime.UtcNow
        });
        await mediator.Db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await controller.Avatar(base64);

        Assert.IsType<OkResult>(result);
        Assert.Equal(base64, (await mediator.Db.BlogAsset.AsNoTracking().SingleAsync(
            p => p.Id == AssetId.AvatarBase64, TestContext.Current.CancellationToken)).Base64Data);
        var saved = await ReadAvatarGeneralSettings(mediator);
        Assert.Equal("Avatar test site", saved.SiteTitle);
        Assert.Equal(saved.ToJson(), config.GeneralSettings.ToJson());
        if (enableCdn)
        {
            Assert.Equal(bytes, Assert.Single(files).Value);
            var url = new Uri(saved.AvatarUrl);
            Assert.Equal($"https://cdn.example.com/images/{AvatarFileName}", url.GetLeftPart(UriPartial.Path));
            Assert.InRange(int.Parse(url.Query[1..]), 100, 998);
            storage.Verify(p => p.DeleteAsync(AvatarFileName), Times.Once);
            storage.Verify(p => p.InsertAsync(AvatarFileName, It.Is<byte[]>(data => data.SequenceEqual(bytes))), Times.Once);
            Assert.Collection(mediator.Commands,
                command => Assert.IsType<UpdateConfigurationCommand>(command),
                command => Assert.IsType<CreateActivityLogCommand>(command));
        }
        else
        {
            Assert.Equal(OriginalAvatarUrl, saved.AvatarUrl);
            Assert.Equal(new byte[] { 1, 2, 3 }, files[AvatarFileName]);
            storage.VerifyNoOtherCalls();
            Assert.IsType<CreateActivityLogCommand>(Assert.Single(mediator.Commands));
        }
        var audit = mediator.Single<CreateActivityLogCommand>();
        Assert.Equal(EventType.AvatarUpdated, audit.EventType);
        Assert.Equal("Update Avatar", audit.Operation);
        Assert.Equal("Avatar Image", audit.TargetName);
        Assert.Equal("avatar-admin", audit.ActorId);
        Assert.Null(audit.MetaData);
        AssertAvatarAuditFailure(logger, controller, failure, base64, saved.AvatarUrl, saved.SiteTitle, audit.ActorId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Avatar_Post_WhenSaveEventFails_PropagatesFailureWithoutAuditing(bool canceledSave)
    {
        Exception failure = canceledSave
            ? new OperationCanceledException("Asset event canceled.")
            : new InvalidOperationException("Asset event failed.");
        _eventMediator.Setup(p => p.PublishAsync(
                It.IsAny<SaveAssetEvent>(), It.IsAny<EventMediationSettings>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(failure);
        var logger = new Mock<ILogger<AssetsController>>();
        var controller = CreateController(logger: logger.Object);

        Assert.Same(failure, await Record.ExceptionAsync(() => controller.Avatar(Convert.ToBase64String(CreatePngBytes(300, 300)))));

        _eventMediator.Verify(p => p.PublishAsync(
            It.IsAny<SaveAssetEvent>(), It.IsAny<EventMediationSettings>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.Empty(_commandMediator.Commands);
        AssertNoAssetAuditError(logger);
    }

    [Fact]
    public async Task Avatar_Post_WhenAssetPersistenceFails_PropagatesFailureWithoutCdnOrAuditing()
    {
        var config = CreateAvatarConfig();
        using var mediator = new ConfigurationCommandRecorder();
        mediator.Initialize(config);
        var storage = new Mock<IBlogImageStorage>(MockBehavior.Strict);
        ConfigureAssetHandlers(mediator, config, storage.Object);
        var logger = new Mock<ILogger<AssetsController>>();
        var controller = CreateController(commandMediator: mediator, logger: logger.Object);
        await mediator.Db.Database.ExecuteSqlRawAsync(
            "CREATE TRIGGER reject_avatar_asset BEFORE INSERT ON BlogAsset BEGIN SELECT RAISE(ABORT, 'Asset save rejected'); END;",
            TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<DbUpdateException>(() => controller.Avatar(Convert.ToBase64String(CreatePngBytes(300, 300))));

        Assert.Empty(await mediator.Db.BlogAsset.AsNoTracking().ToListAsync(TestContext.Current.CancellationToken));
        Assert.Equal(OriginalAvatarUrl, (await ReadAvatarGeneralSettings(mediator)).AvatarUrl);
        Assert.Equal(OriginalAvatarUrl, config.GeneralSettings.AvatarUrl);
        storage.VerifyNoOtherCalls();
        Assert.Empty(mediator.Commands);
        AssertNoAssetAuditError(logger);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Avatar_Post_WhenStorageFails_PropagatesFailureWithoutSavingSettingsOrAuditing(bool failDelete)
    {
        var config = CreateAvatarConfig();
        using var mediator = new ConfigurationCommandRecorder();
        mediator.Initialize(config);
        var originalGeneral = config.GeneralSettings;
        var failure = new IOException("Avatar storage unavailable.");
        var files = new Dictionary<string, byte[]> { [AvatarFileName] = [1, 2, 3] };
        var storage = CreateAvatarStorage(files);
        if (failDelete)
            storage.Setup(p => p.DeleteAsync(AvatarFileName)).ThrowsAsync(failure);
        else
            storage.Setup(p => p.InsertAsync(AvatarFileName, It.IsAny<byte[]>())).ThrowsAsync(failure);
        ConfigureAssetHandlers(mediator, config, storage.Object);
        var logger = new Mock<ILogger<AssetsController>>();
        var controller = CreateController(commandMediator: mediator, logger: logger.Object);
        var base64 = Convert.ToBase64String(CreatePngBytes(300, 300));

        Assert.Same(failure, await Assert.ThrowsAsync<IOException>(() => controller.Avatar(base64)));

        Assert.Equal(base64, (await mediator.Db.BlogAsset.AsNoTracking().SingleAsync(
            TestContext.Current.CancellationToken)).Base64Data);
        Assert.Same(originalGeneral, config.GeneralSettings);
        Assert.Equal(originalGeneral.ToJson(), (await ReadAvatarGeneralSettings(mediator)).ToJson());
        Assert.Empty(mediator.Commands);
        storage.Verify(p => p.DeleteAsync(AvatarFileName), Times.Once);
        storage.Verify(p => p.InsertAsync(AvatarFileName, It.IsAny<byte[]>()), failDelete ? Times.Never : Times.Once);
        if (failDelete)
            Assert.Equal(new byte[] { 1, 2, 3 }, files[AvatarFileName]);
        else
            Assert.Empty(files);
        AssertNoAssetAuditError(logger);
    }

    [Fact]
    public async Task Avatar_Post_WhenSettingsPersistenceFails_PropagatesFailureWithoutAuditing()
    {
        var config = CreateAvatarConfig();
        using var mediator = new ConfigurationCommandRecorder();
        mediator.Initialize(config);
        var originalGeneral = config.GeneralSettings;
        var files = new Dictionary<string, byte[]>();
        var storage = CreateAvatarStorage(files);
        ConfigureAssetHandlers(mediator, config, storage.Object);
        var logger = new Mock<ILogger<AssetsController>>();
        var controller = CreateController(commandMediator: mediator, logger: logger.Object);
        var bytes = CreatePngBytes(300, 300);
        var base64 = Convert.ToBase64String(bytes);
        await mediator.Db.Database.ExecuteSqlRawAsync(
            "CREATE TRIGGER reject_avatar_settings BEFORE UPDATE ON BlogConfiguration BEGIN SELECT RAISE(ABORT, 'Settings save rejected'); END;",
            TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<DbUpdateException>(() => controller.Avatar(base64));

        Assert.Equal(base64, (await mediator.Db.BlogAsset.AsNoTracking().SingleAsync(
            TestContext.Current.CancellationToken)).Base64Data);
        Assert.Equal(bytes, files[AvatarFileName]);
        Assert.Same(originalGeneral, config.GeneralSettings);
        Assert.Equal(originalGeneral.ToJson(), (await ReadAvatarGeneralSettings(mediator)).ToJson());
        Assert.IsType<UpdateConfigurationCommand>(Assert.Single(mediator.Commands));
        AssertNoAssetAuditError(logger);
    }

    [Fact]
    public async Task UpdateSiteIcon_WhenAuditFails_StillPropagatesFailureAfterSavingAssetAndRegeneratingIcons()
    {
        var config = CreateAvatarConfig();
        using var mediator = new ConfigurationCommandRecorder();
        mediator.Initialize(config);
        var failure = new InvalidOperationException("Site icon audit unavailable.");
        mediator.ActivityLogFailure = failure;
        var storage = new Mock<IBlogImageStorage>(MockBehavior.Strict);
        ConfigureAssetHandlers(mediator, config, storage.Object);
        var logger = new Mock<ILogger<AssetsController>>();
        var controller = CreateController(commandMediator: mediator, logger: logger.Object);
        var base64 = Convert.ToBase64String(CreatePngBytes(64, 64));

        Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => controller.UpdateSiteIcon(base64)));

        Assert.Equal(base64, (await mediator.Db.BlogAsset.AsNoTracking().SingleAsync(
            p => p.Id == AssetId.SiteIconBase64, TestContext.Current.CancellationToken)).Base64Data);
        using var bitmap = SKBitmap.Decode(InMemoryIconGenerator.GetIcon("favicon-32x32.png"));
        Assert.Equal(32, bitmap.Width);
        Assert.Equal(32, bitmap.Height);
        Assert.Equal(SKColors.Blue, bitmap.GetPixel(16, 16));
        Assert.Equal(EventType.SiteIconUpdated, Assert.IsType<CreateActivityLogCommand>(Assert.Single(mediator.Commands)).EventType);
        Assert.Equal(OriginalAvatarUrl, (await ReadAvatarGeneralSettings(mediator)).AvatarUrl);
        storage.VerifyNoOtherCalls();
        AssertNoAssetAuditError(logger);
    }

    private void ConfigureAssetHandlers(ConfigurationCommandRecorder mediator, BlogConfig config, IBlogImageStorage storage)
    {
        var assetHandler = new SaveAssetEventHandler(mediator.Db);
        var cdnHandler = new SaveAssetToCdnHandler(Mock.Of<ILogger<SaveAssetToCdnHandler>>(), storage, config, mediator);
        _eventMediator.Setup(p => p.PublishAsync(
                It.IsAny<SaveAssetEvent>(), It.IsAny<EventMediationSettings>(), It.IsAny<CancellationToken>()))
            .Returns(async (SaveAssetEvent request, EventMediationSettings? settings, CancellationToken cancellationToken) =>
            {
                await assetHandler.HandleAsync(request, cancellationToken);
                await cdnHandler.HandleAsync(request, cancellationToken);
            });
    }

    private static BlogConfig CreateAvatarConfig(bool enableCdn = true)
    {
        var general = GeneralSettings.DefaultValue;
        general.SiteTitle = "Avatar test site";
        general.AvatarUrl = OriginalAvatarUrl;
        return new BlogConfig
        {
            GeneralSettings = general,
            ImageSettings = new ImageSettings { EnableCDNRedirect = enableCdn, CDNEndpoint = "https://cdn.example.com/images" }
        };
    }

    private static Mock<IBlogImageStorage> CreateAvatarStorage(Dictionary<string, byte[]> files)
    {
        var storage = new Mock<IBlogImageStorage>();
        storage.Setup(p => p.DeleteAsync(It.IsAny<string>()))
            .Callback((string fileName) => files.Remove(fileName))
            .Returns(Task.CompletedTask);
        storage.Setup(p => p.InsertAsync(It.IsAny<string>(), It.IsAny<byte[]>()))
            .ReturnsAsync((string fileName, byte[] bytes) =>
            {
                files[fileName] = bytes.ToArray();
                return fileName;
            });
        return storage;
    }

    private static async Task<GeneralSettings> ReadAvatarGeneralSettings(ConfigurationCommandRecorder mediator) =>
        (await mediator.Db.BlogConfiguration.AsNoTracking().SingleAsync(
            p => p.CfgKey == nameof(GeneralSettings), TestContext.Current.CancellationToken)).CfgValue.FromJson<GeneralSettings>();

    private static void AssertAvatarAuditFailure(Mock<ILogger<AssetsController>> logger, AssetsController controller,
        Exception failure, params string[] excludedValues)
    {
        var invocation = Assert.Single(logger.Invocations,
            p => p.Method.Name == nameof(ILogger.Log) && Equals(p.Arguments[0], LogLevel.Error));
        Assert.Same(failure, invocation.Arguments[3]);
        var state = Assert.IsAssignableFrom<IReadOnlyList<KeyValuePair<string, object?>>>(invocation.Arguments[2]);
        Assert.Equal(new[] { "EventType", "Operation", "TraceIdentifier", "{OriginalFormat}" }, state.Select(p => p.Key));
        Assert.Contains(state, p => p.Key == "EventType" && Equals(p.Value, EventType.AvatarUpdated));
        Assert.Contains(state, p => p.Key == "Operation" && Equals(p.Value, "Update Avatar"));
        Assert.Contains(state, p => p.Key == "TraceIdentifier" && Equals(p.Value, controller.HttpContext.TraceIdentifier));
        var message = invocation.Arguments[2]?.ToString() ?? string.Empty;
        Assert.Contains("Changes saved, but activity logging failed", message);
        Assert.DoesNotContain("Avatar Image", message);
        foreach (var value in excludedValues)
            Assert.DoesNotContain(value, message);
    }

    private static void AssertNoAssetAuditError(Mock<ILogger<AssetsController>> logger) =>
        Assert.DoesNotContain(logger.Invocations,
            p => p.Method.Name == nameof(ILogger.Log) && Equals(p.Arguments[0], LogLevel.Error));
}
