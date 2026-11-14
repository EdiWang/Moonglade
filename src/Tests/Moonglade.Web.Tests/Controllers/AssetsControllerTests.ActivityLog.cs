using LiteBus.Events.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moonglade.ActivityLog;
using Moonglade.Auth;
using Moonglade.Configuration;
using Moonglade.Features.Asset;
using Moonglade.Setup;
using Moonglade.Web.Controllers;
using Moonglade.Web.Tests.TestSupport;
using Moq;
using SkiaSharp;

namespace Moonglade.Web.Tests.Controllers;

public partial class AssetsControllerTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task Avatar_Post_WhenAuditFails_PreservesSavedAvatarWithoutChangingSettings(bool enableCdn, bool canceledAudit)
    {
        var config = CreateAvatarConfig(enableCdn);
        var original = config.Capture();
        using var mediator = new ConfigurationCommandRecorder();
        mediator.Initialize(config);
        Exception failure = canceledAudit
            ? new OperationCanceledException("Audit dispatch canceled.")
            : new InvalidOperationException("Audit dispatch unavailable.");
        mediator.ActivityLogFailure = failure;
        var logger = new Mock<ILogger<OwnerAvatarController>>();
        var controller = CreateAvatarController(mediator, logger.Object, "avatar-admin");
        controller.HttpContext.TraceIdentifier = "avatar-audit-trace";
        var base64 = Convert.ToBase64String(CreatePngBytes(300, 300));
        var ct = TestContext.Current.CancellationToken;
        await new OwnerAvatarStore(mediator.Db).SetGravatarAsync(true, ct);
        var rowsBefore = await mediator.Db.BlogConfiguration.AsNoTracking().OrderBy(row => row.CfgKey).ToListAsync(TestContext.Current.CancellationToken);

        Assert.IsType<OkResult>(await controller.Upload(base64, TestContext.Current.CancellationToken));

        var avatar = await mediator.Db.OwnerAvatar.AsNoTracking().SingleAsync(ct);
        Assert.Equal(base64, avatar.ImageBase64);
        Assert.True(avatar.UseGravatar);
        Assert.Same(original, config.Capture());
        var rowsAfter = await mediator.Db.BlogConfiguration.AsNoTracking().OrderBy(row => row.CfgKey).ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(rowsBefore.Select(row => (row.CfgKey, row.CfgValue, row.LastModifiedTimeUtc)),
            rowsAfter.Select(row => (row.CfgKey, row.CfgValue, row.LastModifiedTimeUtc)));
        var audit = Assert.IsType<CreateActivityLogCommand>(Assert.Single(mediator.Commands));
        Assert.Equal(ActivityEvents.AvatarUpdated, audit.EventKey);
        Assert.Equal("Avatar Image", audit.TargetDisplayName);
        Assert.Null(audit.TargetId);
        Assert.Equal("avatar-admin", audit.ActorDisplayName);
        Assert.Null(audit.Details);
        AssertAvatarAuditFailure(logger, controller, failure, base64, audit.ActorDisplayName!);
    }

    [Fact]
    public async Task Avatar_Post_WhenAvatarPersistenceFails_PropagatesFailureWithoutAuditing()
    {
        var config = CreateAvatarConfig();
        using var mediator = new ConfigurationCommandRecorder();
        mediator.Initialize(config);
        var logger = new Mock<ILogger<OwnerAvatarController>>();
        var controller = CreateAvatarController(mediator, logger.Object);
        await mediator.Db.Database.ExecuteSqlRawAsync(
            "CREATE TRIGGER reject_avatar BEFORE INSERT ON OwnerAvatar BEGIN SELECT RAISE(ABORT, 'Asset save rejected'); END;",
            TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<DbUpdateException>(() => controller.Upload(Convert.ToBase64String(CreatePngBytes(300, 300)), TestContext.Current.CancellationToken));

        Assert.Empty(await mediator.Db.OwnerAvatar.AsNoTracking().ToListAsync(TestContext.Current.CancellationToken));
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
        var controller = CreateAvatarController(mediator);
        var base64 = Convert.ToBase64String(CreatePngBytes(300, 300));
        await mediator.Db.Database.ExecuteSqlRawAsync(
            "CREATE TRIGGER reject_avatar_settings BEFORE UPDATE ON BlogConfiguration BEGIN SELECT RAISE(ABORT, 'Settings save rejected'); END;",
            TestContext.Current.CancellationToken);

        Assert.IsType<OkResult>(await controller.Upload(base64, TestContext.Current.CancellationToken));

        Assert.Equal(base64, (await mediator.Db.OwnerAvatar.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken)).ImageBase64);
        Assert.Same(original, config.Capture());
        Assert.Equal(original.GeneralSettings.ToJson(), (await ReadAvatarGeneralSettings(mediator)).ToJson());
        Assert.IsType<CreateActivityLogCommand>(Assert.Single(mediator.Commands));
    }

    [Fact]
    public async Task UpdateSiteIcon_WhenAuditFails_PreservesSuccessAfterSavingAssetAndRegeneratingIcons()
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

        Assert.IsType<NoContentResult>(await controller.UpdateSiteIcon(base64));

        Assert.Equal(base64, (await mediator.Db.BlogAsset.AsNoTracking().SingleAsync(
            p => p.Id == AssetId.SiteIconBase64, TestContext.Current.CancellationToken)).Base64Data);
        using var bitmap = SKBitmap.Decode(InMemoryIconGenerator.GetIcon("favicon-32x32.png"));
        Assert.Equal(32, bitmap.Width);
        Assert.Equal(32, bitmap.Height);
        Assert.Equal(SKColors.Blue, bitmap.GetPixel(16, 16));
        Assert.Equal(ActivityEvents.SiteIconUpdated, Assert.IsType<CreateActivityLogCommand>(Assert.Single(mediator.Commands)).EventKey);
        Assert.Equal(config.Capture().GeneralSettings, await ReadAvatarGeneralSettings(mediator));
        AssertAvatarAuditFailure(logger, controller, failure, base64);
    }

    private OwnerAvatarController CreateAvatarController(ConfigurationCommandRecorder store,
        ILogger<OwnerAvatarController>? logger = null, string? username = null,
        LiteBus.Commands.Abstractions.ICommandMediator? commandMediator = null) =>
        new(new OwnerAvatarStore(store.Db), _environment.Object, _cache.Object, _avatarCache,
            commandMediator ?? store, logger ?? Mock.Of<ILogger<OwnerAvatarController>>())
        {
            ControllerContext = CreateController(username).ControllerContext
        };

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
        return new BlogConfig(new BlogSettingsSnapshot
        {
            GeneralSettings = general,
            ImageSettings = new ImageSettings { EnableCDNRedirect = enableCdn, CDNEndpoint = "https://cdn.example.com/images" }
        });
    }

    private static async Task<GeneralSettings> ReadAvatarGeneralSettings(ConfigurationCommandRecorder mediator) =>
        (await mediator.Db.BlogConfiguration.AsNoTracking().SingleAsync(
            p => p.CfgKey == nameof(GeneralSettings), TestContext.Current.CancellationToken)).CfgValue.FromJson<GeneralSettings>();

    private static void AssertAvatarAuditFailure<T>(Mock<ILogger<T>> logger, ControllerBase controller,
        Exception failure, params string[] excludedValues)
    {
        var invocation = Assert.Single(logger.Invocations,
            p => p.Method.Name == nameof(ILogger.Log) && Equals(p.Arguments[0], LogLevel.Error));
        Assert.Null(invocation.Arguments[3]);
        var state = Assert.IsAssignableFrom<IReadOnlyList<KeyValuePair<string, object?>>>(invocation.Arguments[2]);
        Assert.Equal(new[] { "EventKey", "FailureType", "TraceIdentifier", "{OriginalFormat}" }, state.Select(p => p.Key));
        Assert.Contains(state, p => p.Key == "EventKey" && ActivityEvents.IsSupported(p.Value?.ToString()));
        Assert.Contains(state, p => p.Key == "FailureType" && Equals(p.Value, failure.GetType().Name));
        Assert.Contains(state, p => p.Key == "TraceIdentifier" && Equals(p.Value, controller.HttpContext.TraceIdentifier));
        var message = invocation.Arguments[2]?.ToString() ?? string.Empty;
        Assert.Contains("Changes saved, but activity logging failed", message);
        Assert.DoesNotContain("Avatar Image", message);
        foreach (var value in excludedValues)
            Assert.DoesNotContain(value, message);
    }

    private static void AssertNoAssetAuditError(Mock<ILogger<OwnerAvatarController>> logger) =>
        Assert.DoesNotContain(logger.Invocations,
            p => p.Method.Name == nameof(ILogger.Log) && Equals(p.Arguments[0], LogLevel.Error));
}
