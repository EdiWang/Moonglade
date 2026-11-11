using LiteBus.Events.Abstractions;
using Moonglade.Auth;
using Moonglade.Configuration;
using Moonglade.Email.Core;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Moonglade.Email;

public record MentionEvent(
    string TargetPostTitle,
    string Domain,
    string SourceIp,
    string SourceUrl,
    string SourceTitle) : IEvent
{
    [JsonIgnore]
    public BlogSettingsSnapshot Snapshot { get; init; }
}

public class MentionNotificationHandler(
    IEmailNotificationQueue queue,
    IBlogConfig blogConfig,
    EmailCapabilityStatus capabilityStatus,
    IOwnerProfileReader ownerProfiles) : IEventHandler<MentionEvent>
{
    public async Task HandleAsync(MentionEvent notification, CancellationToken ct)
    {
        var settings = notification.Snapshot ?? blogConfig.Capture();
        if (!capabilityStatus.IsAvailable || !settings.NotificationSettings.EnableEmailSending)
        {
            return;
        }

        var owner = await ownerProfiles.ReadProfileAsync(ct);
        if (owner is null) return;

        var payload = new PingPayload
        {
            TargetPostTitle = notification.TargetPostTitle,
            Domain = notification.Domain,
            SourceIp = notification.SourceIp,
            SourceUrl = notification.SourceUrl,
            SourceTitle = notification.SourceTitle
        };

        await queue.EnqueueAsync(new EmailNotification
        {
            MessageType = MessageTypes.BeingPinged,
            DistributionList = owner.Email,
            MessageBody = JsonSerializer.Serialize(payload, MoongladeJsonSerializerOptions.Default)
        }, ct);
    }
}
