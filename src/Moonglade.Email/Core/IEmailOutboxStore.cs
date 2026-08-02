namespace Moonglade.Email.Core;

public interface IEmailOutboxStore : IEmailNotificationQueue
{
    Task<EmailOutboxMessage> ClaimNextAsync(EmailOutboxClaimRequest request, CancellationToken cancellationToken = default);

    Task CompleteAsync(Guid messageId, DateTime completedTimeUtc, CancellationToken cancellationToken = default);

    Task MarkFailedAsync(EmailOutboxFailure failure, DateTime nextAttemptUtc, CancellationToken cancellationToken = default);

    Task DeadLetterAsync(EmailOutboxFailure failure, CancellationToken cancellationToken = default);
}
