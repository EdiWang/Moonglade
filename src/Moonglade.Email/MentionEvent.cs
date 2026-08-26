using LiteBus.Events.Abstractions;
using Moonglade.Configuration;
using Moonglade.Email.Core;
using System.Text.Json;

namespace Moonglade.Email;

public record MentionEvent(
    string TargetPostTitle,
    string Domain,
    string SourceIp,
    string SourceUrl,
    string SourceTitle) : IEvent;

public class MentionNotificationHandler(
    IEmailNotificationQueue queue,
    IBlogConfig blogConfig,
    EmailCapabilityStatus capabilityStatus) : IEventHandler<MentionEvent>
{
    public async Task HandleAsync(MentionEvent notification, CancellationToken ct)
    {
        if (!capabilityStatus.IsAvailable || !blogConfig.NotificationSettings.EnableEmailSending)
        {
            return;
        }

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
            DistributionList = blogConfig.GeneralSettings.OwnerEmail,
            MessageBody = JsonSerializer.Serialize(payload, MoongladeJsonSerializerOptions.Default)
        }, ct);
    }
}
