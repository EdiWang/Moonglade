using LiteBus.Commands.Abstractions;
using LiteBus.Events.Abstractions;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Features.Post;
using Moonglade.Web.Controllers;
using Moonglade.Web.Tests.TestSupport;
using Moq;
using System.Globalization;
using System.Net;
using System.Text.Json;

namespace Moonglade.Web.Tests.Controllers;

public partial class ActivityLogHttpTests
{
    [Theory]
    [InlineData("PUT", "/api/schedule/cancel/{0}")]
    [InlineData("PUT", "/api/schedule/postpone/{0}?hours=2")]
    [InlineData("PUT", "/api/post/{0}/publish")]
    [InlineData("PUT", "/api/post/{0}/unpublish")]
    [InlineData("DELETE", "/api/post/{0}/recycle")]
    [InlineData("POST", "/api/post/{0}/restore", true)]
    [InlineData("DELETE", "/api/post/{0}/destroy", true)]
    public async Task StalePostWrite_ReturnsConflictAndPreservesWinningWrite(string method, string route, bool recycled = false)
    {
        var ct = TestContext.Current.CancellationToken;
        var intercept = new BeforePostWrite();
        await using var fixture = await ActivityLogHttpFixture.CreateAsync(0, ct,
            configureServices: services => services.ConfigureDbContext<BlogDbContext>(options => options.AddInterceptors(intercept)),
            additionalControllers: [typeof(ScheduleController), typeof(PostController), typeof(RecycleBinController)]);
        using var scope = fixture.App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();
        var post = B4HttpPost("Scheduled article", "scheduled-article", DateTime.UtcNow.AddMinutes(-1));
        post.IsDeleted = recycled;
        db.Post.Add(post);
        await db.SaveChangesAsync(ct);
        intercept.CommitCompetingWrite = async token =>
        {
            if (recycled)
            {
                post.IsDeleted = false;
                await db.SaveChangesAsync(token);
            }
            else
            {
                var commands = scope.ServiceProvider.GetRequiredService<ICommandMediator>();
                Assert.Equal(1, await commands.SendAsync(new PublishScheduledPostCommand(), cancellationToken: token));
            }
        };

        using var client = fixture.App.GetTestClient();
        client.DefaultRequestHeaders.Add("X-B1-User", "admin");
        await AddPageToken(client, ct);
        using var request = new HttpRequestMessage(new HttpMethod(method), string.Format(CultureInfo.InvariantCulture, route, post.Id));
        request.Headers.Accept.ParseAdd("application/json");
        using var response = await client.SendAsync(request, ct);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        Assert.Equal(409, problem.RootElement.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(problem.RootElement.GetProperty("detail").GetString()));
        var saved = await db.Post.AsNoTracking().SingleAsync(ct);
        Assert.False(saved.IsDeleted);
        Assert.Equal(recycled ? PostStatus.Scheduled : PostStatus.Published, saved.PostStatus);
        var logs = await fixture.Store.ReadLogsAsync(ct);
        if (recycled) Assert.Empty(logs);
        else Assert.Equal("post.automatically-published", Assert.Single(logs).EventKey);
    }

    private sealed class BeforePostWrite : SaveChangesInterceptor
    {
        public Func<CancellationToken, Task>? CommitCompetingWrite { get; set; }

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (CommitCompetingWrite is { } commit && eventData.Context!.ChangeTracker.Entries<PostEntity>()
                .Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            {
                CommitCompetingWrite = null;
                await commit(cancellationToken);
            }
            return result;
        }
    }

    [Theory]
    [InlineData("en-US")]
    [InlineData("zh-Hans")]
    [InlineData("zh-Hant")]
    [InlineData("de-DE")]
    [InlineData("ja-JP")]
    [InlineData("fr-FR")]
    public async Task AutomaticPublication_DetachedRenameDeleteSnapshots_KeepLocalizedFilterListDetailIdentityAndOccurrence(string culture)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fixture = await ActivityLogHttpFixture.CreateAsync(0, ct);
        using var scope = fixture.App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();
        var occurrence = new DateTime(2026, 7, 1, 0, 0, 2, DateTimeKind.Utc);
        const string title = "<script>B4-detached title</script>";
        var first = B4HttpPost(title, "rename", occurrence.AddSeconds(-2));
        var second = B4HttpPost(title, "delete", occurrence.AddSeconds(-3));
        db.Post.AddRange(first, second);
        await db.SaveChangesAsync(ct);
        ScheduledPostsPublishedEvent? captured = null;
        var events = new Mock<IEventMediator>();
        events.Setup(m => m.PublishAsync(It.IsAny<ScheduledPostsPublishedEvent>(), It.IsAny<EventMediationSettings>(), It.IsAny<CancellationToken>()))
            .Callback((ScheduledPostsPublishedEvent item, EventMediationSettings _, CancellationToken _) => captured = item)
            .Returns(Task.CompletedTask);
        var feature = new PublishScheduledPostCommandHandler(db, new B4HttpTimeProvider(occurrence), events.Object,
            NullLogger<PublishScheduledPostCommandHandler>.Instance);
        Assert.Equal(2, await feature.HandleAsync(new(), ct));
        Assert.NotNull(captured);
        first.Title = "B4-live renamed title";
        db.Post.Remove(second);
        await db.SaveChangesAsync(ct);

