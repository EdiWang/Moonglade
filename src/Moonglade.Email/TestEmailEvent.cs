using System.Text.Json.Serialization;
using LiteBus.Events.Abstractions;
using Moonglade.Configuration;
using Moonglade.Email.Core;

namespace Moonglade.Email;

public record TestEmailEvent : IEvent
{
    [JsonIgnore]
    public BlogSettingsSnapshot Snapshot { get; init; }
}

public class TestNotificationHandler(
    IEmailNotificationQueue queue,
    IBlogConfig blogConfig,
    EmailCapabilityStatus capabilityStatus) : IEventHandler<TestEmailEvent>
{
    public async Task HandleAsync(TestEmailEvent notification, CancellationToken ct)
    {
        var settings = notification.Snapshot ?? blogConfig.Capture();
        if (!capabilityStatus.IsAvailable || !settings.NotificationSettings.EnableEmailSending)
        {
            return;
        }

        await queue.EnqueueAsync(new EmailNotification
        {
            MessageType = MessageTypes.TestMail,
            DistributionList = settings.GeneralSettings.OwnerEmail,
            MessageBody = "{}"
        }, ct);
    }
}
