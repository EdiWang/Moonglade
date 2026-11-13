using LiteBus.Events.Abstractions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Features.Post;
using Moq;

namespace Moonglade.Features.Tests.Post;

public partial class PublishScheduledPostCommandTests
{
    [Fact]
    public async Task HandleAsync_OverlappingWorkers_PublishesAndNotifiesOnce()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(ct);
        var options = new DbContextOptionsBuilder<BlogDbContext>().UseSqlite(connection).Options;
        await using var winnerDb = new BlogDbContext(options);
        await winnerDb.Database.EnsureCreatedAsync(ct);
        var cutoff = new DateTimeOffset(2026, 12, 31, 23, 59, 59, TimeSpan.Zero);
        var post = CreatePostEntity(Guid.NewGuid(), PostStatus.Scheduled, "overlap", cutoff.UtcDateTime);
        winnerDb.Post.Add(post);
        await winnerDb.SaveChangesAsync(ct);

        var pause = new ScheduledSaveProbe();
        await using var staleDb = new BlogDbContext(new DbContextOptionsBuilder<BlogDbContext>(options)
            .AddInterceptors(pause).Options);
        var staleSaving = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var resumeStale = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        pause.BeforeSave = async token =>
        {
            staleSaving.SetResult();
            await resumeStale.Task.WaitAsync(token);
        };
        var winnerEvents = new List<IEvent>();
        var staleEvents = new List<IEvent>();
        var winner = new PublishScheduledPostCommandHandler(winnerDb,
            new SequenceTimeProvider(cutoff, cutoff), RecordingEvents(winnerEvents).Object,
            NullLogger<PublishScheduledPostCommandHandler>.Instance);
        var stale = new PublishScheduledPostCommandHandler(staleDb,
            new SequenceTimeProvider(cutoff, cutoff.AddSeconds(2)), RecordingEvents(staleEvents).Object,
            NullLogger<PublishScheduledPostCommandHandler>.Instance);

        var staleResult = stale.HandleAsync(new(), ct);
        try
        {
            await staleSaving.Task.WaitAsync(TimeSpan.FromSeconds(5), ct);
            Assert.Equal(1, await winner.HandleAsync(new(), ct));
        }
        finally
        {
            resumeStale.TrySetResult();
        }

