using LiteBus.Commands.Abstractions;
using LiteBus.Events.Abstractions;
using LiteBus.Queries.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moonglade.Configuration;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Features.Post;
using Moq;

namespace Moonglade.BackgroundServices.Tests;

public partial class ScheduledPublishServiceTests
{
    [Theory]
    [InlineData("zero")]
    [InlineData("positive")]
    [InlineData("handoff-failure")]
    [InlineData("cache-failure")]
    public async Task Worker_RealScheduledCommand_RetainsIntResultAndPostCommitFailurePoliciesAndStopsPromptly(string scenario)
    {
        var ct = TestContext.Current.CancellationToken;
        var cutoff = new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero);
        var options = new DbContextOptionsBuilder<BlogDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        using var db = new BlogDbContext(options);
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid() };
        if (scenario != "zero")
        {
            for (var index = 0; index < ids.Length; index++)
                db.Post.Add(new PostEntity
                {
                    Id = ids[index],
                    Title = $"B4 worker {index}",
                    Slug = $"worker-{index}",
                    PostContent = "Worker body",
                    ContentAbstract = "Worker abstract",
                    Author = "Worker author",
                    ContentType = "html",
                    ContentLanguageCode = "en-us",
                    PostStatus = PostStatus.Scheduled,
                    ScheduledPublishTimeUtc = cutoff.UtcDateTime,
                    CreateTimeUtc = cutoff.UtcDateTime.AddDays(-1),
                    LastModifiedUtc = cutoff.UtcDateTime.AddDays(-1)
                });
            await db.SaveChangesAsync(ct);
        }
        var handoffFailure = new OperationCanceledException("B4 synthetic handoff failure");
        var cacheFailure = new InvalidOperationException("B4 synthetic cache failure");
        var notices = new List<Type>();
        var events = new Mock<IEventMediator>();
        events.Setup(m => m.PublishAsync(It.IsAny<PostChangedEvent>(), It.IsAny<EventMediationSettings>(), It.IsAny<CancellationToken>()))
            .Returns((PostChangedEvent item, EventMediationSettings _, CancellationToken token) =>
            {
                Assert.Equal(CancellationToken.None, token);
                notices.Add(item.GetType());
                return scenario == "cache-failure" ? Task.FromException(cacheFailure) : Task.CompletedTask;
            });
        events.Setup(m => m.PublishAsync(It.IsAny<ScheduledPostsPublishedEvent>(), It.IsAny<EventMediationSettings>(), It.IsAny<CancellationToken>()))
            .Returns((ScheduledPostsPublishedEvent item, EventMediationSettings _, CancellationToken token) =>
            {
                Assert.Equal(CancellationToken.None, token);
                notices.Add(item.GetType());
                Assert.Equal(ids, item.Posts.Select(post => post.PostId));
                return scenario == "handoff-failure" ? Task.FromException(handoffFailure) : Task.CompletedTask;
            });
        var feature = new PublishScheduledPostCommandHandler(db, new WorkerTimeProvider(cutoff), events.Object,
            NullLogger<PublishScheduledPostCommandHandler>.Instance);
        var commands = new Mock<ICommandMediator>();
        commands.Setup(m => m.SendAsync(It.IsAny<PublishScheduledPostCommand>(), It.IsAny<CommandMediationSettings>(), It.IsAny<CancellationToken>()))
            .Returns((PublishScheduledPostCommand command, CommandMediationSettings _, CancellationToken token) => feature.HandleAsync(command, token));
        var queries = new Mock<IQueryMediator>();
        var queryCalls = 0;
        queries.Setup(m => m.QueryAsync(It.IsAny<GetNextScheduledPostTimeQuery>(), It.IsAny<QueryMediationSettings>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => Interlocked.Increment(ref queryCalls) == 1 ? DateTime.UtcNow.AddMinutes(-1) : DateTime.UtcNow.AddHours(1));
        var logger = new SchedulerLogger();
        using var services = Services(queries.Object, commands.Object);
        using var worker = new ScheduledPublishService(services, logger, new ScheduledPublishWakeUp(), new BlogConfig());
        await worker.StartAsync(ct);
        try
        {
            if (scenario == "zero")
            {
                await logger.Waiting.Task.WaitAsync(TimeSpan.FromSeconds(5), ct);
                Assert.False(logger.Outcome.Task.IsCompleted);
            }
            else
            {
                var outcome = await logger.Outcome.Task.WaitAsync(TimeSpan.FromSeconds(5), ct);
                if (scenario == "cache-failure")
                {
                    Assert.Null(outcome.Count);
                    Assert.Same(cacheFailure, outcome.Failure);
                }
                else
                {
                    Assert.Equal(2, outcome.Count);
                    Assert.Null(outcome.Failure);
                    await logger.Waiting.Task.WaitAsync(TimeSpan.FromSeconds(5), ct);
                }
            }
            Assert.False(worker.ExecuteTask!.IsCompleted);
        }
        finally
        {
            // Stop cancels the existing error backoff, rather than sleeping through its ten seconds.
            await worker.StopAsync(ct).WaitAsync(TimeSpan.FromSeconds(5), ct);
        }
        Assert.True(worker.ExecuteTask!.IsCompleted);
        commands.Verify(m => m.SendAsync(It.IsAny<PublishScheduledPostCommand>(), It.IsAny<CommandMediationSettings>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(scenario == "zero" ? [] : new[] { typeof(PostChangedEvent), typeof(ScheduledPostsPublishedEvent) }, notices);
        db.ChangeTracker.Clear();
        var posts = await db.Post.AsNoTracking().ToListAsync(ct);
        Assert.Equal(scenario == "zero" ? 0 : 2, posts.Count);
        Assert.All(posts, post =>
        {
            Assert.Equal(PostStatus.Published, post.PostStatus);
            Assert.Equal(cutoff.UtcDateTime.AddSeconds(1), post.PubDateUtc);
            Assert.Equal($"2026/8/1/{post.Slug}", post.RouteLink);
            Assert.Null(post.ScheduledPublishTimeUtc);
            Assert.Equal(cutoff.UtcDateTime.AddDays(-1), post.LastModifiedUtc);
        });
    }

    private sealed class WorkerTimeProvider(DateTimeOffset cutoff) : TimeProvider
    {
        private int _calls;
        public override DateTimeOffset GetUtcNow() => _calls++ == 0 ? cutoff : cutoff.AddSeconds(1);
    }
}
