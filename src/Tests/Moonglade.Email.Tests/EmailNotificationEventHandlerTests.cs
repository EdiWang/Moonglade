using Moonglade.Configuration;
using Moonglade.Email.Core;
using System.Text.Json;

namespace Moonglade.Email.Tests;

public class EmailNotificationEventHandlerTests
{
    [Fact]
    public async Task CommentNotificationEventHandler_WhenEmailSendingEnabled_EnqueuesNewCommentNotification()
    {
        var queue = new CapturingEmailNotificationQueue();
        var handler = new CommentNotificationEventHandler(queue, CreateBlogConfig(), CreateAvailableStatus());

        await handler.HandleAsync(
            new CommentEvent("Reader", "reader@example.com", "127.0.0.1", "Hello Post", "**Great** post"),
            TestContext.Current.CancellationToken);

        var notification = Assert.Single(queue.Notifications);
        Assert.Equal(MessageTypes.NewCommentNotification, notification.MessageType);
        Assert.Equal("admin@example.com", notification.DistributionList);

        var payload = Deserialize<NewCommentPayload>(notification.MessageBody);
        Assert.Equal("Reader", payload.Username);
        Assert.Equal("reader@example.com", payload.Email);
        Assert.Equal("127.0.0.1", payload.IpAddress);
        Assert.Equal("Hello Post", payload.PostTitle);
        Assert.Contains("<strong>Great</strong>", payload.CommentContent);
    }

    [Fact]
    public async Task CommentNotificationEventHandler_WhenEmailSendingDisabled_DoesNotEnqueue()
    {
        var queue = new CapturingEmailNotificationQueue();
        var blogConfig = CreateBlogConfig();
        blogConfig.NotificationSettings.EnableEmailSending = false;
        var handler = new CommentNotificationEventHandler(queue, blogConfig, CreateAvailableStatus());

        await handler.HandleAsync(
            new CommentEvent("Reader", "reader@example.com", "127.0.0.1", "Hello Post", "Great post"),
            TestContext.Current.CancellationToken);

        Assert.Empty(queue.Notifications);
    }

    [Fact]
    public async Task CommentNotificationEventHandler_WhenEmailCapabilityUnavailable_DoesNotEnqueue()
    {
        var queue = new CapturingEmailNotificationQueue();
        var handler = new CommentNotificationEventHandler(
            queue,
            CreateBlogConfig(),
            CreateUnavailableStatus());

        await handler.HandleAsync(
            new CommentEvent("Reader", "reader@example.com", "127.0.0.1", "Hello Post", "Great post"),
            TestContext.Current.CancellationToken);

        Assert.Empty(queue.Notifications);
    }

    [Fact]
    public async Task CommentReplyNotificationHandler_WhenEmailSendingEnabled_EnqueuesAdminReplyNotification()
    {
        var queue = new CapturingEmailNotificationQueue();
        var handler = new CommentReplyNotificationHandler(queue, CreateBlogConfig(), CreateAvailableStatus());

        await handler.HandleAsync(
            new CommentReplyEvent(
                "reader@example.com",
                "Original comment",
                "Hello Post",
                "<p>Thanks</p>",
                "https://blog.example/post"),
            TestContext.Current.CancellationToken);

        var notification = Assert.Single(queue.Notifications);
        Assert.Equal(MessageTypes.AdminReplyNotification, notification.MessageType);
        Assert.Equal("reader@example.com", notification.DistributionList);

        var payload = Deserialize<CommentReplyPayload>(notification.MessageBody);
        Assert.Equal("reader@example.com", payload.Email);
        Assert.Equal("Original comment", payload.CommentContent);
        Assert.Equal("Hello Post", payload.Title);
        Assert.Equal("<p>Thanks</p>", payload.ReplyContentHtml);
        Assert.Equal("https://blog.example/post", payload.PostLink);
    }

    [Fact]
    public async Task CommentReplyNotificationHandler_WhenEmailCapabilityUnavailable_DoesNotEnqueue()
    {
        var queue = new CapturingEmailNotificationQueue();
        var handler = new CommentReplyNotificationHandler(
            queue,
            CreateBlogConfig(),
            CreateUnavailableStatus());

        await handler.HandleAsync(
            new CommentReplyEvent(
                "reader@example.com",
                "Original comment",
                "Hello Post",
                "<p>Thanks</p>",
                "https://blog.example/post"),
            TestContext.Current.CancellationToken);

        Assert.Empty(queue.Notifications);
    }

