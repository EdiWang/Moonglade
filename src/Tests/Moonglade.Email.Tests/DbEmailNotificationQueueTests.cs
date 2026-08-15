using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Email.Core;
using Moq;
using System.Text.Json;

namespace Moonglade.Email.Tests;

public class DbEmailNotificationQueueTests
{
    private static BlogDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<BlogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new BlogDbContext(options);
    }

    private static DbEmailNotificationQueue CreateQueue(BlogDbContext db) =>
        new(db, Mock.Of<ILogger<DbEmailNotificationQueue>>());

    [Fact]
    public async Task EnqueueAsync_ValidNotification_PersistsPendingMessage()
    {
        using var db = CreateDbContext();
        var queue = CreateQueue(db);
        var notification = CreateNewCommentNotification();

        var id = await queue.EnqueueAsync(notification, TestContext.Current.CancellationToken);

        var entity = await db.EmailOutboxMessage.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(id, entity.Id);
        Assert.Equal(EmailOutboxMessageStatus.Pending, entity.Status);
        Assert.Equal(MessageTypes.NewCommentNotification, entity.MessageType);
        Assert.Equal("admin@example.com", entity.DistributionList);
        Assert.Equal(0, entity.AttemptCount);
        Assert.NotEqual(default, entity.CreatedTimeUtc);
    }

    [Fact]
    public async Task EnqueueAsync_InvalidNotification_ThrowsArgumentException()
    {
        using var db = CreateDbContext();
        var queue = CreateQueue(db);
        var notification = CreateNewCommentNotification() with
        {
            MessageType = "Unknown"
        };

        await Assert.ThrowsAsync<ArgumentException>(() =>
            queue.EnqueueAsync(notification, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ClaimNextAsync_PendingMessage_LeasesMessage()
    {
        using var db = CreateDbContext();
        var queue = CreateQueue(db);
        var id = await queue.EnqueueAsync(CreateNewCommentNotification(), TestContext.Current.CancellationToken);
        var now = new DateTime(2026, 8, 2, 10, 0, 0, DateTimeKind.Utc);

        var message = await queue.ClaimNextAsync(
            new EmailOutboxClaimRequest("worker-1", now, TimeSpan.FromMinutes(5)),
            TestContext.Current.CancellationToken);

        Assert.NotNull(message);
        Assert.Equal(id, message.Id);
        Assert.Equal(1, message.AttemptCount);

        var entity = await db.EmailOutboxMessage.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(EmailOutboxMessageStatus.Processing, entity.Status);
        Assert.Equal("worker-1", entity.LockedBy);
        Assert.Equal(now.AddMinutes(5), entity.LockedUntilUtc);
        Assert.Equal(now, entity.LastAttemptTimeUtc);
    }

    [Fact]
    public async Task ClaimNextAsync_FutureNotBefore_ReturnsNull()
    {
        using var db = CreateDbContext();
        var queue = CreateQueue(db);
        await queue.EnqueueAsync(CreateNewCommentNotification(), TestContext.Current.CancellationToken);
        var entity = await db.EmailOutboxMessage.SingleAsync(TestContext.Current.CancellationToken);
        entity.NotBeforeUtc = new DateTime(2026, 8, 2, 11, 0, 0, DateTimeKind.Utc);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var message = await queue.ClaimNextAsync(
            new EmailOutboxClaimRequest("worker-1", new DateTime(2026, 8, 2, 10, 0, 0, DateTimeKind.Utc), TimeSpan.FromMinutes(5)),
            TestContext.Current.CancellationToken);

        Assert.Null(message);
    }

    [Fact]
    public async Task ClaimNextAsync_ExpiredLease_ReclaimsProcessingMessage()
    {
        using var db = CreateDbContext();
        var queue = CreateQueue(db);
        var id = await queue.EnqueueAsync(CreateNewCommentNotification(), TestContext.Current.CancellationToken);
        var firstClaimTime = new DateTime(2026, 8, 2, 10, 0, 0, DateTimeKind.Utc);
        await queue.ClaimNextAsync(
            new EmailOutboxClaimRequest("worker-1", firstClaimTime, TimeSpan.FromMinutes(5)),
            TestContext.Current.CancellationToken);

        var activeLeaseMessage = await queue.ClaimNextAsync(
            new EmailOutboxClaimRequest("worker-2", firstClaimTime.AddMinutes(4), TimeSpan.FromMinutes(5)),
            TestContext.Current.CancellationToken);
        var reclaimedMessage = await queue.ClaimNextAsync(
            new EmailOutboxClaimRequest("worker-2", firstClaimTime.AddMinutes(6), TimeSpan.FromMinutes(5)),
            TestContext.Current.CancellationToken);

        Assert.Null(activeLeaseMessage);
        Assert.NotNull(reclaimedMessage);
        Assert.Equal(id, reclaimedMessage.Id);
        Assert.Equal(2, reclaimedMessage.AttemptCount);

        var entity = await db.EmailOutboxMessage.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal("worker-2", entity.LockedBy);
    }

    [Fact]
    public async Task CompleteAsync_ProcessingMessage_MarksSucceededAndClearsLease()
    {
        using var db = CreateDbContext();
        var queue = CreateQueue(db);
        var id = await queue.EnqueueAsync(CreateNewCommentNotification(), TestContext.Current.CancellationToken);
        await queue.ClaimNextAsync(
            new EmailOutboxClaimRequest("worker-1", new DateTime(2026, 8, 2, 10, 0, 0, DateTimeKind.Utc), TimeSpan.FromMinutes(5)),
            TestContext.Current.CancellationToken);
        var completedTime = new DateTime(2026, 8, 2, 10, 1, 0, DateTimeKind.Utc);

        await queue.CompleteAsync(id, completedTime, TestContext.Current.CancellationToken);

        var entity = await db.EmailOutboxMessage.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(EmailOutboxMessageStatus.Succeeded, entity.Status);
        Assert.Equal(completedTime, entity.SentTimeUtc);
        Assert.Null(entity.LockedBy);
        Assert.Null(entity.LockedUntilUtc);
        Assert.Null(entity.LastError);
    }

    [Fact]
    public async Task MarkFailedAsync_ProcessingMessage_SchedulesRetry()
    {
        using var db = CreateDbContext();
        var queue = CreateQueue(db);
        var id = await queue.EnqueueAsync(CreateNewCommentNotification(), TestContext.Current.CancellationToken);
        await queue.ClaimNextAsync(
            new EmailOutboxClaimRequest("worker-1", new DateTime(2026, 8, 2, 10, 0, 0, DateTimeKind.Utc), TimeSpan.FromMinutes(5)),
            TestContext.Current.CancellationToken);
        var nextAttempt = new DateTime(2026, 8, 2, 10, 5, 0, DateTimeKind.Utc);

        await queue.MarkFailedAsync(
            new EmailOutboxFailure(id, "temporary failure", new DateTime(2026, 8, 2, 10, 1, 0, DateTimeKind.Utc)),
            nextAttempt,
            TestContext.Current.CancellationToken);

        var entity = await db.EmailOutboxMessage.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(EmailOutboxMessageStatus.Failed, entity.Status);
        Assert.Equal(nextAttempt, entity.NotBeforeUtc);
        Assert.Equal("temporary failure", entity.LastError);
        Assert.Null(entity.LockedBy);
        Assert.Null(entity.LockedUntilUtc);
    }

    [Fact]
    public async Task ClaimNextAsync_FailedMessageAfterNotBefore_ReclaimsMessage()
    {
        using var db = CreateDbContext();
        var queue = CreateQueue(db);
        var id = await queue.EnqueueAsync(CreateNewCommentNotification(), TestContext.Current.CancellationToken);
        await queue.ClaimNextAsync(
            new EmailOutboxClaimRequest("worker-1", new DateTime(2026, 8, 2, 10, 0, 0, DateTimeKind.Utc), TimeSpan.FromMinutes(5)),
            TestContext.Current.CancellationToken);
        await queue.MarkFailedAsync(
            new EmailOutboxFailure(id, "temporary failure", new DateTime(2026, 8, 2, 10, 1, 0, DateTimeKind.Utc)),
            new DateTime(2026, 8, 2, 10, 5, 0, DateTimeKind.Utc),
            TestContext.Current.CancellationToken);

        var beforeRetry = await queue.ClaimNextAsync(
            new EmailOutboxClaimRequest("worker-2", new DateTime(2026, 8, 2, 10, 4, 0, DateTimeKind.Utc), TimeSpan.FromMinutes(5)),
            TestContext.Current.CancellationToken);
        var afterRetry = await queue.ClaimNextAsync(
            new EmailOutboxClaimRequest("worker-2", new DateTime(2026, 8, 2, 10, 5, 1, 0, DateTimeKind.Utc), TimeSpan.FromMinutes(5)),
            TestContext.Current.CancellationToken);

        Assert.Null(beforeRetry);
        Assert.NotNull(afterRetry);
        Assert.Equal(id, afterRetry.Id);
        Assert.Equal(2, afterRetry.AttemptCount);
    }

    [Fact]
    public async Task DeadLetterAsync_ProcessingMessage_MarksDeadLettered()
    {
        using var db = CreateDbContext();
        var queue = CreateQueue(db);
        var id = await queue.EnqueueAsync(CreateNewCommentNotification(), TestContext.Current.CancellationToken);
        await queue.ClaimNextAsync(
            new EmailOutboxClaimRequest("worker-1", new DateTime(2026, 8, 2, 10, 0, 0, DateTimeKind.Utc), TimeSpan.FromMinutes(5)),
            TestContext.Current.CancellationToken);

        await queue.DeadLetterAsync(
            new EmailOutboxFailure(id, "permanent failure", new DateTime(2026, 8, 2, 10, 1, 0, DateTimeKind.Utc)),
            TestContext.Current.CancellationToken);

        var entity = await db.EmailOutboxMessage.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(EmailOutboxMessageStatus.DeadLettered, entity.Status);
        Assert.Equal("permanent failure", entity.LastError);
        Assert.Null(entity.NotBeforeUtc);
        Assert.Null(entity.LockedBy);
        Assert.Null(entity.LockedUntilUtc);
    }

    private static EmailNotification CreateNewCommentNotification()
    {
        var payload = new NewCommentPayload
        {
            Username = "Reader",
            Email = "reader@example.com",
            IpAddress = "127.0.0.1",
            PostTitle = "Hello",
            CommentContent = "Great post"
        };

        return new EmailNotification
        {
            MessageType = MessageTypes.NewCommentNotification,
            DistributionList = "admin@example.com",
            MessageBody = JsonSerializer.Serialize(payload, MoongladeJsonSerializerOptions.Default)
        };
    }
}
