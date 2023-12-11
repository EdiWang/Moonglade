using MediatR;

using Moonglade.Configuration;

namespace Moonglade.Notification.Client;

public record ContactNotification(
    string Name,
    string Subject,
    string Email,
    string Body) : INotification;

internal record ContactPayload(
    string Name,
    string Subject,
    string Email,
    string Body);

public class ContactNotificationHandler : INotificationHandler<ContactNotification>
{
    private readonly IMoongladeNotification _moongladeNotification;
    private readonly IBlogConfig _blogConfig;

    public ContactNotificationHandler(IMoongladeNotification moongladeNotification, IBlogConfig blogConfig)
    {
        _moongladeNotification = moongladeNotification;
        _blogConfig = blogConfig;
    }

    public async Task Handle(ContactNotification notification, CancellationToken ct)
    {
        var dl = new[] { _blogConfig.GeneralSettings.OwnerEmail };
        var payload = new ContactNotification(
            notification.Name,
            notification.Subject,
            notification.Email,
            notification.Body
        );
        await _moongladeNotification.EnqueueNotification(MailMesageTypes.ContactNotification, dl, payload);
    }
}

