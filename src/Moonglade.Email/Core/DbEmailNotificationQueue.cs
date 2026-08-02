using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moonglade.Data;
using Moonglade.Data.Entities;

namespace Moonglade.Email.Core;

public class DbEmailNotificationQueue(
    BlogDbContext db,
    ILogger<DbEmailNotificationQueue> logger) : IEmailOutboxStore
{
    private const int MaxClaimAttempts = 3;

    public async Task<Guid> EnqueueAsync(EmailNotification notification, CancellationToken cancellationToken = default)
    {
        ValidateNotification(notification);

        var entity = new EmailOutboxMessageEntity
        {
            Id = Guid.NewGuid(),
            MessageType = notification.MessageType,
            DistributionList = notification.DistributionList,
            MessageBody = notification.MessageBody,
            Status = EmailOutboxMessageStatus.Pending,
            AttemptCount = 0,
            CreatedTimeUtc = DateTime.UtcNow,
            ConcurrencyToken = Guid.NewGuid()
        };

        await db.EmailOutboxMessage.AddAsync(entity, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Email outbox message {MessageId} enqueued. Type: {MessageType}.",
            entity.Id,
            entity.MessageType);

        return entity.Id;
    }

    public async Task<EmailOutboxMessage> ClaimNextAsync(
        EmailOutboxClaimRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.WorkerId);

        if (request.LeaseDuration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(request.LeaseDuration), "Lease duration must be greater than zero.");
        }

        for (var attempt = 0; attempt < MaxClaimAttempts; attempt++)
        {
            var entity = await QueryClaimableMessages(request.UtcNow)
                .OrderBy(m => m.CreatedTimeUtc)
                .FirstOrDefaultAsync(cancellationToken);

            if (entity == null)
            {
                return null;
            }

            entity.Status = EmailOutboxMessageStatus.Processing;
            entity.LockedBy = request.WorkerId;
            entity.LockedUntilUtc = request.UtcNow.Add(request.LeaseDuration);
            entity.LastAttemptTimeUtc = request.UtcNow;
            entity.AttemptCount++;
            entity.ConcurrencyToken = Guid.NewGuid();

            try
            {
                await db.SaveChangesAsync(cancellationToken);
                return ToOutboxMessage(entity);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogDebug(ex, "Email outbox claim conflict. Retrying claim attempt {Attempt}.", attempt + 1);
                DetachEntries(ex);
            }
        }

        return null;
    }

    public async Task CompleteAsync(Guid messageId, DateTime completedTimeUtc, CancellationToken cancellationToken = default)
    {
        var entity = await GetMessageAsync(messageId, cancellationToken);

        entity.Status = EmailOutboxMessageStatus.Succeeded;
        entity.SentTimeUtc = completedTimeUtc;
        entity.LockedBy = null;
        entity.LockedUntilUtc = null;
        entity.NotBeforeUtc = null;
        entity.LastError = null;
        entity.ConcurrencyToken = Guid.NewGuid();

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Email outbox message {MessageId} completed.", messageId);
    }

    public async Task MarkFailedAsync(
        EmailOutboxFailure failure,
        DateTime nextAttemptUtc,
        CancellationToken cancellationToken = default)
    {
        var entity = await GetMessageAsync(failure.MessageId, cancellationToken);

        entity.Status = EmailOutboxMessageStatus.Failed;
        entity.NotBeforeUtc = nextAttemptUtc;
        entity.LockedBy = null;
        entity.LockedUntilUtc = null;
        entity.LastError = TrimError(failure.ErrorMessage);
        entity.ConcurrencyToken = Guid.NewGuid();

        await db.SaveChangesAsync(cancellationToken);

        logger.LogWarning(
            "Email outbox message {MessageId} failed and will be retried after {NextAttemptUtc}.",
            failure.MessageId,
            nextAttemptUtc);
    }

    public async Task DeadLetterAsync(EmailOutboxFailure failure, CancellationToken cancellationToken = default)
    {
        var entity = await GetMessageAsync(failure.MessageId, cancellationToken);

        entity.Status = EmailOutboxMessageStatus.DeadLettered;
        entity.NotBeforeUtc = null;
        entity.LockedBy = null;
        entity.LockedUntilUtc = null;
        entity.LastError = TrimError(failure.ErrorMessage);
        entity.ConcurrencyToken = Guid.NewGuid();

        await db.SaveChangesAsync(cancellationToken);

        logger.LogWarning("Email outbox message {MessageId} moved to dead letter.", failure.MessageId);
    }

    private IQueryable<EmailOutboxMessageEntity> QueryClaimableMessages(DateTime utcNow)
    {
        return db.EmailOutboxMessage.Where(m =>
            (m.Status == EmailOutboxMessageStatus.Pending || m.Status == EmailOutboxMessageStatus.Failed ||
             (m.Status == EmailOutboxMessageStatus.Processing && m.LockedUntilUtc <= utcNow)) &&
            (m.NotBeforeUtc == null || m.NotBeforeUtc <= utcNow) &&
            (m.LockedUntilUtc == null || m.LockedUntilUtc <= utcNow));
    }

    private static void ValidateNotification(EmailNotification notification)
    {
        var errors = EmailNotificationContract.ValidateNotification(notification);
        if (errors.Length == 0)
        {
            errors = EmailNotificationContract.ValidatePayload(notification.MessageType, notification.MessageBody);
        }

        if (errors.Length > 0)
        {
            throw new ArgumentException(string.Join("; ", errors), nameof(notification));
        }
    }

    private async Task<EmailOutboxMessageEntity> GetMessageAsync(Guid messageId, CancellationToken cancellationToken)
    {
        return await db.EmailOutboxMessage.FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken)
               ?? throw new InvalidOperationException($"Email outbox message {messageId} was not found.");
    }

    private static EmailOutboxMessage ToOutboxMessage(EmailOutboxMessageEntity entity) =>
        new(
            entity.Id,
            entity.MessageType,
            entity.DistributionList,
            entity.MessageBody,
            entity.AttemptCount,
            entity.CreatedTimeUtc);

    private static void DetachEntries(DbUpdateConcurrencyException exception)
    {
        foreach (var entry in exception.Entries)
        {
            entry.State = EntityState.Detached;
        }
    }

    private static string TrimError(string errorMessage)
    {
        const int maxErrorLength = 2000;
        if (string.IsNullOrWhiteSpace(errorMessage))
        {
            return null;
        }

        return errorMessage.Length <= maxErrorLength
            ? errorMessage
            : errorMessage[..maxErrorLength];
    }
}
