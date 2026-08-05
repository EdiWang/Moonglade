namespace Moonglade.Email.Core;

public interface IEmailNotificationQueue
{
    Task<Guid> EnqueueAsync(EmailNotification notification, CancellationToken cancellationToken = default);
}
