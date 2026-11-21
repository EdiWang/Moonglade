using Edi.CacheAside.InMemory;
using Edi.AspNetCore.Utils.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Moonglade.ActivityLog;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Web.Controllers;
using Moonglade.Web.Tests.Handlers;
using Moonglade.Web.Tests.TestSupport;
using Moonglade.Widgets;
using Moq;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Moonglade.Web.Tests.Controllers;

public class WidgetCollectionHttpTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("reader", HttpStatusCode.Forbidden)]
    public async Task Collection_RequiresAdministrator(string? user, HttpStatusCode status)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fixture = await CreateAsync(ct);
        using var client = fixture.App.GetTestClient();
        if (user is not null) client.DefaultRequestHeaders.Add("X-B1-User", user);
        Assert.Equal(status, (await client.GetAsync("/api/widgets", ct)).StatusCode);
        using var response = await client.PutAsJsonAsync("/api/widgets", new WidgetCollectionRequest { Widgets = [] }, JsonOptions, ct);
        Assert.Equal(status, response.StatusCode);
        using var preview = await client.PostAsJsonAsync("/api/widgets/preview", new WidgetCollectionRequest { Widgets = [] }, JsonOptions, ct);
        Assert.Equal(status, preview.StatusCode);
        Assert.Empty(await ReadWidgetsAsync(fixture, ct));
        Assert.Empty(await fixture.Store.ReadLogsAsync(ct));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Save_RequiresValidAntiforgery(bool invalid)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fixture = await CreateAsync(ct);
        using var client = fixture.App.GetTestClient();
        client.DefaultRequestHeaders.Add("X-B1-User", "admin");
        if (invalid) client.DefaultRequestHeaders.Add("XSRF-TOKEN", "invalid");
        using var response = await client.PutAsJsonAsync("/api/widgets", new WidgetCollectionRequest { Widgets = [] }, JsonOptions, ct);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var preview = await client.PostAsJsonAsync("/api/widgets/preview", new WidgetCollectionRequest { Widgets = [] }, JsonOptions, ct);
        Assert.Equal(HttpStatusCode.BadRequest, preview.StatusCode);
        Assert.Empty(await ReadWidgetsAsync(fixture, ct));
        Assert.Empty(await fixture.Store.ReadLogsAsync(ct));
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("null")]
    [InlineData("nullItem")]
    [InlineData("partial")]
    [InlineData("missingEnabled")]
    [InlineData("duplicate")]
    [InlineData("emptyId")]
    [InlineData("invalidId")]
    [InlineData("title")]
    [InlineData("content")]
    [InlineData("region")]
    [InlineData("type")]
    [InlineData("item")]
    public async Task InvalidCompleteDraft_Is400WithFieldDetails_WithoutAnyPersistence(string mode)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fixture = await CreateAsync(ct);
        using var client = await AdminClientAsync(fixture, ct);
        var original = WidgetCollectionTests.Draft("Original");
        await SaveAsync(client, [original], ct);
        var baseline = await ReadWidgetsAsync(fixture, ct);
        var count = (await fixture.Store.ReadLogsAsync(ct)).Count;
        var valid = WidgetCollectionTests.Draft("Valid");
        var invalid = WidgetCollectionTests.Draft("Invalid");
        var root = JsonSerializer.SerializeToNode(new WidgetCollectionRequest { Widgets = [valid, invalid] }, JsonOptions)!.AsObject();
        var item = root["widgets"]![1]!.AsObject();
        switch (mode)
        {
            case "missing": root.Remove("widgets"); break;
            case "null": root["widgets"] = null; break;
            case "nullItem": root["widgets"]![1] = null; break;
            case "partial": item.Remove("region"); break;
            case "missingEnabled": item.Remove("isEnabled"); break;
            case "duplicate": item["id"] = valid.Id; break;
            case "emptyId": item["id"] = Guid.Empty; break;
            case "invalidId": item["id"] = "not-a-guid"; break;
            case "title": item["title"] = new string('A', 129); break;
            case "content": item["contentCode"] = new string('A', 2001); break;
            case "region": item["region"] = 99; break;
            case "type": item["widgetType"] = 99; break;
            case "item": item["contentCode"] = "[{\"name\":\"No URL\"}]"; break;
        }

        using var response = await client.PutAsync("/api/widgets", new StringContent(root.ToJsonString(), Encoding.UTF8, "application/json"), ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        var errors = problem.RootElement.GetProperty("errors");
        Assert.NotEmpty(errors.EnumerateObject());
        if (mode == "item") Assert.Contains(errors.EnumerateObject(), field => field.Name.Contains("Widgets[1].ContentCode[0]", StringComparison.Ordinal));
        using var preview = await client.PostAsync("/api/widgets/preview", new StringContent(root.ToJsonString(), Encoding.UTF8, "application/json"), ct);
        Assert.Equal(HttpStatusCode.BadRequest, preview.StatusCode);
        Assert.Equal(baseline, await ReadWidgetsAsync(fixture, ct));
        Assert.Equal(count, (await fixture.Store.ReadLogsAsync(ct)).Count);
    }

    [Fact]
    public async Task Replacement_ReturnsCanonicalSavedSnapshot_AndAuditsOnlyActualChangesIncludingDeletedName()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fixture = await CreateAsync(ct);
        using var client = await AdminClientAsync(fixture, ct);
        var first = WidgetCollectionTests.Draft("A");
        var deleted = WidgetCollectionTests.Draft("Deleted display name");
        var initial = await SaveAsync(client, [first, deleted], ct);
        first.Title = "Changed";
        var added = WidgetCollectionTests.Draft("New");
        added.Region = WidgetRegion.BeforeProfile;
        added.SortOrder = 1000;

        var saved = await SaveAsync(client, [first, added], ct);

        Assert.Equal([added.Id, first.Id], saved.Widgets.Select(widget => widget.Id));
        Assert.All(saved.Widgets, widget => Assert.Equal(0, widget.SortOrder));
        Assert.Equal(initial.Widgets[0].CreatedTimeUtc, saved.Widgets[1].CreatedTimeUtc);
        using var get = await client.GetAsync("/api/widgets", ct);
        Assert.True(get.Headers.CacheControl!.NoStore);
        var loaded = (await get.Content.ReadFromJsonAsync<WidgetCollection>(JsonOptions, ct))!;
        Assert.Equal(saved.Widgets, loaded.Widgets);
        var logs = await fixture.Store.ReadLogsAsync(ct);
        Assert.Equal(5, logs.Count);
        var removedLog = Assert.Single(logs, row => row.EventKey == ActivityEvents.WidgetDeleted);
        Assert.Equal(deleted.Id.ToString(), removedLog.TargetId);
        Assert.Equal(deleted.Title, removedLog.TargetDisplayName);
        Assert.DoesNotContain(logs, row => row.Details?.Contains("contentCode", StringComparison.OrdinalIgnoreCase) == true);
        await SaveAsync(client, [first, added], ct);
        Assert.Equal(5, (await fixture.Store.ReadLogsAsync(ct)).Count);
        Assert.Empty((await SaveAsync(client, [], ct)).Widgets);
        Assert.Empty(await ReadWidgetsAsync(fixture, ct));
    }

    [Fact]
    public async Task MetadataFromClient_CannotReplaceServerCreationTimeOrContentType()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fixture = await CreateAsync(ct);
        using var client = await AdminClientAsync(fixture, ct);
        var root = JsonSerializer.SerializeToNode(new WidgetCollectionRequest { Widgets = [WidgetCollectionTests.Draft("Owned metadata")] }, JsonOptions)!;
        root["widgets"]![0]!["createdTimeUtc"] = "2000-01-01T00:00:00Z";
        root["widgets"]![0]!["contentType"] = "Invalid";
        using var response = await client.PutAsync("/api/widgets", new StringContent(root.ToJsonString(), Encoding.UTF8, "application/json"), ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var saved = (await response.Content.ReadFromJsonAsync<WidgetCollection>(JsonOptions, ct))!;
        Assert.Equal(WidgetContentType.JSON, saved.Widgets[0].ContentType);
        Assert.True(saved.Widgets[0].CreatedTimeUtc.Year >= 2026);
    }

    [Fact]
    public async Task SuccessfulReplacement_EvictsWarmActualWidgetCache()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fixture = await CreateAsync(ct);
        using var client = await AdminClientAsync(fixture, ct);
        var widget = WidgetCollectionTests.Draft("Before cache eviction");
        await SaveAsync(client, [widget], ct);
        var cache = fixture.App.Services.GetRequiredService<ICacheAside>();
        async Task<WidgetSnapshot[]> ReadCachedAsync() => (await cache.GetOrCreateAsync("General", "widgets",
            () => ReadWidgetsAsync(fixture, ct), TimeSpan.FromMinutes(30)))!;
        Assert.Equal(widget.Title, Assert.Single(await ReadCachedAsync()).Title);
        widget.Title = "After cache eviction";

        var saved = await SaveAsync(client, [widget], ct);

        Assert.Equal(saved.Widgets, await ReadCachedAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PostcommitAuditOrCacheFailure_PreservesSaveAndCommunicatesCommittedState(bool cacheFailure)
    {
        var ct = TestContext.Current.CancellationToken;
        var cache = new Mock<ICacheAside>();
        if (cacheFailure) cache.Setup(value => value.Remove(It.IsAny<string>(), It.IsAny<string>())).Throws(new InvalidOperationException("Synthetic cache fault."));
        await using var fixture = await CreateAsync(ct, services =>
        {
            if (cacheFailure) services.AddSingleton(cache.Object);
            else services.ConfigureDbContext<BlogDbContext>(options => options.AddInterceptors(new AuditFault()));
        });
        using var client = await AdminClientAsync(fixture, ct);
        var widget = WidgetCollectionTests.Draft("Saved despite notification failure");

        using var response = await client.PutAsJsonAsync("/api/widgets", new WidgetCollectionRequest { Widgets = [widget] }, JsonOptions, ct);

        Assert.Equal(cacheFailure ? HttpStatusCode.InternalServerError : HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(widget.Id, Assert.Single(await ReadWidgetsAsync(fixture, ct)).Id);
        if (cacheFailure)
        {
            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            Assert.True(body.RootElement.GetProperty("committed").GetBoolean());
            Assert.Equal(widget.Id, body.RootElement.GetProperty("savedCollection").GetProperty("widgets")[0].GetProperty("id").GetGuid());
            Assert.Single(await fixture.Store.ReadLogsAsync(ct));
            cache.Verify(value => value.Remove(It.IsAny<string>(), "widgets"), Times.Once);
        }
        else Assert.Empty(await fixture.Store.ReadLogsAsync(ct));
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/html,test")]
    public async Task Preview_RendersSubmittedContentWithSecurityAndOpeningSemantics_WithoutPersistenceOrAudit(string unsafeUrl)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fixture = await CreateAsync(ct);
        var settings = fixture.Store.Config.Capture();
        fixture.Store.Config.Publish(settings with
        {
            GeneralSettings = settings.GeneralSettings with
            {
                WidgetsProfile = false, HotTagAmount = 0, WidgetsCategoryList = false, WidgetsSubscriptionButtons = false
            }
        });
        var previewSettings = fixture.Store.Config.Capture();
        using var client = await AdminClientAsync(fixture, ct);
        var saved = await SaveAsync(client, [WidgetCollectionTests.Draft("Persisted cache value")], ct);
        var cache = fixture.App.Services.GetRequiredService<ICacheAside>();
        var cached = await cache.GetOrCreateAsync("General", "widgets", async () =>
        {
            using var scope = fixture.App.Services.CreateScope();
            return await scope.ServiceProvider.GetRequiredService<BlogDbContext>().Widget.AsNoTracking().ToListAsync(ct);
        }, TimeSpan.FromMinutes(20));
        var links = WidgetCollectionTests.Draft("Draft <script>encoded</script>");
        links.ContentCode = """[{"name":"<script>label</script>","url":"javascript:alert(1)","openInNewTab":true},{"name":"New","url":"https://example.test/new","openInNewTab":true},{"name":"Same","url":"https://example.test/same","openInNewTab":false},{"name":"Legacy","url":"https://example.test/legacy"}]""";
        var image = WidgetCollectionTests.Draft("Image");
        image.WidgetType = WidgetType.ImageLink;
        image.ContentCode = """{"imageUrl":"javascript:alert(1)","altText":"<script>image</script>","cssClass":"x\" onerror=\"alert(1)","linkUrl":"javascript:alert(1)"}""";
        var button = WidgetCollectionTests.Draft("Button");
        button.WidgetType = WidgetType.ButtonLink;
        button.ContentCode = """[{"text":"<script>button</script>","url":"javascript:alert(1)","cssClass":"x\" onclick=\"alert(1)"}]""";
        links.ContentCode = links.ContentCode.Replace("javascript:alert(1)", unsafeUrl);
        image.ContentCode = image.ContentCode.Replace("javascript:alert(1)", unsafeUrl);
        button.ContentCode = button.ContentCode.Replace("javascript:alert(1)", unsafeUrl);
        using var response = await client.PostAsJsonAsync("/api/widgets/preview?dark=true",
            new WidgetCollectionRequest { Widgets = [links, image, button] }, JsonOptions, ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType!.MediaType);
        Assert.True(response.Headers.CacheControl!.NoStore);
        var html = await response.Content.ReadAsStringAsync(ct);
        Assert.Contains("Draft &lt;script&gt;encoded&lt;/script&gt;", html);
        Assert.DoesNotContain("Persisted cache value", html);
        Assert.DoesNotContain("<script", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotMatch("(?:href|src)=\"(?:javascript:|data:text/html)", html);
        Assert.DoesNotMatch(" on(?:error|click)=\"", html);
        Assert.Contains("script-src 'none'", html);
        var newLink = Regex.Match(html, "<a[^>]*href=\"https://example.test/new\"[^>]*>").Value;
        var sameLink = Regex.Match(html, "<a[^>]*href=\"https://example.test/same\"[^>]*>").Value;
        var legacyLink = Regex.Match(html, "<a[^>]*href=\"https://example.test/legacy\"[^>]*>").Value;
        Assert.Contains("target=\"_blank\"", newLink);
        Assert.Contains("noopener noreferrer", newLink);
        Assert.NotEmpty(sameLink);
        Assert.DoesNotContain("target=", sameLink);
        Assert.Contains("target=\"_blank\"", legacyLink);
        Assert.Contains("noopener noreferrer", legacyLink);
        using var empty = await client.PostAsJsonAsync("/api/widgets/preview", new WidgetCollectionRequest { Widgets = [] }, JsonOptions, ct);
        Assert.Equal(HttpStatusCode.OK, empty.StatusCode);
        Assert.DoesNotContain("Persisted cache value", await empty.Content.ReadAsStringAsync(ct));
        Assert.Equal(saved.Widgets, await ReadWidgetsAsync(fixture, ct));
        Assert.Single(await fixture.Store.ReadLogsAsync(ct));
        var stillCached = await cache.GetOrCreateAsync("General", "widgets", () => Task.FromResult(new List<WidgetEntity>()), TimeSpan.FromMinutes(20));
        Assert.Same(cached, stillCached);
        Assert.Same(previewSettings, fixture.Store.Config.Capture());
    }

    [Theory]
    [InlineData("</style>")]
    [InlineData("</STYLE >")]
    [InlineData("</sty</stylele>")]
    public async Task Preview_CapturesCurrentCustomCss_WithoutEscapingItsHtmlRawTextBoundary(string closingTag)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fixture = await CreateAsync(ct);
        var settings = fixture.Store.Config.Capture();
        fixture.Store.Config.Publish(settings with
        {
            AppearanceSettings = settings.AppearanceSettings with
            {
                EnableCustomCss = true,
                CssCode = "/* raw text fixture */ body { --label: \"" + closingTag + "<script>alert(1)</script>\"; }"
            },
            GeneralSettings = settings.GeneralSettings with { SideBarOption = Moonglade.Configuration.SideBarOption.Disabled }
        });
        using var client = await AdminClientAsync(fixture, ct);
        using var response = await client.PostAsJsonAsync("/api/widgets/preview", new WidgetCollectionRequest { Widgets = [] }, JsonOptions, ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync(ct);
        var start = html.IndexOf("/* raw text fixture */", StringComparison.Ordinal);
        Assert.True(start >= 0);
        var css = html[start..html.LastIndexOf("</style>", StringComparison.OrdinalIgnoreCase)];
        Assert.DoesNotContain("</style", css, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<\\/style", css, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(await ReadWidgetsAsync(fixture, ct));
        Assert.Empty(await fixture.Store.ReadLogsAsync(ct));
    }

    private static Task<ActivityLogHttpFixture> CreateAsync(CancellationToken ct, Action<IServiceCollection>? extra = null) =>
        ActivityLogHttpFixture.CreateAsync(historyCount: 0, ct: ct, configureServices: services =>
        {
            services.Configure<MvcOptions>(options => options.Filters.Add<ProblemDetailsResultFilter>());
            extra?.Invoke(services);
        }, additionalControllers: [typeof(WidgetsController)]);

    [Fact]
    public async Task PersistenceFailure_Returns500WithoutSideEffects_ThenAcceptsNextSave()
    {
        var ct = TestContext.Current.CancellationToken;
        var fault = new PersistenceFault();
        var cache = new Mock<ICacheAside>();
        await using var fixture = await ActivityLogHttpFixture.CreateAsync(historyCount: 0, ct: ct,
            configureServices: services =>
            {
                services.AddSingleton(cache.Object);
                services.ConfigureDbContext<BlogDbContext>(options => options.AddInterceptors(fault));
            }, useExceptionHandler: true, additionalControllers: [typeof(WidgetsController)]);
        using var client = await AdminClientAsync(fixture, ct);
        var baseline = await SaveAsync(client, [WidgetCollectionTests.Draft("Original")], ct);
        fault.Fail = true;

        using var response = await client.PutAsJsonAsync("/api/widgets",
            new WidgetCollectionRequest { Widgets = [WidgetCollectionTests.Draft("Failed replacement")] }, JsonOptions, ct);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal(baseline.Widgets, await ReadWidgetsAsync(fixture, ct));
        Assert.Single(await fixture.Store.ReadLogsAsync(ct));
        cache.Verify(value => value.Remove(It.IsAny<string>(), "widgets"), Times.Once);
        fault.Fail = false;
        Assert.Single((await SaveAsync(client, [WidgetCollectionTests.Draft("Successful replacement")], ct)).Widgets);
    }

    private static async Task<HttpClient> AdminClientAsync(ActivityLogHttpFixture fixture, CancellationToken ct)
    {
        var client = fixture.App.GetTestClient();
        client.DefaultRequestHeaders.Add("X-B1-User", "admin");
        using var page = await client.GetAsync("/admin/widgets", ct);
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        var html = await page.Content.ReadAsStringAsync(ct);
        var token = Regex.Match(html, "name=\"CSRF-TOKEN-MOONGLADE-FORM\"[^>]*value=\"([^\"]+)\"");
        Assert.True(token.Success);
        client.DefaultRequestHeaders.Add("Cookie", page.Headers.GetValues("Set-Cookie").Single().Split(';')[0]);
        client.DefaultRequestHeaders.Add("XSRF-TOKEN", WebUtility.HtmlDecode(token.Groups[1].Value));
        return client;
    }

    private static async Task<WidgetCollection> SaveAsync(HttpClient client, List<WidgetDraft?> widgets, CancellationToken ct)
    {
        using var response = await client.PutAsJsonAsync("/api/widgets", new WidgetCollectionRequest { Widgets = widgets }, JsonOptions, ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<WidgetCollection>(JsonOptions, ct))!;
    }

    private static async Task<WidgetSnapshot[]> ReadWidgetsAsync(ActivityLogHttpFixture fixture, CancellationToken ct)
    {
        using var scope = fixture.App.Services.CreateScope();
        var rows = await scope.ServiceProvider.GetRequiredService<BlogDbContext>().Widget.AsNoTracking().ToListAsync(ct);
        return rows.OrderBy(widget => widget.Region).ThenBy(widget => widget.SortOrder).Select(WidgetSnapshot.From).ToArray();
    }

    private sealed class AuditFault : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct = default)
        {
            if (eventData.Context!.ChangeTracker.Entries<ActivityLogEntity>().Any(entry => entry.State == EntityState.Added))
                throw new InvalidOperationException("Synthetic audit fault.");
            return ValueTask.FromResult(result);
        }
    }

    private sealed class PersistenceFault : SaveChangesInterceptor
    {
        public bool Fail { get; set; }
        public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken ct = default)
        {
            if (Fail) throw new InvalidOperationException("Synthetic failure after executing the writes.");
            return ValueTask.FromResult(result);
        }
    }
}
