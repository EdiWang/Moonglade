using LiteBus.Events.Abstractions;
using Moonglade.Configuration;
using Moonglade.Email.Core;

namespace Moonglade.Email;

public record TestEmailEvent : IEvent;

public class TestNotificationHandler(
    IEmailNotificationQueue queue,
    IBlogConfig blogConfig) : IEventHandler<TestEmailEvent>
{
    public async Task HandleAsync(TestEmailEvent notification, CancellationToken ct)
    {
        if (!blogConfig.NotificationSettings.EnableEmailSending)
        {
            return;
        }

        await queue.EnqueueAsync(new EmailNotification
        {
            MessageType = MessageTypes.TestMail,
            DistributionList = blogConfig.GeneralSettings.OwnerEmail,
            MessageBody = "{}"
        }, ct);
    }
}
