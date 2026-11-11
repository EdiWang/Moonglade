using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moonglade.Auth;
using Moonglade.Data;
using Moonglade.Email.Core;
using Moq;
using System.Text.Json;

namespace Moonglade.Email.Tests.Notifications;

public partial class EmailNotificationEventHandlerTests
{
    [Theory]
    [InlineData("comment")]
    [InlineData("mention")]
    [InlineData("test")]
    public async Task OwnerNotification_CapturesRecipientOnceBeforeQueueAwait_AndNextOperationRefreshes(string kind)
    {
        var ct = TestContext.Current.CancellationToken;
        var config = CreateBlogConfig();
        var profiles = new ProfileReader();
        var old = profiles.Owner!;
        var queue = new PausedQueue();
        Task Send() => kind switch
        {
            "comment" => new CommentNotificationEventHandler(queue, config, CreateAvailableStatus(), profiles)
                .HandleAsync(new CommentEvent("Visitor", "visitor@example.test", "192.0.2.1", "Retained post", "**Body**"), ct),
            "mention" => new MentionNotificationHandler(queue, config, CreateAvailableStatus(), profiles)
                .HandleAsync(new MentionEvent("Retained post", "source.example.test", "192.0.2.1", "https://source.example.test/post", "Source"), ct),
            _ => new TestNotificationHandler(queue, config, CreateAvailableStatus(), profiles).HandleAsync(new TestEmailEvent(), ct)
        };

        var pending = Send();
        await queue.Started.Task.WaitAsync(TimeSpan.FromSeconds(10), ct);
        profiles.Owner = old with { Email = "new@example.test" };
        queue.Resume.SetResult();
        await pending;
        Assert.Equal(old.Email, Assert.Single(queue.Notifications).DistributionList);
        Assert.Equal(1, profiles.Reads);

        await Send();

        Assert.Equal(["admin@example.com", "new@example.test"], queue.Notifications.Select(message => message.DistributionList));
        Assert.Equal(2, profiles.Reads);
        Assert.All(queue.Notifications, notification => Assert.Equal(kind switch
        {
            "comment" => MessageTypes.NewCommentNotification,
            "mention" => MessageTypes.BeingPinged,
            _ => MessageTypes.TestMail
        }, notification.MessageType));
        if (kind == "comment")
            Assert.Equal("Retained post", Deserialize<NewCommentPayload>(queue.Notifications[0].MessageBody).PostTitle);
        if (kind == "mention")
            Assert.Equal("https://source.example.test/post", Deserialize<PingPayload>(queue.Notifications[0].MessageBody).SourceUrl);
    }

    [Fact]
    public async Task MissingOwner_SkipsOnlyOwnerDirectedMail_AndCommenterReplyRetainsRecipientAndPayload()
    {
        var ct = TestContext.Current.CancellationToken;
        var profiles = new ProfileReader { Owner = null };
        var queue = new CapturingEmailNotificationQueue();
        var config = CreateBlogConfig();
        var status = CreateAvailableStatus();

        await new CommentNotificationEventHandler(queue, config, status, profiles)
            .HandleAsync(new CommentEvent("Visitor", "visitor@example.test", "192.0.2.1", "Post", "Body"), ct);
        await new MentionNotificationHandler(queue, config, status, profiles)
            .HandleAsync(new MentionEvent("Post", "source.example.test", "192.0.2.1", "https://source.example.test/post", "Source"), ct);
        await new TestNotificationHandler(queue, config, status, profiles).HandleAsync(new TestEmailEvent(), ct);
        Assert.Empty(queue.Notifications);
        Assert.Equal(3, profiles.Reads);

        await new CommentReplyNotificationHandler(queue, config, status).HandleAsync(
            new CommentReplyEvent("visitor@example.test", "Original body", "Post", "<p>Reply</p>", "https://blog.example.test/post"), ct);

        var reply = Assert.Single(queue.Notifications);
        Assert.Equal(MessageTypes.AdminReplyNotification, reply.MessageType);
        Assert.Equal("visitor@example.test", reply.DistributionList);
        var payload = Deserialize<CommentReplyPayload>(reply.MessageBody);
        Assert.Equal("Original body", payload.CommentContent);
        Assert.Equal("<p>Reply</p>", payload.ReplyContentHtml);
        Assert.Equal(3, profiles.Reads);
    }

    [Fact]
    public async Task TestNotification_SuppliedOwnerIsRetainedWithoutAmbientRead_AndIsNotSerialized()
    {
        var ct = TestContext.Current.CancellationToken;
        var profiles = new ProfileReader { Owner = null };
        var queue = new CapturingEmailNotificationQueue();
        var notification = new TestEmailEvent { Owner = new OwnerProfile(Guid.NewGuid(), "captured@example.test") };

        await new TestNotificationHandler(queue, CreateBlogConfig(), CreateAvailableStatus(), profiles).HandleAsync(notification, ct);

        Assert.Equal("captured@example.test", Assert.Single(queue.Notifications).DistributionList);
        Assert.Equal(0, profiles.Reads);
        Assert.Equal("{}", JsonSerializer.Serialize(notification));
    }

    [Fact]
    public async Task QueuedNotification_ProfileChanges_DoNotRewritePersistedRecipientOrPayload()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = new BlogDbContext(new DbContextOptionsBuilder<BlogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var queue = new DbEmailNotificationQueue(db, Mock.Of<ILogger<DbEmailNotificationQueue>>());
        var profiles = new ProfileReader();
        var handler = new CommentNotificationEventHandler(queue, CreateBlogConfig(), CreateAvailableStatus(), profiles);
        await handler.HandleAsync(new CommentEvent("First visitor", "first@example.test", "192.0.2.1", "First post", "First body"), ct);
        var first = await db.EmailOutboxMessage.AsNoTracking().SingleAsync(ct);

        profiles.Owner = profiles.Owner! with { Email = "changed@example.test" };
        await handler.HandleAsync(new CommentEvent("Second visitor", "second@example.test", "192.0.2.2", "Second post", "Second body"), ct);
        db.ChangeTracker.Clear();

        var retained = await db.EmailOutboxMessage.AsNoTracking().SingleAsync(message => message.Id == first.Id, ct);
        Assert.Equal("admin@example.com", retained.DistributionList);
        Assert.Equal(first.MessageBody, retained.MessageBody);
        Assert.Equal("<p>First body</p>\n", Deserialize<NewCommentPayload>(retained.MessageBody).CommentContent);
        var next = await db.EmailOutboxMessage.AsNoTracking().SingleAsync(message => message.Id != first.Id, ct);
        Assert.Equal("changed@example.test", next.DistributionList);
        Assert.Equal("Second post", Deserialize<NewCommentPayload>(next.MessageBody).PostTitle);
        Assert.Equal(2, profiles.Reads);
    }

    private sealed class PausedQueue : IEmailNotificationQueue
    {
        public List<EmailNotification> Notifications { get; } = [];
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Resume { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public async Task<Guid> EnqueueAsync(EmailNotification notification, CancellationToken cancellationToken = default)
        {
            Notifications.Add(notification);
            Started.TrySetResult();
            await Resume.Task.WaitAsync(cancellationToken);
            return Guid.NewGuid();
        }
    }
}
