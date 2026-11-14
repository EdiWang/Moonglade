using Edi.CacheAside.InMemory;
using LiteBus.Commands.Abstractions;
using LiteBus.Events.Abstractions;
using LiteBus.Queries.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moonglade.Auth;
using Moonglade.Configuration;
using Moonglade.Email.Core;
using Moonglade.Web.Controllers;
using Moonglade.Web.Tests.TestSupport;
using Moq;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Moonglade.Web.Tests.Controllers;

public partial class SettingsValidationHttpTests
{
    [Theory]
    [InlineData("content", """{"postListPageSize":0}""", "PostListPageSize")]
    [InlineData("subscription", """{"feedItemCount":-1}""", "FeedItemCount")]
    [InlineData("advanced", """{"headScripts":"<p>private-marker</p>"}""", "HeadScripts")]
    [InlineData("appearance", """{"enableCustomCss":true,"cssCode":""}""", "CssCode")]
    [InlineData("custom-menu", """{"isEnabled":true,"menuJson":"[null]"}""", "Menus[0]")]
    [InlineData("custom-menu", """{"isEnabled":true,"menuJson":"null"}""", "Menus")]
    public async Task InvalidSettings_ReturnValidationProblemAndPreserveRuntime(string route, string json, string field)
    {
        using var store = new ConfigurationCommandRecorder();
        var config = Defaults();
        var original = config.Capture().CustomMenuSettings;
        await using var app = await CreateApp(store, config);
        using var client = app.GetTestClient();

        using var response = await client.PostAsync($"/api/settings/{route}", new StringContent(json, Encoding.UTF8, "application/json"),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var problem = JsonDocument.Parse(body);
        Assert.True(problem.RootElement.GetProperty("errors").TryGetProperty(field, out _));
        Assert.DoesNotContain("private-marker", body);
        Assert.Same(original, config.Capture().CustomMenuSettings);
        Assert.Empty(store.Commands.OfType<Moonglade.ActivityLog.CreateActivityLogCommand>());
    }

    [Theory]
    [InlineData("general", """{"avatarUrl":"https://example.com"}""", "avatarUrl")]
    [InlineData("general", """{"canonicalPrefix":"https://example.com"}""", "canonicalPrefix")]
    [InlineData("advanced", """{"enableSiteMap":false}""", "enableSiteMap")]
    [InlineData("advanced", """{"robotsTxtContent":"User-agent: *"}""", "robotsTxtContent")]
    [InlineData("seo", """{"metaKeyword":""}""", "MetaKeyword")]
    [InlineData("seo", """{"indexNow":true}""", "indexNow")]
    [InlineData("subscription", """{"feedItemCount":null}""", "FeedItemCount")]
    [InlineData("subscription", """{"FeedItemCount":1,"feedItemCount":2}""", "FeedItemCount")]
    [InlineData("appearance", """{"enableCustomCss":true}""", "CssCode")]
    public async Task InvalidPatch_DoesNotPersistOrAudit(string route, string json, string field)
    {
        using var store = new ConfigurationCommandRecorder();
        var config = Defaults();
        var original = config.Capture();
        await using var app = await CreateApp(store, config);
        using var client = app.GetTestClient();
        var ct = TestContext.Current.CancellationToken;

        using var response = await client.PatchAsync($"/api/settings/{route}", new StringContent(json, Encoding.UTF8, "application/json"), ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        Assert.True(problem.RootElement.GetProperty("errors").TryGetProperty(field, out _));
        Assert.Same(original, config.Capture());
        Assert.Empty(store.Commands.OfType<Moonglade.ActivityLog.CreateActivityLogCommand>());
    }

    [Fact]
    public async Task Patch_EnablesCssWithContentAndPreservesTheme()
    {
        using var store = new ConfigurationCommandRecorder();
        var config = Defaults();
        var original = config.Capture().AppearanceSettings;
        await using var app = await CreateApp(store, config);
        using var client = app.GetTestClient();
        var ct = TestContext.Current.CancellationToken;
        const string css = "body { color: red; }";
        const string json = """{"enableCustomCss":true,"cssCode":"body { color: red; }"}""";

        using var response = await client.PatchAsync("/api/settings/appearance", new StringContent(json, Encoding.UTF8, "application/json"), ct);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var updated = config.Capture().AppearanceSettings;
        Assert.Equal(original.ThemeId, updated.ThemeId);
        Assert.True(updated.EnableCustomCss);
        Assert.Equal(css, updated.CssCode);
        Assert.Equal(updated.ToJson(), (await store.Db.BlogConfiguration.AsNoTracking().SingleAsync(
            row => row.CfgKey == nameof(AppearanceSettings), ct)).CfgValue);
        Assert.Single(store.Commands.OfType<Moonglade.ActivityLog.CreateActivityLogCommand>());
    }

    [Theory]
    [InlineData(5, true)]
    [InlineData(6, false)]
    public async Task MenuLimit_IsValidatedAtTheSharedWriteBoundary(int count, bool valid)
    {
        using var store = new ConfigurationCommandRecorder();
        var config = Defaults();
        await using var app = await CreateApp(store, config);
        using var client = app.GetTestClient();
        var request = new CustomMenuSettingsJsonModel
        {
            IsEnabled = true,
            MenuJson = Enumerable.Range(0, count).Select(_ => new Menu { Title = "Link" }).ToArray().ToJson()
        };

        using var response = await client.PostAsJsonAsync("/api/settings/custom-menu", request, TestContext.Current.CancellationToken);

        Assert.Equal(valid ? HttpStatusCode.NoContent : HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(valid ? count : 1, config.Capture().CustomMenuSettings.Menus.Count);
        if (!valid) Assert.Empty(store.Commands.OfType<Moonglade.ActivityLog.CreateActivityLogCommand>());
    }

    [Theory]
    [InlineData("{}", 0, false)]
    [InlineData("""{"feedItemCount":0,"useFullContent":false}""", 0, false)]
    [InlineData("""{"feedItemCount":5,"useFullContent":true}""", 5, true)]
    public async Task FeedRequest_BindsImmutableSettingsWithoutApplyingPersistedDefaults(string json, int count, bool full)
    {
        using var store = new ConfigurationCommandRecorder();
        var config = Defaults();
        var retained = config.Capture();
        await using var app = await CreateApp(store, config);
        using var client = app.GetTestClient();

        using var response = await client.PostAsync("/api/settings/subscription", new StringContent(json, Encoding.UTF8, "application/json"),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(count, config.Capture().FeedSettings.FeedItemCount);
        Assert.Equal(full, config.Capture().FeedSettings.UseFullContent);
        Assert.Equal(20, retained.FeedSettings.FeedItemCount);
        Assert.Equal(config.Capture().FeedSettings.ToJson(), (await store.Db.BlogConfiguration.AsNoTracking().SingleAsync(
            row => row.CfgKey == nameof(FeedSettings), TestContext.Current.CancellationToken)).CfgValue);
    }

    [Fact]
    public async Task BlankSenderAndDisabledMenu_CanBeSaved()
    {
        using var store = new ConfigurationCommandRecorder();
        var config = Defaults();
        await using var app = await CreateApp(store, config);
        using var client = app.GetTestClient();
        using var sender = await client.PostAsJsonAsync("/api/settings/notification", new NotificationSettings { EmailDisplayName = null },
            TestContext.Current.CancellationToken);
        using var menu = await client.PostAsJsonAsync("/api/settings/custom-menu", new CustomMenuSettingsJsonModel { IsEnabled = false },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, sender.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, menu.StatusCode);
        Assert.Null(config.Capture().NotificationSettings.EmailDisplayName);
        Assert.False(config.Capture().CustomMenuSettings.IsEnabled);
        Assert.Null(config.Capture().CustomMenuSettings.Menus);
    }

    private static BlogConfig Defaults()
    {
        return new BlogConfig(BlogConfig.Parse(new Dictionary<string, string>()).Snapshot);
    }

    [Theory]
    [InlineData(false, "[]")]
    [InlineData(true, "[]")]
    [InlineData(true, "[{\"title\":\"Parent\",\"url\":null,\"subMenus\":[{\"title\":\"Child\",\"url\":\"/child\",\"isOpenInNewTab\":true}]}]")]
    public async Task MenuEnvelope_SavesImmutableGraphWithLegacyJsonAndRetainsPreviousSnapshot(bool enabled, string json)
    {
        using var store = new ConfigurationCommandRecorder();
        var config = Defaults();
        var retained = config.Capture();
        var originalMenus = retained.CustomMenuSettings.ToJson();
        await using var app = await CreateApp(store, config);
        using var client = app.GetTestClient();

        using var response = await client.PostAsJsonAsync("/api/settings/custom-menu",
            new CustomMenuSettingsJsonModel { IsEnabled = enabled, MenuJson = json }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var current = config.Capture();
        Assert.NotSame(retained, current);
        Assert.Equal(originalMenus, retained.CustomMenuSettings.ToJson());
        using var actual = JsonDocument.Parse(current.CustomMenuSettings.ToJson());
        using var submitted = JsonDocument.Parse(json);
        Assert.Equal(enabled, actual.RootElement.GetProperty("isEnabled").GetBoolean());
        Assert.Equal(submitted.RootElement.GetArrayLength(), actual.RootElement.GetProperty("menus").GetArrayLength());
        Assert.Equal(current.CustomMenuSettings.ToJson(), (await store.Db.BlogConfiguration.AsNoTracking().SingleAsync(
            row => row.CfgKey == nameof(CustomMenuSettings), TestContext.Current.CancellationToken)).CfgValue);
        if (current.CustomMenuSettings.Menus.Count > 0)
        {
            Assert.Null(current.CustomMenuSettings.Menus[0].Url);
            var child = Assert.Single(current.CustomMenuSettings.Menus[0].SubMenus);
            Assert.Equal("/child", child.Url);
            Assert.True(child.IsOpenInNewTab);
        }
    }

    [Theory]
    [InlineData("general")]
    [InlineData("content")]
    [InlineData("comment")]
    [InlineData("notification")]
    [InlineData("subscription")]
    [InlineData("watermark")]
    [InlineData("advanced")]
    [InlineData("appearance")]
    [InlineData("custom-menu")]
    public async Task SettingsSave_WhenAuditDispatchFails_ReturnsNoContentOverHttp(string route)
    {
        using var store = new ConfigurationCommandRecorder { ActivityLogFailure = new InvalidOperationException("Audit unavailable.") };
        var config = Defaults();
        await using var app = await CreateApp(store, config);
        using var client = app.GetTestClient();
        object model = route switch
        {
            "general" => config.Capture().GeneralSettings with { SiteTitle = "Changed title" },
            "content" => config.Capture().ContentSettings with { PostListPageSize = 30 },
            "comment" => config.Capture().CommentSettings with { EnableComments = false },
            "notification" => config.Capture().NotificationSettings with { EnableEmailSending = true },
            "subscription" => config.Capture().FeedSettings with { FeedItemCount = 7 },
            "watermark" => config.Capture().ImageSettings with { KeepOriginImage = true },
            "advanced" => config.Capture().AdvancedSettings with { EnableOpml = false },
            "appearance" => config.Capture().AppearanceSettings with { ThemeId = 777 },
            "custom-menu" => new CustomMenuSettingsJsonModel { IsEnabled = false },
            _ => throw new ArgumentOutOfRangeException(nameof(route))
        };

        using var response = await client.PostAsJsonAsync($"/api/settings/{route}", model, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.Single(store.Commands.OfType<Moonglade.ActivityLog.CreateActivityLogCommand>());
    }

    private static async Task<WebApplication> CreateApp(ConfigurationCommandRecorder store, BlogConfig config, bool emailAvailable = false)
    {
        store.Initialize(config);
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Services.AddSingleton<IBlogConfig>(config);
        builder.Services.AddSingleton<IOwnerProfileReader>(store.Accounts);
        builder.Services.AddSingleton<ICommandMediator>(store);
        builder.Services.AddSingleton(Mock.Of<IQueryMediator>());
        builder.Services.AddSingleton(Mock.Of<IEventMediator>());
        builder.Services.AddSingleton(new EmailCapabilityStatusEvaluator(new EmailServiceOptionsValidator(), new EmailOutboxWorkerOptionsValidator())
            .Evaluate(emailAvailable ? new EmailServiceOptions
            {
                SmtpServer = "smtp.example.test",
                SmtpUserName = "sender@example.test",
                SmtpPassword = "synthetic-test-value"
            } : new EmailServiceOptions(), new EmailOutboxWorkerOptions()));
        builder.Services.AddInMemoryCacheAside();
        builder.Services.AddSingleton<PostCacheInvalidator>();
        builder.Services.AddControllers().AddApplicationPart(typeof(SettingsController).Assembly)
            .ConfigureApplicationPartManager(manager => manager.FeatureProviders.Add(new SettingsOnlyFeatureProvider()));
        builder.Services.AddAuthorization(options => options.AddPolicy(BlogAuthSchemas.AdministratorPolicy,
            policy => policy.RequireAssertion(_ => true)));
        var app = builder.Build();
        app.UseAuthorization();
        app.MapControllers();
        await app.StartAsync(TestContext.Current.CancellationToken);
        return app;
    }

    private sealed class SettingsOnlyFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
    {
        public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
        {
            foreach (var controller in feature.Controllers.Where(type => type.AsType() != typeof(SettingsController)).ToArray())
                feature.Controllers.Remove(controller);
        }
    }
}