    [Fact]
    public async Task MentionNotificationHandler_WhenEmailSendingEnabled_EnqueuesBeingPingedNotification()
    {
        var queue = new CapturingEmailNotificationQueue();
        var handler = new MentionNotificationHandler(queue, CreateBlogConfig(), CreateAvailableStatus());

        await handler.HandleAsync(
            new MentionEvent(
                "Target Post",
                "source.example",
                "127.0.0.1",
                "https://source.example/post",
                "Source Post"),
            TestContext.Current.CancellationToken);

        var notification = Assert.Single(queue.Notifications);
        Assert.Equal(MessageTypes.BeingPinged, notification.MessageType);
        Assert.Equal("admin@example.com", notification.DistributionList);

        var payload = Deserialize<PingPayload>(notification.MessageBody);
        Assert.Equal("Target Post", payload.TargetPostTitle);
        Assert.Equal("source.example", payload.Domain);
        Assert.Equal("127.0.0.1", payload.SourceIp);
        Assert.Equal("https://source.example/post", payload.SourceUrl);
        Assert.Equal("Source Post", payload.SourceTitle);
    }

    [Fact]
    public async Task MentionNotificationHandler_WhenEmailCapabilityUnavailable_DoesNotEnqueue()
    {
        var queue = new CapturingEmailNotificationQueue();
        var handler = new MentionNotificationHandler(
            queue,
            CreateBlogConfig(),
            CreateUnavailableStatus());

        await handler.HandleAsync(
            new MentionEvent(
                "Target Post",
                "source.example",
                "127.0.0.1",
                "https://source.example/post",
                "Source Post"),
            TestContext.Current.CancellationToken);

        Assert.Empty(queue.Notifications);
    }

    [Fact]
    public async Task TestNotificationHandler_WhenEmailSendingEnabled_EnqueuesTestMailNotification()
    {
        var queue = new CapturingEmailNotificationQueue();
        var handler = new TestNotificationHandler(queue, CreateBlogConfig(), CreateAvailableStatus());

        await handler.HandleAsync(new TestEmailEvent(), TestContext.Current.CancellationToken);

        var notification = Assert.Single(queue.Notifications);
        Assert.Equal(MessageTypes.TestMail, notification.MessageType);
        Assert.Equal("admin@example.com", notification.DistributionList);
        Assert.Equal("{}", notification.MessageBody);
    }

    [Fact]
    public async Task TestNotificationHandler_WhenEmailCapabilityUnavailable_DoesNotEnqueue()
    {
        var queue = new CapturingEmailNotificationQueue();
        var handler = new TestNotificationHandler(
            queue,
            CreateBlogConfig(),
            CreateUnavailableStatus());

        await handler.HandleAsync(new TestEmailEvent(), TestContext.Current.CancellationToken);

        Assert.Empty(queue.Notifications);
    }

    private static BlogConfig CreateBlogConfig() => new()
    {
        GeneralSettings = new GeneralSettings
        {
            OwnerEmail = "admin@example.com"
        },
        NotificationSettings = new NotificationSettings
        {
            EnableEmailSending = true
        }
    };

    private static EmailCapabilityStatus CreateAvailableStatus() => CreateStatus(
        new EmailServiceOptions
        {
            Provider = "smtp",
            SmtpServer = "smtp.example.com",
            SmtpUserName = "sender@example.com",
            SmtpPassword = "password",
            SmtpPort = 587
        },
        new EmailOutboxWorkerOptions());

    private static EmailCapabilityStatus CreateUnavailableStatus() => CreateStatus(
        new EmailServiceOptions
        {
            Provider = "AzureCommunication"
        },
        new EmailOutboxWorkerOptions());

    private static EmailCapabilityStatus CreateStatus(
        EmailServiceOptions serviceOptions,
        EmailOutboxWorkerOptions workerOptions)
    {
        var evaluator = new EmailCapabilityStatusEvaluator(
            new EmailServiceOptionsValidator(),
            new EmailOutboxWorkerOptionsValidator());

        return evaluator.Evaluate(serviceOptions, workerOptions);
    }

    private static TPayload Deserialize<TPayload>(string json)
    {
        var payload = JsonSerializer.Deserialize<TPayload>(json, MoongladeJsonSerializerOptions.Default);
        Assert.NotNull(payload);
        return payload;
    }

    private sealed class CapturingEmailNotificationQueue : IEmailNotificationQueue
    {
        public List<EmailNotification> Notifications { get; } = [];

        public Task<Guid> EnqueueAsync(EmailNotification notification, CancellationToken cancellationToken = default)
        {
            Notifications.Add(notification);
            return Task.FromResult(Guid.NewGuid());
        }
    }
}
