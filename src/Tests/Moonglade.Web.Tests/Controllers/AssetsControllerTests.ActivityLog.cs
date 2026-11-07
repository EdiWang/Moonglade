using LiteBus.Events.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moonglade.ActivityLog;
using Moonglade.Configuration;
using Moonglade.Data.Entities;
using Moonglade.Features.Asset;
using Moonglade.Setup;
using Moonglade.Web.Controllers;
using Moonglade.Web.Tests.TestSupport;
using Moq;
using SkiaSharp;

namespace Moonglade.Web.Tests.Controllers;

public partial class AssetsControllerTests
{
    private const string OriginalAvatarUrl = "https://legacy-cdn.example.com/avatar.png";

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task Avatar_Post_WhenAuditFails_PreservesSavedLocalAssetWithoutChangingSettings(bool enableCdn, bool canceledAudit)
    {
        var config = CreateAvatarConfig(enableCdn);
        var original = config.Capture();
        using var mediator = new ConfigurationCommandRecorder();
        mediator.Initialize(config);
        Exception failure = canceledAudit
            ? new OperationCanceledException("Audit dispatch canceled.")
            : new InvalidOperationException("Audit dispatch unavailable.");
        mediator.ActivityLogFailure = failure;
        ConfigureAssetHandler(mediator);
        var logger = new Mock<ILogger<AssetsController>>();
        var controller = CreateController("avatar-admin", commandMediator: mediator, logger: logger.Object);
        controller.HttpContext.TraceIdentifier = "avatar-audit-trace";
        var base64 = Convert.ToBase64String(CreatePngBytes(300, 300));
        mediator.Db.BlogAsset.Add(new BlogAssetEntity
        {
            Id = AssetId.AvatarBase64, Base64Data = Convert.ToBase64String([1, 2, 3]), LastModifiedTimeUtc = DateTime.UtcNow
        });
        await mediator.Db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var rowsBefore = await mediator.Db.BlogConfiguration.AsNoTracking().OrderBy(row => row.CfgKey).ToListAsync(TestContext.Current.CancellationToken);

        Assert.IsType<OkResult>(await controller.Avatar(base64));

        Assert.Equal(base64, (await mediator.Db.BlogAsset.AsNoTracking().SingleAsync(
            p => p.Id == AssetId.AvatarBase64, TestContext.Current.CancellationToken)).Base64Data);
        Assert.Same(original, config.Capture());
        var rowsAfter = await mediator.Db.BlogConfiguration.AsNoTracking().OrderBy(row => row.CfgKey).ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(rowsBefore.Select(row => (row.CfgKey, row.CfgValue, row.LastModifiedTimeUtc)),
            rowsAfter.Select(row => (row.CfgKey, row.CfgValue, row.LastModifiedTimeUtc)));
        var audit = Assert.IsType<CreateActivityLogCommand>(Assert.Single(mediator.Commands));
        Assert.Equal(EventType.AvatarUpdated, audit.EventType);
        Assert.Equal("Update Avatar", audit.Operation);
        Assert.Equal("Avatar Image", audit.TargetName);
        Assert.Equal("avatar-admin", audit.ActorId);
        Assert.Null(audit.MetaData);
        AssertAvatarAuditFailure(logger, controller, failure, base64, OriginalAvatarUrl, audit.ActorId);
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
    public async Task Avatar_Post_WhenAssetPersistenceFails_PropagatesFailureWithoutAuditing()
    {
        var config = CreateAvatarConfig();
        using var mediator = new ConfigurationCommandRecorder();
        mediator.Initialize(config);
        ConfigureAssetHandler(mediator);
        var logger = new Mock<ILogger<AssetsController>>();
        var controller = CreateController(commandMediator: mediator, logger: logger.Object);
        await mediator.Db.Database.ExecuteSqlRawAsync(
            "CREATE TRIGGER reject_avatar_asset BEFORE INSERT ON BlogAsset BEGIN SELECT RAISE(ABORT, 'Asset save rejected'); END;",
            TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<DbUpdateException>(() => controller.Avatar(Convert.ToBase64String(CreatePngBytes(300, 300))));

        Assert.Empty(await mediator.Db.BlogAsset.AsNoTracking().ToListAsync(TestContext.Current.CancellationToken));
        Assert.Equal(OriginalAvatarUrl, (await ReadAvatarGeneralSettings(mediator)).AvatarUrl);
        Assert.Equal(OriginalAvatarUrl, config.Capture().GeneralSettings.AvatarUrl);
        Assert.Empty(mediator.Commands);
        AssertNoAssetAuditError(logger);
    }



    [Fact]
    public async Task Avatar_Post_DoesNotWriteSettingsEvenWhenThoseWritesWouldFail()
    {
        var config = CreateAvatarConfig();
        var original = config.Capture();
        using var mediator = new ConfigurationCommandRecorder();
        mediator.Initialize(config);
        ConfigureAssetHandler(mediator);
        var controller = CreateController(commandMediator: mediator);
        var base64 = Convert.ToBase64String(CreatePngBytes(300, 300));
        await mediator.Db.Database.ExecuteSqlRawAsync(
            "CREATE TRIGGER reject_avatar_settings BEFORE UPDATE ON BlogConfiguration BEGIN SELECT RAISE(ABORT, 'Settings save rejected'); END;",
            TestContext.Current.CancellationToken);

        Assert.IsType<OkResult>(await controller.Avatar(base64));

        Assert.Equal(base64, (await mediator.Db.BlogAsset.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken)).Base64Data);
        Assert.Same(original, config.Capture());
        Assert.Equal(original.GeneralSettings.ToJson(), (await ReadAvatarGeneralSettings(mediator)).ToJson());
        Assert.IsType<CreateActivityLogCommand>(Assert.Single(mediator.Commands));
    }

    [Fact]
    public async Task UpdateSiteIcon_WhenAuditFails_StillPropagatesFailureAfterSavingAssetAndRegeneratingIcons()
    {
        var config = CreateAvatarConfig();
        using var mediator = new ConfigurationCommandRecorder();
        mediator.Initialize(config);
        var failure = new InvalidOperationException("Site icon audit unavailable.");
        mediator.ActivityLogFailure = failure;
        ConfigureAssetHandler(mediator);
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
        AssertNoAssetAuditError(logger);
    }

    private void ConfigureAssetHandler(ConfigurationCommandRecorder mediator)
    {
        var assetHandler = new SaveAssetEventHandler(mediator.Db);
        _eventMediator.Setup(p => p.PublishAsync(
                It.IsAny<SaveAssetEvent>(), It.IsAny<EventMediationSettings>(), It.IsAny<CancellationToken>()))
            .Returns((SaveAssetEvent request, EventMediationSettings? settings, CancellationToken token) =>
                assetHandler.HandleAsync(request, token));
    }

    private static BlogConfig CreateAvatarConfig(bool enableCdn = true)
    {
        var general = GeneralSettings.DefaultValue;
        general = general with { SiteTitle = "Avatar test site" };
        general = general with { AvatarUrl = OriginalAvatarUrl };
        return new BlogConfig(new BlogSettingsSnapshot
        {
            GeneralSettings = general,
            ImageSettings = new ImageSettings { EnableCDNRedirect = enableCdn, CDNEndpoint = "https://cdn.example.com/images" }
        });
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