        Assert.Equal(0, await staleResult);
        Assert.Empty(staleEvents);
        var publication = Assert.Single(Assert.Single(winnerEvents.OfType<ScheduledPostsPublishedEvent>()).Posts);
        Assert.Equal(post.Id, publication.PostId);
        Assert.Equal(cutoff.UtcDateTime, publication.PublishedTimeUtc);
        var saved = await winnerDb.Post.AsNoTracking().SingleAsync(ct);
        Assert.Equal("2026/12/31/overlap", saved.RouteLink);
        Assert.Equal(cutoff.UtcDateTime, saved.PubDateUtc);
    }

    [Fact]
    public async Task HandleAsync_MixedEligibility_UsesCutoffAndExecutionTimeAndDoesNotRepeat()
    {
        using var db = CreateDbContext();
        var cutoff = new DateTimeOffset(2026, 12, 31, 23, 59, 59, TimeSpan.Zero);
        var execution = cutoff.AddSeconds(2);
        var before = CreatePostEntity(Guid.NewGuid(), PostStatus.Scheduled, "before", cutoff.UtcDateTime.AddTicks(-1));
        var at = CreatePostEntity(Guid.NewGuid(), PostStatus.Scheduled, "at", cutoff.UtcDateTime);
        before.Title = "Before title";
        at.Title = "At title";
        PostEntity[] ineligible =
        [
            CreatePostEntity(Guid.NewGuid(), PostStatus.Scheduled, "after", cutoff.UtcDateTime.AddTicks(1)),
            CreatePostEntity(Guid.NewGuid(), PostStatus.Scheduled, "null"),
            CreatePostEntity(Guid.NewGuid(), PostStatus.Scheduled, "deleted", cutoff.UtcDateTime.AddMinutes(-1)),
            CreatePostEntity(Guid.NewGuid(), PostStatus.Draft, "draft", cutoff.UtcDateTime.AddMinutes(-1)),
            CreatePostEntity(Guid.NewGuid(), PostStatus.Published, "published", cutoff.UtcDateTime.AddMinutes(-1), cutoff.UtcDateTime.AddDays(-1))
        ];
        ineligible[2].IsDeleted = true;
        db.Post.AddRange([before, at, .. ineligible]);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var untouched = ineligible.Select(State).ToArray();
        var modified = new BlogConfigurationEntity { CfgKey = "B4-count", CfgValue = "retained", LastModifiedTimeUtc = cutoff.UtcDateTime };
        db.BlogConfiguration.Add(modified);
        var lastModified = new[] { before.LastModifiedUtc, at.LastModifiedUtc };
        var notices = new List<IEvent>();
        var events = RecordingEvents(notices);
        var handler = new PublishScheduledPostCommandHandler(db, new SequenceTimeProvider(cutoff, execution), events.Object,
            NullLogger<PublishScheduledPostCommandHandler>.Instance);

        Assert.Equal(3, await handler.HandleAsync(new(), TestContext.Current.CancellationToken));

        Assert.Equal([typeof(PostChangedEvent), typeof(ScheduledPostsPublishedEvent)], notices.Select(item => item.GetType()));
        var publications = Assert.IsType<ScheduledPostsPublishedEvent>(notices[1]).Posts;
        Assert.Equal(new[] { (before.Id, "Before title", execution.UtcDateTime), (at.Id, "At title", execution.UtcDateTime) },
            publications.Select(item => (item.PostId, item.Title, item.PublishedTimeUtc)));
        foreach (var (post, index) in new[] { before, at }.Select((post, index) => (post, index)))
        {
            Assert.Equal(PostStatus.Published, post.PostStatus);
            Assert.Equal(execution.UtcDateTime, post.PubDateUtc);
            Assert.Equal($"2027/1/1/{post.Slug}", post.RouteLink);
            Assert.Null(post.ScheduledPublishTimeUtc);
            Assert.Equal(lastModified[index], post.LastModifiedUtc);
        }
        Assert.Equal(untouched, ineligible.Select(State));
        // The immediately-after-cutoff row must remain ineligible on the second pass as well.
        var repeat = new PublishScheduledPostCommandHandler(db, new SequenceTimeProvider(cutoff, execution), events.Object,
            NullLogger<PublishScheduledPostCommandHandler>.Instance);
        Assert.Equal(0, await repeat.HandleAsync(new(), TestContext.Current.CancellationToken));
        Assert.Equal(2, notices.Count);
        Assert.Equal(untouched, ineligible.Select(State));
    }

    [Fact]
    public async Task HandleAsync_EmptySelection_DoesNotSaveOrNotify()
    {
        var probe = new ScheduledSaveProbe();
        using var db = ProbeDb(probe);
        db.BlogConfiguration.Add(new BlogConfigurationEntity { CfgKey = "must-not-save", CfgValue = "pending", LastModifiedTimeUtc = DueCutoffUtc.UtcDateTime });
        var events = new Mock<IEventMediator>(MockBehavior.Strict);

        Assert.Equal(0, await ProbeHandler(db, events.Object).HandleAsync(new(), TestContext.Current.CancellationToken));

        Assert.Equal(0, probe.Saves);
        Assert.Empty(events.Invocations);
        Assert.Equal(EntityState.Added, db.ChangeTracker.Entries<BlogConfigurationEntity>().Single().State);
    }

    [Theory]
    [InlineData("save-failure")]
    [InlineData("precommit-cancellation")]
    public async Task HandleAsync_UnsuccessfulSave_DoesNotNotifyOrPersistPublication(string scenario)
    {
        var probe = new ScheduledSaveProbe();
        using var db = ProbeDb(probe);
        var post = CreatePostEntity(Guid.NewGuid(), PostStatus.Scheduled, "not-committed", DueCutoffUtc.UtcDateTime);
        db.Post.Add(post);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var original = State(post);
        Exception failure = scenario == "save-failure"
            ? new DbUpdateException("B4 synthetic save failure")
            : new OperationCanceledException("B4 synthetic precommit cancellation");
        probe.Failure = failure;
        var events = new Mock<IEventMediator>(MockBehavior.Strict);

        var actual = await Assert.ThrowsAnyAsync<Exception>(() => ProbeHandler(db, events.Object)
            .HandleAsync(new(), TestContext.Current.CancellationToken));

        Assert.Same(failure, actual);
        Assert.Empty(events.Invocations);
        db.ChangeTracker.Clear();
        Assert.Equal(original, State(await db.Post.SingleAsync(TestContext.Current.CancellationToken)));
    }

    [Fact]
    public async Task HandleAsync_ZeroRowSave_DoesNotNotify()
    {
        var probe = new ScheduledSaveProbe();
        using var db = ProbeDb(probe);
        db.Post.Add(CreatePostEntity(Guid.NewGuid(), PostStatus.Scheduled, "zero-save", DueCutoffUtc.UtcDateTime));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        probe.SuppressSave = true;
        var events = new Mock<IEventMediator>(MockBehavior.Strict);

        Assert.Equal(0, await ProbeHandler(db, events.Object).HandleAsync(new(), TestContext.Current.CancellationToken));
        Assert.Empty(events.Invocations);
        db.ChangeTracker.Clear();
        Assert.Equal(PostStatus.Scheduled, (await db.Post.SingleAsync(TestContext.Current.CancellationToken)).PostStatus);
    }

    [Fact]
    public async Task HandleAsync_PostCommitCancellation_UsesNoncancelableNotifications()
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var probe = new ScheduledSaveProbe();
        using var db = ProbeDb(probe);
        db.Post.Add(CreatePostEntity(Guid.NewGuid(), PostStatus.Scheduled, "committed", DueCutoffUtc.UtcDateTime));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        probe.AfterSave = cancellation.Cancel;
        var notices = new List<IEvent>();
        var events = RecordingEvents(notices);

        Assert.Equal(1, await ProbeHandler(db, events.Object).HandleAsync(new(), cancellation.Token));

        Assert.True(cancellation.IsCancellationRequested);
        Assert.Equal([typeof(PostChangedEvent), typeof(ScheduledPostsPublishedEvent)], notices.Select(item => item.GetType()));
        Assert.All(events.Invocations, invocation => Assert.Equal(CancellationToken.None, invocation.Arguments[2]));
        db.ChangeTracker.Clear();
        Assert.Equal(PostStatus.Published, (await db.Post.SingleAsync(TestContext.Current.CancellationToken)).PostStatus);
    }

    [Fact]
    public async Task HandleAsync_CacheSubscriberMutatesPosts_HandsOffDetachedReadOnlySnapshot()
    {
        using var db = CreateDbContext();
        var first = CreatePostEntity(Guid.NewGuid(), PostStatus.Scheduled, "first", DueCutoffUtc.UtcDateTime);
        var second = CreatePostEntity(Guid.NewGuid(), PostStatus.Scheduled, "second", DueCutoffUtc.UtcDateTime);
        first.Title = "Captured first";
        second.Title = "Captured second";
        db.Post.AddRange(first, second);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        ScheduledPostsPublishedEvent? captured = null;
        var events = new Mock<IEventMediator>();
        WireEvents(events, (item, token) =>
            {
                Assert.Equal(CancellationToken.None, token);
                if (item is PostChangedEvent)
                {
                    first.Title = "Renamed first";
                    first.PubDateUtc = DueCutoffUtc.UtcDateTime.AddDays(20);
                    db.Post.Remove(second);
                }
                else captured = Assert.IsType<ScheduledPostsPublishedEvent>(item);
                return Task.CompletedTask;
            });

        Assert.Equal(2, await ProbeHandler(db, events.Object).HandleAsync(new(), TestContext.Current.CancellationToken));

        Assert.NotNull(captured);
        var expected = new[]
        {
            (first.Id, "Captured first", SuccessfulPublishTimeUtc.UtcDateTime),
            (second.Id, "Captured second", SuccessfulPublishTimeUtc.UtcDateTime)
        };
        Assert.Equal(expected, captured.Posts.Select(post => (post.PostId, post.Title, post.PublishedTimeUtc)));
        if (captured.Posts is IList<ScheduledPostPublication> list)
        {
            Assert.True(list.IsReadOnly);
            Assert.Throws<NotSupportedException>(() => list.RemoveAt(0));
            Assert.Throws<NotSupportedException>(() => list[0] = new(Guid.NewGuid(), "replacement", DueCutoffUtc.UtcDateTime));
        }
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        Assert.Equal(expected, captured.Posts.Select(post => (post.PostId, post.Title, post.PublishedTimeUtc)));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task HandleAsync_NotificationFailures_PreservePublicationAndOriginalCacheException(bool cacheFails, bool handoffFails)
    {
        using var db = CreateDbContext();
        db.Post.Add(CreatePostEntity(Guid.NewGuid(), PostStatus.Scheduled, "failure-policy", DueCutoffUtc.UtcDateTime));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        const string secret = "B4-PAYLOAD-EXCEPTION-SENTINEL";
        var cacheFailure = new InvalidOperationException(secret);
        var handoffFailure = new OperationCanceledException(secret);
        var calls = new List<Type>();
        var events = new Mock<IEventMediator>();
        WireEvents(events, (item, token) =>
            {
                calls.Add(item.GetType());
                Assert.Equal(CancellationToken.None, token);
                return item switch
                {
                    PostChangedEvent when cacheFails => Task.FromException(cacheFailure),
                    ScheduledPostsPublishedEvent when handoffFails => Task.FromException(handoffFailure),
                    _ => Task.CompletedTask
                };
            });
        var logger = new Mock<ILogger<PublishScheduledPostCommandHandler>>();
        var handler = new PublishScheduledPostCommandHandler(db, new SequenceTimeProvider(DueCutoffUtc, SuccessfulPublishTimeUtc), events.Object, logger.Object);

        if (cacheFails)
            Assert.Same(cacheFailure, await Assert.ThrowsAsync<InvalidOperationException>(() => handler.HandleAsync(new(), TestContext.Current.CancellationToken)));
        else
            Assert.Equal(1, await handler.HandleAsync(new(), TestContext.Current.CancellationToken));

        Assert.Equal([typeof(PostChangedEvent), typeof(ScheduledPostsPublishedEvent)], calls);
        db.ChangeTracker.Clear();
        var post = await db.Post.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(PostStatus.Published, post.PostStatus);
        Assert.Equal(SuccessfulPublishTimeUtc.UtcDateTime, post.PubDateUtc);
        Assert.Equal("2026/8/15/failure-policy", post.RouteLink);
        if (handoffFails)
        {
            var diagnostic = Assert.Single(logger.Invocations, invocation => invocation.Method.Name == "Log");
            Assert.Null(diagnostic.Arguments[3]);
            Assert.DoesNotContain(secret, diagnostic.Arguments[2]!.ToString()!);
            Assert.Contains(nameof(OperationCanceledException), diagnostic.Arguments[2]!.ToString()!);
        }
    }

    private static Mock<IEventMediator> RecordingEvents(List<IEvent> notices)
    {
        var events = new Mock<IEventMediator>();
        WireEvents(events, (item, token) =>
            {
                Assert.Equal(CancellationToken.None, token);
                notices.Add(item);
                return Task.CompletedTask;
            });
        return events;
    }

    private static void WireEvents(Mock<IEventMediator> events, Func<IEvent, CancellationToken, Task> publish)
    {
        events.Setup(m => m.PublishAsync(It.IsAny<PostChangedEvent>(), It.IsAny<EventMediationSettings>(), It.IsAny<CancellationToken>()))
            .Returns((PostChangedEvent item, EventMediationSettings _, CancellationToken token) => publish(item, token));
        events.Setup(m => m.PublishAsync(It.IsAny<ScheduledPostsPublishedEvent>(), It.IsAny<EventMediationSettings>(), It.IsAny<CancellationToken>()))
            .Returns((ScheduledPostsPublishedEvent item, EventMediationSettings _, CancellationToken token) => publish(item, token));
    }

    private static PublishScheduledPostCommandHandler ProbeHandler(BlogDbContext db, IEventMediator events) =>
        new(db, new SequenceTimeProvider(DueCutoffUtc, SuccessfulPublishTimeUtc), events,
            NullLogger<PublishScheduledPostCommandHandler>.Instance);

    private static BlogDbContext ProbeDb(ScheduledSaveProbe probe) => new(new DbContextOptionsBuilder<BlogDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).AddInterceptors(probe).Options);

    private static (Guid Id, string Title, PostStatus Status, bool Deleted, DateTime? Schedule, DateTime? Published, string? Route, DateTime? Modified)
        State(PostEntity post) => (post.Id, post.Title, post.PostStatus, post.IsDeleted, post.ScheduledPublishTimeUtc,
            post.PubDateUtc, post.RouteLink, post.LastModifiedUtc);

    private sealed class ScheduledSaveProbe : SaveChangesInterceptor
    {
        public int Saves { get; private set; }
        public Exception? Failure { get; set; }
        public bool SuppressSave { get; set; }
        public Func<CancellationToken, Task>? BeforeSave { get; set; }
        public Action? AfterSave { get; set; }

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            Saves++;
            if (Failure is not null) throw Failure;
            if (BeforeSave is not null) await BeforeSave(cancellationToken);
            return SuppressSave ? InterceptionResult<int>.SuppressWithResult(0) : result;
        }

        public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result,
            CancellationToken cancellationToken = default)
        {
            AfterSave?.Invoke();
            return ValueTask.FromResult(result);
        }
    }
}
