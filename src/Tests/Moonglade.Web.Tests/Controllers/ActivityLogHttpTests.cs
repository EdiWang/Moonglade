using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Moonglade.ActivityLog;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Web.Tests.TestSupport;
using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Moonglade.Web.Tests.Controllers;

public partial class ActivityLogHttpTests
{
    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("reader", HttpStatusCode.Forbidden)]
    public async Task Clear_AnonymousOrNonAdministrator_IsDeniedWithoutMutation(string? user, HttpStatusCode expected)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fixture = await ActivityLogHttpFixture.CreateAsync(ct: ct);
        using var client = fixture.App.GetTestClient();
        if (user != null) client.DefaultRequestHeaders.Add("X-B1-User", user);
        var original = await fixture.Store.ReadLogsAsync(ct);

        using var page = await client.GetAsync("/admin/activitylogs", ct);
        using var response = await client.DeleteAsync("/api/activitylog/clear", ct);

        Assert.Equal(expected, page.StatusCode);
        Assert.Equal(expected, response.StatusCode);
        Assert.Equal(original.Select(row => (row.Id, row.EventKey, row.Details)),
            (await fixture.Store.ReadLogsAsync(ct)).Select(row => (row.Id, row.EventKey, row.Details)));
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("invalid")]
    public async Task Clear_AdminWithoutValidAntiforgery_IsDeniedWithoutMutation(string tokenMode)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fixture = await ActivityLogHttpFixture.CreateAsync(ct: ct);
        using var client = fixture.App.GetTestClient();
        client.DefaultRequestHeaders.Add("X-B1-User", "admin");
        var original = await fixture.Store.ReadLogsAsync(ct);
        using var page = await client.GetAsync("/admin/activitylogs", ct);
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        client.DefaultRequestHeaders.Add("Cookie", page.Headers.GetValues("Set-Cookie").Single().Split(';')[0]);
        if (tokenMode == "invalid") client.DefaultRequestHeaders.Add("XSRF-TOKEN", "not-a-valid-token");

        using var response = await client.DeleteAsync("/api/activitylog/clear", ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(original.Select(row => (row.Id, row.EventKey, row.TargetDisplayName)),
            (await fixture.Store.ReadLogsAsync(ct)).Select(row => (row.Id, row.EventKey, row.TargetDisplayName)));
    }

    [Fact]
    public async Task Clear_AdminWithPageToken_ClearsHistoryAndReturnsCanonicalReadShape()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fixture = await ActivityLogHttpFixture.CreateAsync(ct: ct);
        using var client = fixture.App.GetTestClient();
        client.DefaultRequestHeaders.Add("X-B1-User", "admin");
        client.DefaultRequestHeaders.UserAgent.ParseAdd("b1-http-agent");
        using var history = JsonDocument.Parse(await client.GetStringAsync(
            $"/api/activitylog/list?pageIndex=1&pageSize=10&eventKeys={ActivityEvents.CommentDeleted}", ct));
        Assert.Equal(1, history.RootElement.GetProperty("totalItemCount").GetInt32());
        var historical = Assert.Single(history.RootElement.GetProperty("items").EnumerateArray());
        Assert.Equal("comment.deleted", historical.GetProperty("eventKey").GetString());
        Assert.Equal("Comments deleted: Historical target 1", historical.GetProperty("summary").GetString());
        using var page = await client.GetAsync("/admin/activitylogs", ct);
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        var html = await page.Content.ReadAsStringAsync(ct);
        // Untrusted summaries and details must be rendered as text, not executable HTML.
        Assert.Contains("x-text=\"log.summary\"", html);
        Assert.Contains("x-text=\"formatDetailValue(field.value, field.key)\"", html);
        Assert.DoesNotMatch("x-html=\"[^\"]*(log\\.|field\\.|currentDetail\\.)", html);
        var token = Regex.Match(html, "name=\"CSRF-TOKEN-MOONGLADE-FORM\"[^>]*value=\"([^\"]+)\"");
        Assert.True(token.Success, "Actual ActivityLogs Razor page must issue a form token.");
        client.DefaultRequestHeaders.Add("Cookie", page.Headers.GetValues("Set-Cookie").Single().Split(';')[0]);
        client.DefaultRequestHeaders.Add("XSRF-TOKEN", WebUtility.HtmlDecode(token.Groups[1].Value));

        using var response = await client.DeleteAsync("/api/activitylog/clear", ct);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsStringAsync(ct));
        var row = Assert.Single(await fixture.Store.ReadLogsAsync(ct));
        Assert.Equal("admin", row.ActorDisplayName);
        Assert.Equal(ActivityActorKind.Administrator, row.ActorKind);
        Assert.Equal("192.0.2.25", row.IpAddress);
        Assert.Equal("b1-http-agent", row.UserAgent);
        using var metadata = JsonDocument.Parse(row.Details!);
        Assert.Equal(3, metadata.RootElement.GetProperty("DeletedCount").GetInt32());
        using var list = JsonDocument.Parse(await client.GetStringAsync(
            $"/api/activitylog/list?pageIndex=1&pageSize=10&eventKeys={ActivityEvents.ActivityLogsCleared}", ct));
        Assert.Equal(1, list.RootElement.GetProperty("totalItemCount").GetInt32());
        var item = Assert.Single(list.RootElement.GetProperty("items").EnumerateArray());
        Assert.Equal("activity-log.cleared", item.GetProperty("eventKey").GetString());
        Assert.Equal("Activity log cleared", item.GetProperty("summary").GetString());
        Assert.Equal("admin", item.GetProperty("actor").GetString());
        Assert.Equal("Administrator", item.GetProperty("actorKind").GetString());
        Assert.Equal(row.Id, item.GetProperty("id").GetInt64());
        Assert.Equal(["actor", "actorKind", "eventKey", "eventTimeUtc", "id", "summary"],
            item.EnumerateObject().Select(property => property.Name).Order().ToArray());
        using var detail = JsonDocument.Parse(await client.GetStringAsync($"/api/activitylog/{row.Id}/metadata", ct));
        var field = Assert.Single(detail.RootElement.GetProperty("fields").EnumerateArray());
        Assert.Equal("DeletedCount", field.GetProperty("key").GetString());
        Assert.Equal("Deleted count", field.GetProperty("label").GetString());
        Assert.Equal(3, field.GetProperty("value").GetInt32());
    }

    [Theory]
    [InlineData("en-US")]
    [InlineData("zh-Hans")]
    [InlineData("zh-Hant")]
    [InlineData("de-DE")]
    [InlineData("ja-JP")]
    [InlineData("fr-FR")]
    public async Task Catalog_AllEvents_LocalizedFiltersSummaryAndHeadingShareIdentity(string culture)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fixture = await ActivityLogHttpFixture.CreateAsync(0, ct);
        using var scope = fixture.App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();
        const string target = "<script>captured title</script>";
        foreach (var definition in ActivityEvents.All)
            db.ActivityLog.Add(new ActivityLogEntity
            {
                EventKey = definition.Key,
                EventTimeUtc = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
                ActorKind = ActivityActorKind.Administrator,
                ActorDisplayName = "admin",
                TargetId = "captured-id",
                TargetDisplayName = target,
                Details = definition.SettingKeys is null ? null : """{"Changes":[]}"""
            });
        await db.SaveChangesAsync(ct);
        using var client = fixture.App.GetTestClient();
        client.DefaultRequestHeaders.Add("X-B1-User", "admin");
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd(culture);
        using var filters = JsonDocument.Parse(await client.GetStringAsync("/api/activitylog/eventtypes", ct));
        var labels = filters.RootElement.EnumerateArray()
            .SelectMany(group => group.GetProperty("items").EnumerateArray())
            .ToDictionary(item => item.GetProperty("key").GetString()!, item => item.GetProperty("label").GetString()!);
        Assert.Equal(52, labels.Count);
        using var list = JsonDocument.Parse(await client.GetStringAsync("/api/activitylog/list?pageSize=100", ct));
        Assert.Equal(52, list.RootElement.GetProperty("totalItemCount").GetInt32());
        var items = list.RootElement.GetProperty("items").EnumerateArray().ToArray();
        Assert.Equal(52, items.Length);
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
            var localizer = scope.ServiceProvider.GetRequiredService<IStringLocalizer<Program>>();
            var expectedGroups = ActivityEvents.All.GroupBy(definition => definition.Group)
                .OrderBy(group => group.Key, StringComparer.Ordinal).ToArray();
            var actualGroups = filters.RootElement.EnumerateArray().ToArray();
            Assert.Equal(expectedGroups.Select(group => localizer[group.Key].Value),
                actualGroups.Select(group => group.GetProperty("label").GetString()));
            for (var index = 0; index < expectedGroups.Length; index++)
            {
                var group = actualGroups[index];
                Assert.Equal(["items", "label"], group.EnumerateObject().Select(property => property.Name).Order().ToArray());
                var options = group.GetProperty("items").EnumerateArray().ToArray();
                Assert.Equal(expectedGroups[index].OrderBy(definition => definition.Key, StringComparer.Ordinal)
                    .Select(definition => definition.Key), options.Select(option => option.GetProperty("key").GetString()));
                Assert.All(options, option => Assert.Equal(["key", "label"],
                    option.EnumerateObject().Select(property => property.Name).Order().ToArray()));
            }
            foreach (var item in items)
            {
                var key = item.GetProperty("eventKey").GetString()!;
                var definition = ActivityEvents.Get(key);
                Assert.Equal(localizer[definition.Label].Value, labels[key]);
                if (culture != "en-US") Assert.NotEqual(definition.Label, labels[key]);
                var expectedSummary = definition.SettingKeys is null ? $"{labels[key]}: {target}" : labels[key];
                Assert.Equal(expectedSummary, item.GetProperty("summary").GetString());
                Assert.Equal("admin", item.GetProperty("actor").GetString());
                Assert.Equal("Administrator", item.GetProperty("actorKind").GetString());
                using var detail = JsonDocument.Parse(await client.GetStringAsync(
                    $"/api/activitylog/{item.GetProperty("id").GetInt64()}/metadata", ct));
                Assert.Equal(key, detail.RootElement.GetProperty("eventKey").GetString());
                Assert.Equal(labels[key], detail.RootElement.GetProperty("heading").GetString());
                Assert.Equal(target, detail.RootElement.GetProperty("targetDisplayName").GetString());
                Assert.Equal("captured-id", detail.RootElement.GetProperty("targetId").GetString());
                Assert.Empty(detail.RootElement.GetProperty("fields").EnumerateArray());
            }
        }
        finally { CultureInfo.CurrentUICulture = originalCulture; }
    }

    [Fact]
    public async Task Metadata_ExistingEmptyRowIs200_MissingIs404_WithReadableContextAndTypedFields()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fixture = await ActivityLogHttpFixture.CreateAsync(0, ct);
        using var scope = fixture.App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();
        ActivityLogEntity[] rows =
        [
            new() { EventKey = ActivityEvents.PostCreated, EventTimeUtc = DateTime.UtcNow, ActorKind = ActivityActorKind.Administrator },
            new()
            {
                EventKey = ActivityEvents.CommentDeleted, EventTimeUtc = DateTime.UtcNow,
                ActorKind = ActivityActorKind.Administrator, ActorDisplayName = "System",
                TargetDisplayName = "Captured comments", TargetId = "snapshot",
                IpAddress = "192.0.2.50", UserAgent = "<agent>",
                Details = "{\"CommentIds\":[\"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa\",\"bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb\"]}"
            }
        ];
        db.AddRange(rows);
        await db.SaveChangesAsync(ct);
        using var client = fixture.App.GetTestClient();
        client.DefaultRequestHeaders.Add("X-B1-User", "admin");
        using var emptyResponse = await client.GetAsync($"/api/activitylog/{rows[0].Id}/metadata", ct);
        Assert.Equal(HttpStatusCode.OK, emptyResponse.StatusCode);
        using var empty = JsonDocument.Parse(await emptyResponse.Content.ReadAsStringAsync(ct));
        Assert.Equal("Post created", empty.RootElement.GetProperty("heading").GetString());
        Assert.Equal("Administrator", empty.RootElement.GetProperty("actor").GetString());
        Assert.Empty(empty.RootElement.GetProperty("fields").EnumerateArray());
        using var missing = await client.GetAsync($"/api/activitylog/{rows[1].Id + 1}/metadata", ct);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        using var context = JsonDocument.Parse(await client.GetStringAsync($"/api/activitylog/{rows[1].Id}/metadata", ct));
        Assert.Equal("System", context.RootElement.GetProperty("actor").GetString());
        Assert.Equal("Administrator", context.RootElement.GetProperty("actorKind").GetString());
        Assert.Equal("snapshot", context.RootElement.GetProperty("targetId").GetString());
        Assert.Equal("192.0.2.50", context.RootElement.GetProperty("ipAddress").GetString());
        Assert.Equal("<agent>", context.RootElement.GetProperty("userAgent").GetString());
        var field = Assert.Single(context.RootElement.GetProperty("fields").EnumerateArray());
        Assert.Equal("CommentIds", field.GetProperty("key").GetString());
        Assert.Equal("Comment IDs", field.GetProperty("label").GetString());
        Assert.Equal(new[] { Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb") },
            field.GetProperty("value").EnumerateArray().Select(value => value.GetGuid()));
    }

    [Theory]
    [InlineData("eventKeys=unsupported", "eventKeys")]
    [InlineData("eventKeys=205", "eventKeys")]
    [InlineData("eventKeys=post.created&eventKeys=unknown", "eventKeys")]
    [InlineData("eventTypes=205", "eventTypes")]
    [InlineData("eventTypes=205&eventKeys=post.created", "eventTypes")]
    [InlineData("startTimeUtc=2026-02-02T00%3A00%3A00Z&endTimeUtc=2026-02-01T00%3A00%3A00Z", "endTimeUtc")]
    public async Task List_InvalidOrObsoleteFilters_Return400ValidationProblemWithoutMutation(string query, string field)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fixture = await ActivityLogHttpFixture.CreateAsync(ct: ct);
        using var client = fixture.App.GetTestClient();
        client.DefaultRequestHeaders.Add("X-B1-User", "admin");
        using var response = await client.GetAsync("/api/activitylog/list?" + query, ct);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        Assert.Equal(400, problem.RootElement.GetProperty("status").GetInt32());
        Assert.NotEmpty(problem.RootElement.GetProperty("errors").GetProperty(field).EnumerateArray());
        Assert.Equal(3, (await fixture.Store.ReadLogsAsync(ct)).Count);
    }

    [Theory]
    [InlineData(null, "/api/activitylog/list", HttpStatusCode.Unauthorized)]
    [InlineData("reader", "/api/activitylog/list", HttpStatusCode.Forbidden)]
    [InlineData(null, "/api/activitylog/eventtypes", HttpStatusCode.Unauthorized)]
    [InlineData("reader", "/api/activitylog/eventtypes", HttpStatusCode.Forbidden)]
    [InlineData(null, "/api/activitylog/1/metadata", HttpStatusCode.Unauthorized)]
    [InlineData("reader", "/api/activitylog/1/metadata", HttpStatusCode.Forbidden)]
    public async Task ReadEndpoints_AnonymousAndNonAdministrators_AreDenied(string? user, string path, HttpStatusCode expected)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fixture = await ActivityLogHttpFixture.CreateAsync(ct: ct);
        using var client = fixture.App.GetTestClient();
        if (user != null) client.DefaultRequestHeaders.Add("X-B1-User", user);
        using var response = await client.GetAsync(path, ct);
        Assert.Equal(expected, response.StatusCode);
        Assert.DoesNotContain("historical-admin", await response.Content.ReadAsStringAsync(ct));
        Assert.Equal(3, (await fixture.Store.ReadLogsAsync(ct)).Count);
    }

    [Theory]
    [InlineData("en-US", "System")]
    [InlineData("zh-Hans", "系统")]
    [InlineData("zh-Hant", "系統")]
    [InlineData("de-DE", "System")]
    [InlineData("ja-JP", "システム")]
    [InlineData("fr-FR", "Système")]
    public async Task SystemActor_IsExplicitlyLocalizedAndOmitsStoredRequestContext(string culture, string expectedActor)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fixture = await ActivityLogHttpFixture.CreateAsync(0, ct);
        using var scope = fixture.App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();
        var row = new ActivityLogEntity
        {
            EventKey = ActivityEvents.PostPublished,
            EventTimeUtc = DateTime.UtcNow,
            ActorKind = ActivityActorKind.System,
            ActorDisplayName = "B2-MUST-NOT-RENDER",
            IpAddress = "192.0.2.99",
            UserAgent = "B2-MUST-NOT-RENDER"
        };
        db.Add(row);
        await db.SaveChangesAsync(ct);
        using var client = fixture.App.GetTestClient();
        client.DefaultRequestHeaders.Add("X-B1-User", "admin");
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd(culture);
        var payload = await client.GetStringAsync($"/api/activitylog/{row.Id}/metadata", ct);
        Assert.DoesNotContain("B2-MUST-NOT-RENDER", payload);
        Assert.DoesNotContain("192.0.2.99", payload);
        using var detail = JsonDocument.Parse(payload);
        Assert.Equal(expectedActor, detail.RootElement.GetProperty("actor").GetString());
        Assert.Equal("System", detail.RootElement.GetProperty("actorKind").GetString());
        Assert.Equal(JsonValueKind.Null, detail.RootElement.GetProperty("ipAddress").ValueKind);
        Assert.Equal(JsonValueKind.Null, detail.RootElement.GetProperty("userAgent").ValueKind);
        using var list = JsonDocument.Parse(await client.GetStringAsync("/api/activitylog/list", ct));
        var item = Assert.Single(list.RootElement.GetProperty("items").EnumerateArray());
        Assert.Equal(expectedActor, item.GetProperty("actor").GetString());
        Assert.Equal(detail.RootElement.GetProperty("heading").GetString(), item.GetProperty("summary").GetString());
    }

    [Fact]
    public async Task List_RepeatedKeysAndInclusiveUtcDates_ReturnExactCapturedRows()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fixture = await ActivityLogHttpFixture.CreateAsync(ct: ct);
        using var client = fixture.App.GetTestClient();
        client.DefaultRequestHeaders.Add("X-B1-User", "admin");
        using var list = JsonDocument.Parse(await client.GetStringAsync(
            "/api/activitylog/list?eventKeys=post.created&eventKeys=comment.deleted&startTimeUtc=2026-01-01T12%3A00%3A00Z&endTimeUtc=2026-01-01T12%3A01%3A00Z", ct));
        Assert.Equal(2, list.RootElement.GetProperty("totalItemCount").GetInt32());
        var items = list.RootElement.GetProperty("items").EnumerateArray().ToArray();
        Assert.Equal(new long[] { 2, 1 }, items.Select(item => item.GetProperty("id").GetInt64()));
        Assert.Equal(new[] { "comment.deleted", "post.created" }, items.Select(item => item.GetProperty("eventKey").GetString()));
        Assert.Equal(new[] { "Comments deleted: Historical target 1", "Post created: Historical target 0" },
            items.Select(item => item.GetProperty("summary").GetString()));
    }

    [Fact]
    public async Task List_EndOfLocalDay_IncludesFinalTickButExcludesNextMidnight()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fixture = await ActivityLogHttpFixture.CreateAsync(0, ct);
        using var scope = fixture.App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();
        var nextMidnightUtc = new DateTime(2026, 11, 8, 16, 0, 0, DateTimeKind.Utc);
        var lastTick = new ActivityLogEntity
        {
            EventKey = ActivityEvents.PostCreated,
            EventTimeUtc = nextMidnightUtc.AddTicks(-1),
            ActorKind = ActivityActorKind.Administrator
        };
        db.AddRange(lastTick, new ActivityLogEntity
        {
            EventKey = ActivityEvents.PostCreated,
            EventTimeUtc = nextMidnightUtc,
            ActorKind = ActivityActorKind.Administrator
        });
        await db.SaveChangesAsync(ct);

        using var client = fixture.App.GetTestClient();
        client.DefaultRequestHeaders.Add("X-B1-User", "admin");
        using var list = JsonDocument.Parse(await client.GetStringAsync(
            "/api/activitylog/list?endTimeUtc=2026-11-08T15%3A59%3A59.9999999Z", ct));

        Assert.Equal(1, list.RootElement.GetProperty("totalItemCount").GetInt32());
        var item = Assert.Single(list.RootElement.GetProperty("items").EnumerateArray());
        Assert.Equal(lastTick.Id, item.GetProperty("id").GetInt64());
    }

    [Theory]
    [InlineData("en-US", "Administrator", "Post created")]
    [InlineData("zh-Hans", "管理员", "文章已创建")]
    public async Task BlankActorAndTargetDisplay_UseLocalizedAdministratorAndStableIdFallback(string culture, string expectedActor, string expectedLabel)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fixture = await ActivityLogHttpFixture.CreateAsync(0, ct);
        using var scope = fixture.App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();
        var row = new ActivityLogEntity
        {
            EventKey = ActivityEvents.PostCreated,
            EventTimeUtc = DateTime.UtcNow,
            ActorKind = ActivityActorKind.Administrator,
            ActorDisplayName = "   ",
            TargetId = "safe-stable-id",
            TargetDisplayName = "   "
        };
        db.Add(row);
        await db.SaveChangesAsync(ct);
        using var client = fixture.App.GetTestClient();
        client.DefaultRequestHeaders.Add("X-B1-User", "admin");
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd(culture);
        using var list = JsonDocument.Parse(await client.GetStringAsync("/api/activitylog/list", ct));
        var item = Assert.Single(list.RootElement.GetProperty("items").EnumerateArray());
        Assert.Equal(expectedActor, item.GetProperty("actor").GetString());
        Assert.Equal("Administrator", item.GetProperty("actorKind").GetString());
        Assert.Equal(expectedLabel + ": safe-stable-id", item.GetProperty("summary").GetString());
        using var detail = JsonDocument.Parse(await client.GetStringAsync($"/api/activitylog/{row.Id}/metadata", ct));
        Assert.Equal(expectedActor, detail.RootElement.GetProperty("actor").GetString());
        Assert.Equal(expectedLabel, detail.RootElement.GetProperty("heading").GetString());
        Assert.Equal("safe-stable-id", detail.RootElement.GetProperty("targetId").GetString());
    }

    [Theory]
    [InlineData("en-US")]
    [InlineData("zh-Hans")]
    [InlineData("zh-Hant")]
    [InlineData("de-DE")]
    [InlineData("ja-JP")]
    [InlineData("fr-FR")]
    public async Task NonSettingsDetails_AllowedLabels_LocalizeFromWriterAndRemainTypedPlainText(string culture)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fixture = await ActivityLogHttpFixture.CreateAsync(0, ct);
        using var scope = fixture.App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();
        var id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        Dictionary<string, (string Label, object Value)> expected = new(StringComparer.Ordinal)
        {
            ["PostId"] = ("Post ID", id),
            ["PageId"] = ("Page ID", id),
            ["CategoryId"] = ("Category ID", 101),
            ["Id"] = ("Category ID", 102),
            ["CommentId"] = ("Comment ID", id),
            ["CommentIds"] = ("Comment IDs", new[] { id }),
            ["TagId"] = ("Tag ID", id),
            ["ThemeId"] = ("Theme ID", 7),
            ["WidgetId"] = ("Widget ID", id),
            ["FileId"] = ("File ID", id),
            ["Slug"] = ("Slug", "b5-safe-slug"),
            ["PostStatus"] = ("Post status", PostStatus.Draft),
            ["Hours"] = ("Hours", 3),
            ["Count"] = ("Count", 2),
            ["DeletedCount"] = ("Deleted count", 4),
            ["WidgetType"] = ("Widget Type", WidgetType.ImageLink),
            ["AccentColor"] = ("Accent Color", "#123456"),
            ["FileName"] = ("File Name", "<b>B5 file name</b>.png"),
            ["OldFileName"] = ("Previous file name", "B5 previous file.txt"),
            ["FileSize"] = ("File size", 2048),
            ["ContentType"] = ("Content Type", "image/png"),
            ["ImageSize"] = ("Image dimensions", "64x32"),
            ["SkipWatermark"] = ("Skip watermark", true),
            ["IsEnabled"] = ("Enabled", false),
            ["IsPublished"] = ("Published", true),
            ["Username"] = ("Username", "<script>B5 display name</script>")
        };
        var definitions = ActivityEvents.All.Where(definition => definition.SettingKeys is null).OrderBy(definition => definition.Key).ToArray();
        Assert.Equal(expected.Keys.Order(StringComparer.Ordinal),
            definitions.SelectMany(definition => definition.DetailKeys).Distinct().Order(StringComparer.Ordinal));
        var jsonOptions = new JsonSerializerOptions { Converters = { new JsonStringEnumConverter() } };
        using var client = fixture.App.GetTestClient();
        client.DefaultRequestHeaders.Add("X-B1-User", "admin");
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd(culture);
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
            var localizer = scope.ServiceProvider.GetRequiredService<IStringLocalizer<Program>>();
            foreach (var definition in definitions)
            {
                var keys = definition.DetailKeys.Order(StringComparer.Ordinal).ToArray();
                await new CreateActivityLogCommandHandler(db, fixture.Store.WriterLogger.Object).HandleAsync(
                    new CreateActivityLogCommand(definition.Key, ActivityActorKind.Administrator, "admin",
                        Details: ActivityLogDetails.Create(keys.Select(key => (key, (object?)expected[key].Value)).ToArray())), ct);
                var row = await db.ActivityLog.AsNoTracking().SingleAsync(item => item.EventKey == definition.Key, ct);
                using var response = await client.GetAsync($"/api/activitylog/{row.Id}/metadata", ct);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                using var details = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
                Assert.Equal(row.Id, details.RootElement.GetProperty("id").GetInt64());
                Assert.Equal(definition.Key, details.RootElement.GetProperty("eventKey").GetString());
                Assert.Equal(JsonValueKind.Null, details.RootElement.GetProperty("changes").ValueKind);
                var fields = details.RootElement.GetProperty("fields").EnumerateArray().ToArray();
                Assert.Equal(keys, fields.Select(field => field.GetProperty("key").GetString()));
                foreach (var field in fields)
                {
                    var key = field.GetProperty("key").GetString()!;
                    var label = localizer[expected[key].Label];
                    if (culture != "en-US") Assert.False(label.ResourceNotFound, $"Missing existing resource {expected[key].Label} in {culture}.");
                    Assert.Equal(label.Value, field.GetProperty("label").GetString());
                    var value = JsonSerializer.SerializeToElement(expected[key].Value, jsonOptions);
                    Assert.Equal(value.ValueKind, field.GetProperty("value").ValueKind);
                    Assert.True(JsonElement.DeepEquals(value, field.GetProperty("value")), $"Unexpected value for {definition.Key}/{key}.");
                }
            }
            using var page = await client.GetAsync("/admin/activitylogs", ct);
            Assert.Equal(HttpStatusCode.OK, page.StatusCode);
            var html = await page.Content.ReadAsStringAsync(ct);
            // These bindings enforce text-only handling of untrusted data, independent of tags or styling.
            Assert.Contains("x-text=\"field.label\"", html);
            Assert.Contains("x-text=\"formatDetailValue(field.value, field.key)\"", html);
            Assert.Contains("x-text=\"currentDetail.ipAddress\"", html);
            Assert.Contains("x-text=\"currentDetail.userAgent\"", html);
            Assert.DoesNotMatch("x-html=\"[^\"]*(field\\.|currentDetail\\.)", html);
        }
        finally { CultureInfo.CurrentUICulture = originalCulture; }
    }
}
