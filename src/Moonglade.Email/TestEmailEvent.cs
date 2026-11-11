using LiteBus.Events.Abstractions;
using Moonglade.Auth;
using Moonglade.Configuration;
using Moonglade.Email.Core;
using System.Text.Json.Serialization;

namespace Moonglade.Email;

public record TestEmailEvent : IEvent
{
    [JsonIgnore]
    public BlogSettingsSnapshot Snapshot { get; init; }

    [JsonIgnore]
    public OwnerProfile Owner { get; init; }
}

public class TestNotificationHandler(
    IEmailNotificationQueue queue,
    IBlogConfig blogConfig,
    EmailCapabilityStatus capabilityStatus,
    IOwnerProfileReader ownerProfiles) : IEventHandler<TestEmailEvent>
{
    public async Task HandleAsync(TestEmailEvent notification, CancellationToken ct)
    {
        var settings = notification.Snapshot ?? blogConfig.Capture();
        if (!capabilityStatus.IsAvailable || !settings.NotificationSettings.EnableEmailSending)
        {
            return;
        }

        var owner = notification.Owner ?? await ownerProfiles.ReadProfileAsync(ct);
        if (owner is null) return;

        await queue.EnqueueAsync(new EmailNotification
        {
            MessageType = MessageTypes.TestMail,
            DistributionList = owner.Email,
            MessageBody = "{}"
        }, ct);
    }
}
