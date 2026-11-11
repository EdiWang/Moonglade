using Edi.CacheAside.InMemory;
using LiteBus.Commands.Abstractions;
using LiteBus.Events.Abstractions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moonglade.ActivityLog;
using Moonglade.BackgroundServices;
using Moonglade.Configuration;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Web.Controllers;
using Moonglade.Web.Handlers;
using Moonglade.Web.Tests.TestSupport;
using Moq;
using System.Net;
using System.Security.Claims;
using System.Text.Json;

namespace Moonglade.Web.Tests.Controllers;

public partial class SettingsControllerTests
{
    public static IEnumerable<object[]> SettingsChangeRoutes() =>
        SettingsActivityCases.Groups.SelectMany(group => group.Group == "custom-menu"
            ? new[] { new object[] { group.Group, false } }
            : new[] { new object[] { group.Group, false }, [group.Group, true] });

    [Theory]
    [MemberData(nameof(SettingsChangeRoutes))]
    public async Task NoOp_FullAndPatchAllGroups_ReturnNoContentWithoutAuditOrCache(string group, bool patch)
    {
        var config = new BlogConfig(BlogConfig.Parse(new Dictionary<string, string>()).Snapshot);
        using var mediator = new ConfigurationCommandRecorder { PersistActivityLogs = true };
        var controller = CreateController(config, mediator, new Mock<IAuthenticationService>());
        var before = config.Capture();
        var model = SettingsActivityCases.Get(before, group);
        var times = await mediator.Db.BlogConfiguration.AsNoTracking().ToDictionaryAsync(row => row.CfgKey, row => row.LastModifiedTimeUtc,
            TestContext.Current.CancellationToken);

        var result = await SaveController(controller, group, model, patch);

        Assert.IsType<NoContentResult>(result);
        Assert.Empty(mediator.Commands.OfType<CreateActivityLogCommand>());
        Assert.Empty(await mediator.Db.ActivityLog.AsNoTracking().ToListAsync(TestContext.Current.CancellationToken));
        Assert.Empty(Mock.Get(mediator.EventMediator).Invocations);
        Assert.NotSame(before, config.Capture());
        Assert.Equal(model.ToJson(), SettingsActivityCases.Get(config.Capture(), group).ToJson());
        Assert.Equal(times.OrderBy(row => row.Key), (await mediator.Db.BlogConfiguration.AsNoTracking().ToDictionaryAsync(
            row => row.CfgKey, row => row.LastModifiedTimeUtc, TestContext.Current.CancellationToken)).OrderBy(row => row.Key));
    }

    [Theory]
    [MemberData(nameof(SettingsChangeRoutes))]
    public async Task ChangedSave_AllGroups_PersistsSafeBeforeAfterAndPreservesOtherGroups(string group, bool patch)
    {
        var config = new BlogConfig(BlogConfig.Parse(new Dictionary<string, string>()).Snapshot);
        using var mediator = new ConfigurationCommandRecorder { PersistActivityLogs = true };
        var controller = CreateController(config, mediator, new Mock<IAuthenticationService>());
        var original = config.Capture();
        var before = SettingsActivityCases.Get(original, group);
        var retainedJson = before.ToJson();
        var after = ChangedGroup(original, group);
        var key = SafeKey(group);

        Assert.IsType<NoContentResult>(await SaveController(controller, group, after, patch));

        var row = await mediator.Db.ActivityLog.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(SettingsActivityCases.Groups.Single(item => item.Group == group).EventKey, row.EventKey);
        using var json = JsonDocument.Parse(row.Details!);
        Assert.Equal(["Changes"], json.RootElement.EnumerateObject().Select(property => property.Name));
        var change = Assert.Single(json.RootElement.GetProperty("Changes").EnumerateArray());
        Assert.Equal(["Key", "NewValue", "OldValue"], change.EnumerateObject().Select(property => property.Name).Order());
        Assert.Equal(key, change.GetProperty("Key").GetString());
        AssertJsonValue(before.GetType().GetProperty(key)!.GetValue(before), change.GetProperty("OldValue"));
        AssertJsonValue(after.GetType().GetProperty(key)!.GetValue(after), change.GetProperty("NewValue"));
        Assert.Equal(after.ToJson(), SettingsActivityCases.Get(config.Capture(), group).ToJson());
        Assert.Equal(after.ToJson(), await ReadStoredSettings(mediator, before.GetType().Name));
        Assert.Equal(retainedJson, SettingsActivityCases.Get(original, group).ToJson());
        foreach (var other in SettingsActivityCases.Groups.Where(item => item.Group != group))
            Assert.Same(SettingsActivityCases.Get(original, other.Group), SettingsActivityCases.Get(config.Capture(), other.Group));
        Assert.Single(Mock.Get(mediator.EventMediator).Invocations);
    }

