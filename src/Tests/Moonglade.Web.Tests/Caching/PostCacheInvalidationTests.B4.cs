using Edi.CacheAside.InMemory;
using LiteBus.Events.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Features.Post;
using Moonglade.Web.Handlers;
using Moq;
using System.Text.Json;

namespace Moonglade.Web.Tests.Caching;

public partial class PostCacheInvalidationTests
{
    private static readonly DateTimeOffset B4Cutoff = new(2026, 12, 31, 23, 59, 59, TimeSpan.Zero);
    private static readonly DateTimeOffset B4Execution = B4Cutoff.AddSeconds(2);

    [Fact]
    public async Task ScheduledPublication_RealBusMixedBatch_PreservesPublicationCachesAndExactlyOneAuditPerPost()
    {
        using var environment = await TestEnvironment.CreateAsync();
        using var scope = environment.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();
        var first = await db.Post.SingleAsync(TestContext.Current.CancellationToken);
        SetScheduled(first, B4Cutoff.UtcDateTime.AddTicks(-1));
        var second = B4Post("second-post", PostStatus.Scheduled, B4Cutoff.UtcDateTime);
        PostEntity[] ineligible =
        [
            B4Post("future", PostStatus.Scheduled, B4Cutoff.UtcDateTime.AddTicks(1)),
            B4Post("null-schedule", PostStatus.Scheduled),
            B4Post("deleted", PostStatus.Scheduled, B4Cutoff.UtcDateTime),
            B4Post("draft", PostStatus.Draft, B4Cutoff.UtcDateTime),
            B4Post("already-published", PostStatus.Published, B4Cutoff.UtcDateTime)
        ];
        ineligible[2].IsDeleted = true;
        db.AddRange([second, .. ineligible]);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var untouched = ineligible.Select(B4State).ToArray();
        var modified = new[] { first.LastModifiedUtc, second.LastModifiedUtc };
        var primed = environment.PrimeCaches();
        var oldFeed = environment.Invalidator.GetFeedCacheKey("rss");
        var oldSitemap = environment.Invalidator.SiteMapCacheKey;
        var events = scope.ServiceProvider.GetRequiredService<IEventMediator>();
        var handler = B4Handler(db, events);

        Assert.Equal(2, await handler.HandleAsync(new(), TestContext.Current.CancellationToken));

        db.ChangeTracker.Clear();
        foreach (var (id, slug, lastModified) in new[] { (first.Id, first.Slug, modified[0]), (second.Id, second.Slug, modified[1]) })
        {
            var saved = await db.Post.SingleAsync(post => post.Id == id, TestContext.Current.CancellationToken);
            Assert.Equal(PostStatus.Published, saved.PostStatus);
            Assert.Equal(B4Execution.UtcDateTime, saved.PubDateUtc);
            Assert.Equal($"2027/1/1/{slug}", saved.RouteLink);
            Assert.Null(saved.ScheduledPublishTimeUtc);
            Assert.Equal(lastModified, saved.LastModifiedUtc);
        }
        var persistedIneligible = await db.Post.AsNoTracking().Where(post => post.Id != first.Id && post.Id != second.Id)
            .ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(untouched.OrderBy(post => post.Id), persistedIneligible.Select(B4State).OrderBy(post => post.Id));
        var sitemap = await SiteMapMapHandler.Handle(new Microsoft.AspNetCore.Http.DefaultHttpContext(),
            environment.Services.GetRequiredService<Moonglade.Configuration.IBlogConfig>(),
            environment.Cache, environment.Invalidator, db);
        var sitemapText = Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.ContentHttpResult>(sitemap).ResponseContent;
        Assert.Contains("2027/1/1/second-post", sitemapText);
        foreach (var (partition, key) in primed)
            Assert.Equal(key == "sitemap" ? sitemapText : "fresh",
                environment.Cache.GetOrCreate(partition, environment.CacheKey(partition, key), () => "fresh"));
        Assert.Equal("1:probe", environment.Invalidator.GetCacheKey("probe"));
        Assert.Equal("1:rss", environment.Invalidator.GetFeedCacheKey("rss"));
        Assert.Equal("1:sitemap", environment.Invalidator.SiteMapCacheKey);
        Assert.NotEqual(oldFeed, environment.Invalidator.GetFeedCacheKey("rss"));
        Assert.NotEqual(oldSitemap, environment.Invalidator.SiteMapCacheKey);
        foreach (var key in TestEnvironment.UnrelatedKeys)
            Assert.Equal("preserved", environment.Cache.GetOrCreate("General", key, () => "missing"));
        Assert.Equal("preserved", environment.Cache.GetOrCreate("Page", "about", () => "missing"));
        Assert.Contains("second-post", await environment.ReadFeedAsync(false, null));
        Assert.Contains("second-post", await environment.ReadFeedAsync(true, null));
        var logs = await db.ActivityLog.AsNoTracking().OrderBy(row => row.Id).ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, logs.Count);
        Assert.Equal(new[] { first.Id.ToString(), second.Id.ToString() }.Order(), logs.Select(row => row.TargetId).Order());
        foreach (var row in logs)
        {
            Assert.Equal("post.automatically-published", row.EventKey);
            Assert.Equal(ActivityActorKind.System, row.ActorKind);
            Assert.Equal("Old title", row.TargetDisplayName);
            Assert.Equal(B4Execution.UtcDateTime, row.EventTimeUtc);
            Assert.Null(row.ActorDisplayName);
            Assert.Null(row.IpAddress);
            Assert.Null(row.UserAgent);
            using var details = JsonDocument.Parse(row.Details!);
            var field = Assert.Single(details.RootElement.EnumerateObject());
            Assert.Equal("PostId", field.Name);
            Assert.Equal(Guid.Parse(row.TargetId!), field.Value.GetGuid());
        }
        Assert.Equal(0, await B4Handler(db, events).HandleAsync(new(), TestContext.Current.CancellationToken));
        Assert.Equal(logs.Select(row => row.Id), await db.ActivityLog.OrderBy(row => row.Id).Select(row => row.Id).ToListAsync(TestContext.Current.CancellationToken));
        Assert.Equal("1:probe", environment.Invalidator.GetCacheKey("probe"));
        Assert.Equal("1:rss", environment.Invalidator.GetFeedCacheKey("rss"));
        Assert.Equal("1:sitemap", environment.Invalidator.SiteMapCacheKey);
    }

    [Fact]
    public async Task ScheduledPublication_OneSqliteActivityInsertFails_ContinuesAndNeverResurrectsFailedEntry()
    {
        using var environment = await TestEnvironment.CreateAsync();
        using var scope = environment.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();
        var first = await db.Post.SingleAsync(TestContext.Current.CancellationToken);
        SetScheduled(first, B4Cutoff.UtcDateTime);
        var second = B4Post("second-failure-policy", PostStatus.Scheduled, B4Cutoff.UtcDateTime);
        db.Add(second);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        // SQLite trigger DDL cannot parameterize its predicate; the only substitution is a generated Guid.
        var trigger = $"CREATE TRIGGER b4_reject_one_activity BEFORE INSERT ON ActivityLog WHEN NEW.TargetId = '{first.Id:D}' BEGIN SELECT RAISE(ABORT, 'B4-PRIVATE-INSERT-FAILURE'); END;";
        await db.Database.ExecuteSqlRawAsync(trigger, TestContext.Current.CancellationToken);
        environment.PrimeCaches();

        Assert.Equal(2, await B4Handler(db, scope.ServiceProvider.GetRequiredService<IEventMediator>())
            .HandleAsync(new(), TestContext.Current.CancellationToken));

        Assert.Equal(2, await db.Post.CountAsync(post => post.PostStatus == PostStatus.Published, TestContext.Current.CancellationToken));
        Assert.Equal("1:probe", environment.Invalidator.GetCacheKey("probe"));
        Assert.Equal("1:rss", environment.Invalidator.GetFeedCacheKey("rss"));
        Assert.Equal("1:sitemap", environment.Invalidator.SiteMapCacheKey);
        var row = Assert.Single(await db.ActivityLog.AsNoTracking().ToListAsync(TestContext.Current.CancellationToken));
        Assert.Equal(second.Id.ToString(), row.TargetId);
        Assert.Equal(B4Execution.UtcDateTime, row.EventTimeUtc);
        Assert.DoesNotContain(db.ChangeTracker.Entries<ActivityLogEntity>(), entry => entry.State == EntityState.Added);
        // Removing the failure condition makes an accidental retained Added row observable.
        await db.Database.ExecuteSqlRawAsync("DROP TRIGGER b4_reject_one_activity;", TestContext.Current.CancellationToken);
        db.BlogConfiguration.Add(new BlogConfigurationEntity { CfgKey = "B4-unrelated-save", CfgValue = "retained", LastModifiedTimeUtc = B4Execution.UtcDateTime });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        Assert.Equal(second.Id.ToString(), (await db.ActivityLog.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken)).TargetId);
        Assert.Equal(0, await B4Handler(db, scope.ServiceProvider.GetRequiredService<IEventMediator>())
            .HandleAsync(new(), TestContext.Current.CancellationToken));
        Assert.Equal(1, await db.ActivityLog.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ScheduledPublication_RealCacheFailsAfterCommit_StillPersistsAuditAndRethrowsOriginalFailure()
    {
        using var environment = await TestEnvironment.CreateAsync();
        using var scope = environment.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();
        var post = await db.Post.SingleAsync(TestContext.Current.CancellationToken);
        SetScheduled(post, B4Cutoff.UtcDateTime);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        environment.PrimeCaches();
        var failure = new InvalidOperationException("B4 synthetic cache failure");
        var cache = new Mock<ICacheAside>();
        cache.Setup(value => value.Remove("Post")).Throws(failure);
        var failingInvalidator = new PostCacheInvalidator(cache.Object);
        var realBus = scope.ServiceProvider.GetRequiredService<IEventMediator>();
        var events = new Mock<IEventMediator>();
        events.Setup(value => value.PublishAsync(It.IsAny<PostChangedEvent>(), It.IsAny<EventMediationSettings>(), It.IsAny<CancellationToken>()))
            .Returns((PostChangedEvent item, EventMediationSettings _, CancellationToken token) =>
                new Moonglade.Web.Handlers.PostChangedEventHandler(failingInvalidator).HandleAsync(item, token));
        events.Setup(value => value.PublishAsync(It.IsAny<ScheduledPostsPublishedEvent>(), It.IsAny<EventMediationSettings>(), It.IsAny<CancellationToken>()))
            .Returns((ScheduledPostsPublishedEvent item, EventMediationSettings _, CancellationToken token) => realBus.PublishAsync(item, cancellationToken: token));

        Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() =>
            B4Handler(db, events.Object).HandleAsync(new(), TestContext.Current.CancellationToken)));

        db.ChangeTracker.Clear();
        var saved = await db.Post.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(PostStatus.Published, saved.PostStatus);
        Assert.Equal(B4Execution.UtcDateTime, saved.PubDateUtc);
        Assert.Equal("2027/1/1/old-post", saved.RouteLink);
        var row = Assert.Single(await db.ActivityLog.AsNoTracking().ToListAsync(TestContext.Current.CancellationToken));
        Assert.Equal(post.Id.ToString(), row.TargetId);
        Assert.Equal(B4Execution.UtcDateTime, row.EventTimeUtc);
        Assert.Equal("1:probe", failingInvalidator.GetCacheKey("probe"));
        Assert.Equal("0:rss", failingInvalidator.GetFeedCacheKey("rss"));
        Assert.Equal("0:sitemap", failingInvalidator.SiteMapCacheKey);
        Assert.Equal(0, await B4Handler(db, events.Object).HandleAsync(new(), TestContext.Current.CancellationToken));
        Assert.Equal(1, await db.ActivityLog.CountAsync(TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData("save-failure")]
    [InlineData("cancelled-before-query")]
    public async Task ScheduledPublication_UncommittedSqlitePass_LeavesPostCachesAndActivityUntouched(string scenario)
    {
        var ct = TestContext.Current.CancellationToken;
        using var environment = await TestEnvironment.CreateAsync();
        using var scope = environment.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();
        var post = await db.Post.SingleAsync(ct);
        SetScheduled(post, B4Cutoff.UtcDateTime);
        await db.SaveChangesAsync(ct);
        var original = B4State(post);
        var primed = environment.PrimeCaches();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var handler = B4Handler(db, scope.ServiceProvider.GetRequiredService<IEventMediator>());
        if (scenario == "save-failure")
        {
            await db.Database.ExecuteSqlRawAsync(
                "CREATE TRIGGER b4_reject_post BEFORE UPDATE ON Post BEGIN SELECT RAISE(ABORT, 'B4 synthetic scheduled save failure'); END;", ct);
            await Assert.ThrowsAsync<DbUpdateException>(() => handler.HandleAsync(new(), ct));
        }
        else
        {
            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => handler.HandleAsync(new(), cancellation.Token));
        }

        db.ChangeTracker.Clear();
        Assert.Equal(original, B4State(await db.Post.SingleAsync(ct)));
        Assert.Empty(await db.ActivityLog.AsNoTracking().ToListAsync(ct));
        Assert.Equal("0:probe", environment.Invalidator.GetCacheKey("probe"));
        Assert.Equal("0:rss", environment.Invalidator.GetFeedCacheKey("rss"));
        Assert.Equal("0:sitemap", environment.Invalidator.SiteMapCacheKey);
        foreach (var (partition, key) in primed)
            Assert.Equal("cached", environment.Cache.GetOrCreate(partition, environment.CacheKey(partition, key), () => "missing"));
    }

    private static PublishScheduledPostCommandHandler B4Handler(BlogDbContext db, IEventMediator events) =>
        new(db, new B4SequenceTimeProvider(), events, NullLogger<PublishScheduledPostCommandHandler>.Instance);

    private static PostEntity B4Post(string slug, PostStatus status, DateTime? schedule = null)
    {
        var post = TestEnvironment.CreatePost(Guid.NewGuid(), B4Cutoff.UtcDateTime.AddDays(-1));
        post.Slug = slug;
        post.PostStatus = status;
        post.ScheduledPublishTimeUtc = schedule;
        if (status != PostStatus.Published) { post.PubDateUtc = null; post.RouteLink = null; }
        return post;
    }

    private static void SetScheduled(PostEntity post, DateTime schedule)
    {
        post.PostStatus = PostStatus.Scheduled;
        post.ScheduledPublishTimeUtc = schedule;
        post.PubDateUtc = null;
        post.RouteLink = null;
    }

    private static (Guid Id, PostStatus Status, bool Deleted, DateTime? Schedule, DateTime? Published, string? Route, DateTime? Modified)
        B4State(PostEntity post) => (post.Id, post.PostStatus, post.IsDeleted, post.ScheduledPublishTimeUtc, post.PubDateUtc, post.RouteLink, post.LastModifiedUtc);

    private sealed class B4SequenceTimeProvider : TimeProvider
    {
        private int _calls;
        public override DateTimeOffset GetUtcNow() => _calls++ == 0 ? B4Cutoff : B4Execution;
    }
}