        await scope.ServiceProvider.GetRequiredService<IEventMediator>().PublishAsync(captured, cancellationToken: ct);

        Assert.Equal("B4-live renamed title", (await db.Post.AsNoTracking().SingleAsync(ct)).Title);
        Assert.False(await db.Post.AnyAsync(post => post.Id == second.Id, ct));
        var rows = await fixture.Store.ReadLogsAsync(ct);
        Assert.Equal(2, rows.Count);
        using var client = fixture.App.GetTestClient();
        client.DefaultRequestHeaders.Add("X-B1-User", "admin");
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd(culture);
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
            var localizer = scope.ServiceProvider.GetRequiredService<IStringLocalizer<Program>>();
            var label = localizer["Post automatically published"].Value;
            using var filters = JsonDocument.Parse(await client.GetStringAsync("/api/activitylog/eventtypes", ct));
            var group = Assert.Single(filters.RootElement.EnumerateArray(),
                value => value.GetProperty("label").GetString() == localizer["Posts"].Value);
            var filter = Assert.Single(group.GetProperty("items").EnumerateArray(),
                value => value.GetProperty("key").GetString() == "post.automatically-published");
            Assert.Equal(label, filter.GetProperty("label").GetString());
            if (culture != "en-US") Assert.NotEqual("Post automatically published", label);
            using var list = JsonDocument.Parse(await client.GetStringAsync(
                "/api/activitylog/list?pageSize=10&eventKeys=post.automatically-published", ct));
            Assert.Equal(2, list.RootElement.GetProperty("totalItemCount").GetInt32());
            var items = list.RootElement.GetProperty("items").EnumerateArray().ToArray();
            Assert.Equal(rows.Select(row => row.Id).Order(), items.Select(item => item.GetProperty("id").GetInt64()).Order());
            foreach (var item in items)
            {
                Assert.Equal("post.automatically-published", item.GetProperty("eventKey").GetString());
                Assert.Equal("System", item.GetProperty("actorKind").GetString());
                Assert.Equal(localizer["System"].Value, item.GetProperty("actor").GetString());
                Assert.Equal(label + ": " + title, item.GetProperty("summary").GetString());
                Assert.Equal(occurrence, item.GetProperty("eventTimeUtc").GetDateTime());
                using var detail = JsonDocument.Parse(await client.GetStringAsync($"/api/activitylog/{item.GetProperty("id").GetInt64()}/metadata", ct));
                var value = detail.RootElement;
                var row = Assert.Single(rows, row => row.Id == item.GetProperty("id").GetInt64());
                Assert.Equal("post.automatically-published", value.GetProperty("eventKey").GetString());
                Assert.Equal(label, value.GetProperty("heading").GetString());
                Assert.Equal(title, value.GetProperty("targetDisplayName").GetString());
                Assert.Equal(row.TargetId, value.GetProperty("targetId").GetString());
                Assert.Contains(row.TargetId, new[] { first.Id.ToString(), second.Id.ToString() });
                Assert.Equal(occurrence, value.GetProperty("eventTimeUtc").GetDateTime());
                Assert.Equal("System", value.GetProperty("actorKind").GetString());
                Assert.Equal(localizer["System"].Value, value.GetProperty("actor").GetString());
                Assert.Equal(JsonValueKind.Null, value.GetProperty("ipAddress").ValueKind);
                Assert.Equal(JsonValueKind.Null, value.GetProperty("userAgent").ValueKind);
                var field = Assert.Single(value.GetProperty("fields").EnumerateArray());
                Assert.Equal("PostId", field.GetProperty("key").GetString());
                Assert.Equal(localizer["Post ID"].Value, field.GetProperty("label").GetString());
                Assert.Equal(Guid.Parse(row.TargetId!), field.GetProperty("value").GetGuid());
            }
            var page = await client.GetStringAsync("/admin/activitylogs", ct);
            Assert.Contains("x-text=\"log.summary\"", page);
            Assert.Contains("x-text=\"currentDetail.targetDisplayName\"", page);
            Assert.DoesNotContain("x-html=\"currentDetail.targetDisplayName\"", page);
        }
        finally { CultureInfo.CurrentUICulture = previous; }
    }

    private static PostEntity B4HttpPost(string title, string slug, DateTime due) => new()
    {
        Id = Guid.NewGuid(),
        Title = title,
        Slug = slug,
        Author = "B4 author",
        PostContent = "B4 body not audited",
        ContentAbstract = "B4 abstract not audited",
        PostStatus = PostStatus.Scheduled,
        ScheduledPublishTimeUtc = due,
        CreateTimeUtc = due.AddDays(-1),
        LastModifiedUtc = due.AddHours(-1),
        ContentType = "html",
        ContentLanguageCode = "en-us",
        IsFeedIncluded = true
    };

    private sealed class B4HttpTimeProvider(DateTime occurrence) : TimeProvider
    {
        private int _calls;
        public override DateTimeOffset GetUtcNow() => new(_calls++ == 0 ? occurrence.AddSeconds(-2) : occurrence);
    }
}