    [Theory]
    [InlineData("general", false)]
    [InlineData("content", false)]
    [InlineData("comment", false)]
    [InlineData("notification", false)]
    [InlineData("watermark", false)]
    [InlineData("advanced", false)]
    [InlineData("appearance", false)]
    [InlineData("custom-menu", false)]
    [InlineData("general", true)]
    [InlineData("content", true)]
    [InlineData("comment", true)]
    [InlineData("notification", true)]
    [InlineData("watermark", true)]
    [InlineData("advanced", true)]
    [InlineData("seo", false)]
    [InlineData("seo", true)]
    [InlineData("appearance", true)]
    public async Task RestrictedOnly_FullAndPatch_WritesEmptyChangesWithoutRestrictedNamesOrValues(string group, bool patch)
    {
        var config = new BlogConfig(BlogConfig.Parse(new Dictionary<string, string>()).Snapshot);
        using var mediator = new ConfigurationCommandRecorder { PersistActivityLogs = true };
        var controller = CreateController(config, mediator, new Mock<IAuthenticationService>());
        var before = config.Capture();
        var after = RestrictedGroup(before, group);

        Assert.IsType<NoContentResult>(await SaveController(controller, group, after, patch));

        var row = await mediator.Db.ActivityLog.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal("""{"Changes":[]}""", row.Details);
        Assert.Equal(SettingsActivityCases.Groups.Single(item => item.Group == group).EventKey, row.EventKey);
        Assert.DoesNotContain("B3-private", row.Details);
        Assert.Equal(after.ToJson(), SettingsActivityCases.Get(config.Capture(), group).ToJson());
        Assert.Equal(after.ToJson(), await ReadStoredSettings(mediator, after.GetType().Name));
        Assert.Single(Mock.Get(mediator.EventMediator).Invocations);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MixedSave_PersistsOnlySafeChangedFieldWithoutProtectedValues(bool patch)
    {
        var config = new BlogConfig(BlogConfig.Parse(new Dictionary<string, string>()).Snapshot);
        using var mediator = new ConfigurationCommandRecorder { PersistActivityLogs = true };
        var controller = CreateController(config, mediator, new Mock<IAuthenticationService>());
        var after = config.Capture().GeneralSettings with
        {
            SiteTitle = "Changed title",
            Copyright = "[c] B3-private-description"
        };

        Assert.IsType<NoContentResult>(await SaveController(controller, "general", after, patch));

        var row = await mediator.Db.ActivityLog.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);
        using var details = JsonDocument.Parse(row.Details!);
        var change = Assert.Single(details.RootElement.GetProperty("Changes").EnumerateArray());
        Assert.Equal("SiteTitle", change.GetProperty("Key").GetString());
        Assert.Equal("Moonglade", change.GetProperty("OldValue").GetString());
        Assert.Equal("Changed title", change.GetProperty("NewValue").GetString());
        Assert.DoesNotContain("OwnerEmail", row.Details);
        Assert.DoesNotContain("Description", row.Details);
        Assert.DoesNotContain("B3-private", row.Details);
        Assert.Equal(after.ToJson(), await ReadStoredSettings(mediator, nameof(GeneralSettings)));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task AuditInsertFailure_KeepsCommittedSettingsTimestampAndCacheWithoutResurrection(bool patch, bool includeTrace)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var store = await ActivityLogTestStore.CreateAsync(ct: ct);
        using var databaseScope = store.Services.CreateScope();
        var db = databaseScope.ServiceProvider.GetRequiredService<BlogDbContext>();
        var services = new ServiceCollection();
        store.ConfigureServices(services);
        // LiteBus creates handler scopes; share this context to exercise failed-row cleanup and unrelated tracked work.
        services.AddSingleton(db);
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var config = store.Config;
        var time = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        db.BlogConfiguration.Add(new BlogConfigurationEntity
        {
            CfgKey = nameof(GeneralSettings),
            CfgValue = config.Capture().GeneralSettings.ToJson(),
            LastModifiedTimeUtc = time
        });
        await db.SaveChangesAsync(ct);
        var logger = new Mock<ILogger<SettingsController>>();
        const string trace = "b2-settings-insert-trace";
        var controller = new SettingsController(config, logger.Object,
            scope.ServiceProvider.GetRequiredService<IEventMediator>(),
            scope.ServiceProvider.GetRequiredService<Moonglade.Email.Core.EmailCapabilityStatus>(),
            scope.ServiceProvider.GetRequiredService<ICommandMediator>(),
            scope.ServiceProvider.GetRequiredService<Moonglade.Auth.IOwnerProfileReader>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    RequestServices = scope.ServiceProvider,
                    TraceIdentifier = includeTrace ? trace : string.Empty,
                    User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "B2-private-actor")], "TestAuth"))
                }
            }
        };
        controller.HttpContext.Connection.RemoteIpAddress = IPAddress.Parse("192.0.2.123");
        controller.Request.Headers.UserAgent = "B2-private-agent";
        var caches = scope.ServiceProvider.GetRequiredService<PostCacheInvalidator>();
        var feedKey = caches.GetFeedCacheKey("rss");
        var sitemapKey = caches.SiteMapCacheKey;
        var themes = scope.ServiceProvider.GetRequiredService<ThemeCacheInvalidator>();
        var themeKey = themes.GetCacheKey(100);
        await db.Database.ExecuteSqlRawAsync(
            "CREATE TRIGGER reject_b3_activity BEFORE INSERT ON ActivityLog BEGIN SELECT RAISE(ABORT, 'B3-private-insert-error'); END;", ct);
        var after = config.Capture().GeneralSettings with
        {
            SiteTitle = "Saved title",
            Copyright = "[c] B2-private-description"
        };
        var pending = new BlogConfigurationEntity
        {
            CfgKey = "B3Unrelated",
            CfgValue = "preserved",
            LastModifiedTimeUtc = DateTime.UtcNow
        };
        var stagePending = true;
        db.SavedChanges += (_, _) =>
        {
            if (!stagePending) return;
            stagePending = false;
            db.BlogConfiguration.Add(pending);
        };

        Assert.IsType<NoContentResult>(await SaveController(controller, "general", after, patch));

        Assert.Equal("Saved title", config.Capture().GeneralSettings.SiteTitle);
        var saved = await db.BlogConfiguration.AsNoTracking().SingleAsync(row => row.CfgKey == nameof(GeneralSettings), ct);
        Assert.Equal(after.ToJson(), saved.CfgValue);
        var savedTime = saved.LastModifiedTimeUtc;
        Assert.True(savedTime > time);
        Assert.NotEqual(feedKey, caches.GetFeedCacheKey("rss"));
        Assert.NotEqual(sitemapKey, caches.SiteMapCacheKey);
        Assert.Equal(themeKey, themes.GetCacheKey(100));
        Assert.Empty(await store.ReadLogsAsync(ct));
        Assert.Empty(db.ChangeTracker.Entries<ActivityLogEntity>());
        Assert.Equal(EntityState.Added, db.Entry(pending).State);
        Assert.False(await db.BlogConfiguration.AsNoTracking().AnyAsync(row => row.CfgKey == "B3Unrelated", ct));
        var diagnostic = Assert.Single(store.WriterLogger.Invocations, call => Equals(call.Arguments[0], LogLevel.Error));
        Assert.Null(diagnostic.Arguments[3]);
        var fields = Assert.IsAssignableFrom<IReadOnlyList<KeyValuePair<string, object?>>>(diagnostic.Arguments[2]);
        Assert.Equal(includeTrace ? ["EventKey", "FailureType", "TraceIdentifier", "{OriginalFormat}"] :
            new[] { "EventKey", "FailureType", "{OriginalFormat}" }, fields.Select(field => field.Key).ToArray());
        Assert.Contains(fields, field => field.Key == "EventKey" && Equals(field.Value, "settings.general.updated"));
        Assert.Contains(fields, field => field.Key == "FailureType" && Equals(field.Value, nameof(DbUpdateException)));
        if (includeTrace)
            Assert.Contains(fields, field => field.Key == "TraceIdentifier" && Equals(field.Value, trace));
        else
            Assert.Contains(fields, field => field.Key == "{OriginalFormat}" &&
                Equals(field.Value, "Activity logging failed for {EventKey}. Failure type: {FailureType}"));
        var diagnosticText = diagnostic.Arguments[2]!.ToString()!;
        foreach (var excluded in new[] { "B3-private", "B2-private", "Saved title", "192.0.2.123" })
            Assert.DoesNotContain(excluded, diagnosticText);
        Assert.DoesNotContain(logger.Invocations, call => Equals(call.Arguments[0], LogLevel.Error));
        await db.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_b3_activity;", ct);
        await db.SaveChangesAsync(ct);
        Assert.Equal("preserved", (await db.BlogConfiguration.AsNoTracking().SingleAsync(row => row.CfgKey == "B3Unrelated", ct)).CfgValue);
        Assert.Empty(await store.ReadLogsAsync(ct));
        Assert.Equal(savedTime, (await db.BlogConfiguration.AsNoTracking().SingleAsync(row => row.CfgKey == nameof(GeneralSettings), ct)).LastModifiedTimeUtc);

        Assert.IsType<NoContentResult>(await SaveController(controller, "general", after with { SiteTitle = "Later title" }, patch));
        var laterActivity = Assert.Single(await store.ReadLogsAsync(ct));
        Assert.Equal("""{"Changes":[{"Key":"SiteTitle","OldValue":"Saved title","NewValue":"Later title"}]}""", laterActivity.Details);
        Assert.DoesNotContain(trace, laterActivity.Details);
        Assert.DoesNotContain("B2-private", laterActivity.Details);
        Assert.Single(store.WriterLogger.Invocations, call => Equals(call.Arguments[0], LogLevel.Error));
        Assert.DoesNotContain(logger.Invocations, call => Equals(call.Arguments[0], LogLevel.Error));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AuditDispatchFailure_PreservesActualFeedAndSitemapInvalidation(bool canceled)
    {
        var ct = TestContext.Current.CancellationToken;
        var config = new BlogConfig(BlogConfig.Parse(new Dictionary<string, string>()).Snapshot);
        using var mediator = new ConfigurationCommandRecorder { ActivityLogFailure = CreateAuditFailure(canceled) };
        var controller = CreateController(config, mediator, new Mock<IAuthenticationService>());
        using var services = new ServiceCollection().AddInMemoryCacheAside().BuildServiceProvider();
        var posts = new PostCacheInvalidator(services.GetRequiredService<ICacheAside>());
        var themes = new ThemeCacheInvalidator();
        var scheduler = new ScheduledPublishWakeUp();
        var cacheHandler = new ConfigurationChangedEventHandler(posts, themes, scheduler);
        var feedKey = posts.GetFeedCacheKey("rss");
        var sitemapKey = posts.SiteMapCacheKey;
        var themeKey = themes.GetCacheKey(100);
        Mock.Get(mediator.EventMediator).Setup(events => events.PublishAsync(
                It.IsAny<IEvent>(), It.IsAny<EventMediationSettings>(), CancellationToken.None))
            .Returns<IEvent, EventMediationSettings, CancellationToken>((message, _, token) =>
                cacheHandler.HandleAsync(Assert.IsType<ConfigurationChangedEvent>(message), token));
        var row = await mediator.Db.BlogConfiguration.SingleAsync(item => item.CfgKey == nameof(GeneralSettings), ct);
        row.LastModifiedTimeUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        await mediator.Db.SaveChangesAsync(ct);
        var after = config.Capture().GeneralSettings with { SiteTitle = "Saved title" };

        Assert.IsType<NoContentResult>(await controller.General(after));

        Assert.NotEqual(feedKey, posts.GetFeedCacheKey("rss"));
        Assert.NotEqual(sitemapKey, posts.SiteMapCacheKey);
        Assert.Equal(themeKey, themes.GetCacheKey(100));
        Assert.Equal("Saved title", config.Capture().GeneralSettings.SiteTitle);
        Assert.Equal(after.ToJson(), await ReadStoredSettings(mediator, nameof(GeneralSettings)));
        Assert.True(row.LastModifiedTimeUtc > new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        Assert.Single(mediator.Commands.OfType<CreateActivityLogCommand>());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AccountNamedEvents_RealWriterStoresNoProtectedValuesAndPreservesSignOutPolicy(bool reset)
    {
        var ct = TestContext.Current.CancellationToken;
        var account = CreateLocalAccount();
        var config = new BlogConfig();
        using var mediator = new ConfigurationCommandRecorder { PersistActivityLogs = true };
        var authentication = new Mock<IAuthenticationService>();
        var controller = CreateController(account, mediator, authentication);

        var result = reset
            ? await controller.ResetLocalAccountTotp(new Moonglade.Auth.ResetLocalAccountTotpRequest { CurrentPassword = "Password1" })
            : await controller.UpdateLocalAccountPassword(new Moonglade.Auth.UpdateLocalAccountPasswordRequest
            { OldPassword = "Password1", NewPassword = "NewPassword2" });

        Assert.IsType<NoContentResult>(result);
        var row = await mediator.Db.ActivityLog.AsNoTracking().SingleAsync(ct);
        Assert.Equal(reset ? ActivityEvents.SettingsAuthenticatorReset : ActivityEvents.SettingsPasswordUpdated, row.EventKey);
        Assert.Null(row.Details);
        var detail = (await new GetMetaDataByActivityLogIdQueryHandler(mediator.Db).HandleAsync(new(row.Id), ct))!;
        Assert.Empty(detail.Details);
        if (reset)
        {
            foreach (var scheme in new[] { Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme,
                         Moonglade.Auth.BlogAuthSchemas.LocalAccountSetup, Moonglade.Auth.BlogAuthSchemas.LocalAccountTwoFactor })
                authentication.Verify(service => service.SignOutAsync(controller.HttpContext, scheme, null), Times.Once);
            Assert.False((await mediator.Accounts.ReadAsync(ct))!.IsTotpEnabled);
            Assert.Null((await mediator.Db.OwnerAccount.AsNoTracking().SingleAsync(ct)).TotpSecret);
        }
        else
        {
            authentication.Verify(service => service.SignOutAsync(It.IsAny<Microsoft.AspNetCore.Http.HttpContext>(),
                It.IsAny<string>(), It.IsAny<AuthenticationProperties>()), Times.Never);
            Assert.Equal(account.Email, (await mediator.Accounts.ReadAsync(ct))!.Email);
            Assert.True((await mediator.Accounts.ReadAsync(ct))!.IsTotpEnabled);
            Assert.Null(await mediator.Accounts.AuthenticateLocalAsync(account.Email, "Password1", ct));
            Assert.NotNull(await mediator.Accounts.AuthenticateLocalAsync(account.Email, "NewPassword2", ct));
        }
    }

    [Fact]
    public async Task PatchCacheFailure_PropagatesCommittedFailureWithoutFabricatingAudit()
    {
        var ct = TestContext.Current.CancellationToken;
        var config = new BlogConfig(BlogConfig.Parse(new Dictionary<string, string>()).Snapshot);
        using var mediator = new ConfigurationCommandRecorder();
        var controller = CreateController(config, mediator, new Mock<IAuthenticationService>());
        var failure = new InvalidOperationException("Synthetic cache failure");
        Mock.Get(mediator.EventMediator).Setup(events => events.PublishAsync(
            It.IsAny<IEvent>(), It.IsAny<EventMediationSettings>(), CancellationToken.None)).ThrowsAsync(failure);

        Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => controller.Patch("general",
            JsonSerializer.SerializeToElement(new Dictionary<string, string> { ["SiteTitle"] = "Saved title" }))));

        Assert.Equal("Saved title", config.Capture().GeneralSettings.SiteTitle);
        Assert.Equal(config.Capture().GeneralSettings.ToJson(), await ReadStoredSettings(mediator, nameof(GeneralSettings)));
        Assert.Empty(mediator.Commands.OfType<CreateActivityLogCommand>());
        Assert.Empty(await mediator.Db.ActivityLog.AsNoTracking().ToListAsync(ct));
    }

    private static string SafeKey(string group) => group switch
    {
        "general" => "SiteTitle",
        "content" => "PostListPageSize",
        "comment" => "EnableComments",
        "notification" => "EnableEmailSending",
        "subscription" => "FeedItemCount",
        "watermark" => "KeepOriginImage",
        "advanced" => "EnableOpml",
        "seo" => "EnableSiteMap",
        "appearance" => "ThemeId",
        _ => "IsEnabled"
    };

    private static object ChangedGroup(BlogSettingsSnapshot root, string group) => group switch
    {
        "general" => root.GeneralSettings with { SiteTitle = "Changed title" },
        "content" => root.ContentSettings with { PostListPageSize = 30 },
        "comment" => root.CommentSettings with { EnableComments = false },
        "notification" => root.NotificationSettings with { EnableEmailSending = true },
        "subscription" => root.FeedSettings with { FeedItemCount = 7 },
        "watermark" => root.ImageSettings with { KeepOriginImage = true },
        "advanced" => root.AdvancedSettings with { EnableOpml = false },
        "seo" => root.SEOSettings with { EnableSiteMap = false },
        "appearance" => root.AppearanceSettings with { ThemeId = 777 },
        _ => root.CustomMenuSettings with { IsEnabled = false }
    };

    private static object RestrictedGroup(BlogSettingsSnapshot root, string group) => group switch
    {
        "general" => root.GeneralSettings with { Copyright = "[c] B3-private-description" },
        "content" => root.ContentSettings with { PostFooterHtmlPitch = "B3-private-footer" },
        "comment" => root.CommentSettings with { WordFilterKeywords = "B3-private-keywords" },
        "notification" => root.NotificationSettings with { EmailDisplayName = "B3-private-sender" },
        "watermark" => root.ImageSettings with { WatermarkText = "B3-private-watermark" },
        "advanced" => root.AdvancedSettings with { HeadScripts = "<script>B3-private-head()</script>" },
        "seo" => root.SEOSettings with { RobotsTxtContent = "B3-private-robots" },
        "appearance" => root.AppearanceSettings with { CssCode = "B3-private-css" },
        _ => root.CustomMenuSettings with { Menus = [new Menu { Title = "B3-private-menu" }] }
    };

    private static Task<IActionResult> SaveController(SettingsController controller, string group, object model, bool patch) =>
        patch ? controller.Patch(group, JsonSerializer.SerializeToElement(model.GetType().GetProperties()
                .Where(property => property.GetMethod?.IsStatic == false)
                .ToDictionary(property => property.Name, property => property.GetValue(model))))
            : model switch
            {
                GeneralSettings value => controller.General(value),
                ContentSettings value => controller.Content(value),
                CommentSettings value => controller.Comment(value),
                NotificationSettings value => controller.Notification(value),
                FeedSettings value => controller.Subscription(value),
                ImageSettings value => controller.Image(value),
                AdvancedSettings value => controller.Advanced(value),
                SEOSettings value => controller.Seo(value),
                AppearanceSettings value => controller.Appearance(value),
                CustomMenuSettings value => controller.CustomMenu(new CustomMenuSettingsJsonModel { IsEnabled = value.IsEnabled, MenuJson = value.Menus.ToJson() }),
                _ => throw new InvalidOperationException("Unsupported test settings group.")
            };

    private static void AssertJsonValue(object? expected, JsonElement actual)
    {
        var value = expected is Enum ? expected.ToString() : expected;
        Assert.True(JsonElement.DeepEquals(JsonSerializer.SerializeToElement(value), actual),
            "The activity value must preserve the independently submitted scalar.");
    }
}
