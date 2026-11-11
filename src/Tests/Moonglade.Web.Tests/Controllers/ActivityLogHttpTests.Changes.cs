using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Moonglade.ActivityLog;
using Moonglade.Configuration;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Web.Tests.TestSupport;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Moonglade.Web.Tests.Controllers;

public partial class ActivityLogHttpTests
{
    public static IEnumerable<object[]> HttpSettingsRoutes() => SettingsActivityCases.Groups.SelectMany(group => group.Group == "custom-menu"
        ? new[] { new object[] { group.Group, false } }
        : new[] { new object[] { group.Group, false }, [group.Group, true] });

    [Theory]
    [MemberData(nameof(HttpSettingsRoutes))]
    public async Task SettingsHttp_FullAndPatch_Return204AndExactSafeChangeThenSuppressNoOp(string group, bool patch)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fixture = await ActivityLogHttpFixture.CreateAsync(0, ct, includeSettings: true);
        using var client = fixture.App.GetTestClient();
        client.DefaultRequestHeaders.Add("X-B1-User", "admin");
        await AddPageToken(client, ct);
        var before = fixture.Store.Config.Capture();
        object model = group switch
        {
            "general" => before.GeneralSettings with { SiteTitle = "Saved title" },
            "content" => before.ContentSettings with { PostListPageSize = 30 },
            "comment" => before.CommentSettings with { EnableComments = false },
            "notification" => before.NotificationSettings with { EnableEmailSending = true },
            "subscription" => before.FeedSettings with { FeedItemCount = 7 },
            "watermark" => before.ImageSettings with { KeepOriginImage = true },
            "advanced" => before.AdvancedSettings with { EnableOpml = false },
            "seo" => before.SEOSettings with { EnableSiteMap = false },
            "appearance" => before.AppearanceSettings with { ThemeId = 777 },
            _ => new CustomMenuSettingsJsonModel { IsEnabled = false, MenuJson = before.CustomMenuSettings.Menus.ToJson() }
        };
        var key = group switch
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
        var content = patch ? new Dictionary<string, object?> { [key] = model.GetType().GetProperty(key)!.GetValue(model) } : model;
        using var request = new HttpRequestMessage(patch ? HttpMethod.Patch : HttpMethod.Post, $"/api/settings/{group}")
        { Content = JsonContent.Create(content) };
        using var response = await client.SendAsync(request, ct);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsStringAsync(ct));
        var row = Assert.Single(await fixture.Store.ReadLogsAsync(ct));
        Assert.Equal(SettingsActivityCases.Groups.Single(item => item.Group == group).EventKey, row.EventKey);
        Assert.Null(row.TargetDisplayName);
        using var metadata = JsonDocument.Parse(await client.GetStringAsync($"/api/activitylog/{row.Id}/metadata", ct));
        Assert.Empty(metadata.RootElement.GetProperty("fields").EnumerateArray());
        var change = Assert.Single(metadata.RootElement.GetProperty("changes").EnumerateArray());
        Assert.Equal(key, change.GetProperty("key").GetString());
        var old = SettingsActivityCases.Get(before, group).GetType().GetProperty(key)!.GetValue(SettingsActivityCases.Get(before, group));
        Assert.True(JsonElement.DeepEquals(JsonSerializer.SerializeToElement(old), change.GetProperty("oldValue")));
        Assert.True(JsonElement.DeepEquals(JsonSerializer.SerializeToElement(model.GetType().GetProperty(key)!.GetValue(model)), change.GetProperty("newValue")));
        using var again = new HttpRequestMessage(patch ? HttpMethod.Patch : HttpMethod.Post, $"/api/settings/{group}")
        { Content = JsonContent.Create(content) };
        using var unchanged = await client.SendAsync(again, ct);
        Assert.Equal(HttpStatusCode.NoContent, unchanged.StatusCode);
        Assert.Empty(await unchanged.Content.ReadAsStringAsync(ct));
        Assert.Single(await fixture.Store.ReadLogsAsync(ct));
        Assert.Equal(SettingsActivityCases.Get(fixture.Store.Config.Capture(), group).ToJson(),
            await ReadConfiguration(fixture, SettingsActivityCases.Get(before, group).GetType().Name, ct));
    }

    [Theory]
    [InlineData(null, false, HttpStatusCode.Unauthorized)]
    [InlineData("reader", false, HttpStatusCode.Forbidden)]
    [InlineData("admin", false, HttpStatusCode.BadRequest)]
    [InlineData("admin-invalid", false, HttpStatusCode.BadRequest)]
    [InlineData(null, true, HttpStatusCode.Unauthorized)]
    [InlineData("reader", true, HttpStatusCode.Forbidden)]
    [InlineData("admin", true, HttpStatusCode.BadRequest)]
    [InlineData("admin-invalid", true, HttpStatusCode.BadRequest)]
    public async Task SettingsHttp_UnauthorizedOrMissingCsrf_PreservesStateWithoutAudit(string? user, bool patch, HttpStatusCode status)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fixture = await ActivityLogHttpFixture.CreateAsync(0, ct, includeSettings: true);
        using var client = fixture.App.GetTestClient();
        if (user != null) client.DefaultRequestHeaders.Add("X-B1-User", user == "admin-invalid" ? "admin" : user);
        if (user == "admin-invalid")
        {
            await AddPageToken(client, ct);
            client.DefaultRequestHeaders.Remove("XSRF-TOKEN");
            client.DefaultRequestHeaders.Add("XSRF-TOKEN", "invalid-token");
        }
        var original = fixture.Store.Config.Capture();
        var json = patch ? """{"feedItemCount":7}""" : """{"feedItemCount":7,"useFullContent":true}""";
        using var request = new HttpRequestMessage(patch ? HttpMethod.Patch : HttpMethod.Post, "/api/settings/subscription")
        { Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json") };

        using var response = await client.SendAsync(request, ct);

        Assert.Equal(status, response.StatusCode);
        Assert.Same(original, fixture.Store.Config.Capture());
        Assert.Equal(original.FeedSettings.ToJson(), await ReadConfiguration(fixture, nameof(FeedSettings), ct));
        Assert.Empty(await fixture.Store.ReadLogsAsync(ct));
    }

    [Theory]
    [InlineData("en-US")]
    [InlineData("zh-Hans")]
    [InlineData("zh-Hant")]
    [InlineData("de-DE")]
    [InlineData("ja-JP")]
    [InlineData("fr-FR")]
    public async Task SettingDetails_All51LabelsAreLocalizedAndOldNewValuesRemainTypedPlainText(string culture)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fixture = await ActivityLogHttpFixture.CreateAsync(0, ct);
        using var scope = fixture.App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();
        using var client = fixture.App.GetTestClient();
        client.DefaultRequestHeaders.Add("X-B1-User", "admin");
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd(culture);
        var labelCount = 0;
        foreach (var group in SettingsActivityCases.Groups)
        {
            var keys = group.Group == "general" ? group.Allowed.Append("UseGravatarAsProfilePicture") : group.Allowed;
            var changes = keys.Select(key => new ActivityLogSettingChange(key, "<before>", "<script>after</script>")).ToArray();
            await new CreateActivityLogCommandHandler(db, fixture.Store.WriterLogger.Object).HandleAsync(new CreateActivityLogCommand(
                group.EventKey, ActivityActorKind.Administrator, "admin", Details: ActivityLogDetails.Create(("Changes", changes))), ct);
            var row = db.ActivityLog.Local.OrderBy(item => item.Id).Last();
            using var details = JsonDocument.Parse(await client.GetStringAsync($"/api/activitylog/{row.Id}/metadata", ct));
            Assert.Empty(details.RootElement.GetProperty("fields").EnumerateArray());
            var actual = details.RootElement.GetProperty("changes").EnumerateArray().ToArray();
            Assert.Equal(keys, actual.Select(change => change.GetProperty("key").GetString()));
            foreach (var change in actual)
            {
                var key = change.GetProperty("key").GetString();
                var label = change.GetProperty("label").GetString();
                var english = SettingsActivityCases.Labels[key!];
                var previous = CultureInfo.CurrentUICulture;
                try
                {
                    CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
                    Assert.Equal(scope.ServiceProvider.GetRequiredService<IStringLocalizer<Program>>()[english].Value, label);
                }
                finally { CultureInfo.CurrentUICulture = previous; }
                if (culture != "en-US") Assert.NotEqual(english, label);
                Assert.Equal("<before>", change.GetProperty("oldValue").GetString());
                Assert.Equal("<script>after</script>", change.GetProperty("newValue").GetString());
                labelCount++;
            }
        }
        Assert.Equal(51, labelCount);
        using var page = await client.GetAsync("/admin/activitylogs", ct);
        var html = await page.Content.ReadAsStringAsync(ct);
        // Old/new values can contain markup; only text bindings are a security contract here.
        Assert.Contains("x-text=\"formatDetailValue(change.oldValue, change.key)\"", html);
        Assert.Contains("x-text=\"formatDetailValue(change.newValue, change.key)\"", html);
        Assert.DoesNotMatch("x-html=\"[^\"]*(change\\.|currentDetail\\.)", html);
    }

    [Fact]
    public async Task RestrictedOnlyHttp_HasEmptyChangesWhileNonSettingsKeepFields()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fixture = await ActivityLogHttpFixture.CreateAsync(0, ct, includeSettings: true);
        using var client = fixture.App.GetTestClient();
        client.DefaultRequestHeaders.Add("X-B1-User", "admin");
        await AddPageToken(client, ct);
        using var response = await client.PatchAsJsonAsync("/api/settings/general",
            new Dictionary<string, string> { ["Copyright"] = "[c] B3-private-copyright" }, ct);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var row = Assert.Single(await fixture.Store.ReadLogsAsync(ct));
        using var metadata = JsonDocument.Parse(await client.GetStringAsync($"/api/activitylog/{row.Id}/metadata", ct));
        Assert.Empty(metadata.RootElement.GetProperty("changes").EnumerateArray());
        Assert.Empty(metadata.RootElement.GetProperty("fields").EnumerateArray());
        Assert.DoesNotContain("OwnerEmail", metadata.RootElement.GetRawText());
        Assert.DoesNotContain("B3-private", metadata.RootElement.GetRawText());
        using var scope = fixture.App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();
        var ordinary = new ActivityLogEntity
        {
            EventKey = ActivityEvents.ActivityLogsCleared,
            EventTimeUtc = DateTime.UtcNow,
            ActorKind = ActivityActorKind.Administrator,
            Details = """{"DeletedCount":3}"""
        };
        db.Add(ordinary);
        await db.SaveChangesAsync(ct);
        using var detail = JsonDocument.Parse(await client.GetStringAsync($"/api/activitylog/{ordinary.Id}/metadata", ct));
        Assert.Equal(JsonValueKind.Null, detail.RootElement.GetProperty("changes").ValueKind);
        Assert.Single(detail.RootElement.GetProperty("fields").EnumerateArray());
    }

    [Fact]
    public async Task LegacyNormalizationHttp_RefreshesRawTimestampAndActualCacheWithoutActivity()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fixture = await ActivityLogHttpFixture.CreateAsync(0, ct, includeSettings: true);
        using var scope = fixture.App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();
        var row = await db.BlogConfiguration.SingleAsync(item => item.CfgKey == nameof(FeedSettings), ct);
        row.CfgValue = "{}";
        await db.SaveChangesAsync(ct);
        var time = row.LastModifiedTimeUtc;
        var caches = fixture.App.Services.GetRequiredService<PostCacheInvalidator>();
        var cacheKey = caches.GetFeedCacheKey("rss");
        using var client = fixture.App.GetTestClient();
        client.DefaultRequestHeaders.Add("X-B1-User", "admin");
        await AddPageToken(client, ct);

        using var response = await client.PatchAsJsonAsync("/api/settings/subscription",
            new Dictionary<string, int> { ["FeedItemCount"] = 20 }, ct);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsStringAsync(ct));
        Assert.Empty(await fixture.Store.ReadLogsAsync(ct));
        Assert.NotEqual(cacheKey, caches.GetFeedCacheKey("rss"));
        db.ChangeTracker.Clear();
        var saved = await db.BlogConfiguration.SingleAsync(item => item.CfgKey == nameof(FeedSettings), ct);
        Assert.Equal(new FeedSettings { FeedItemCount = 20, UseFullContent = false }.ToJson(), saved.CfgValue);
        Assert.True(saved.LastModifiedTimeUtc > time);
        Assert.Equal(20, fixture.Store.Config.Capture().FeedSettings.FeedItemCount);
        Assert.False(fixture.Store.Config.Capture().FeedSettings.UseFullContent);
    }

    private static async Task AddPageToken(HttpClient client, CancellationToken ct)
    {
        using var page = await client.GetAsync("/admin/activitylogs", ct);
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        var token = Regex.Match(await page.Content.ReadAsStringAsync(ct), "name=\"CSRF-TOKEN-MOONGLADE-FORM\"[^>]*value=\"([^\"]+)\"");
        Assert.True(token.Success);
        client.DefaultRequestHeaders.Add("Cookie", page.Headers.GetValues("Set-Cookie").Single().Split(';')[0]);
        client.DefaultRequestHeaders.Add("XSRF-TOKEN", WebUtility.HtmlDecode(token.Groups[1].Value));
    }

    private static async Task<string> ReadConfiguration(ActivityLogHttpFixture fixture, string key, CancellationToken ct)
    {
        using var scope = fixture.App.Services.CreateScope();
        return (await scope.ServiceProvider.GetRequiredService<BlogDbContext>().BlogConfiguration.AsNoTracking().SingleAsync(row => row.CfgKey == key, ct)).CfgValue;
    }
}
