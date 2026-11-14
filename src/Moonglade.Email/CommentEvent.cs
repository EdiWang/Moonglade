using LiteBus.Events.Abstractions;
using Moonglade.Auth;
using Moonglade.Configuration;
using Moonglade.Email.Core;
using Moonglade.Utils;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Moonglade.Email;

public record CommentEvent(
    string Username,
    string Email,
    string IPAddress,
    string PostTitle,
    string CommentContent) : IEvent
{
    [JsonIgnore]
    public BlogSettingsSnapshot Snapshot { get; init; }
}

public class CommentNotificationEventHandler(
    IEmailNotificationQueue queue,
    IBlogConfig blogConfig,
    EmailCapabilityStatus capabilityStatus,
    IOwnerProfileReader ownerProfiles) : IEventHandler<CommentEvent>
{
    public async Task HandleAsync(CommentEvent notification, CancellationToken ct)
    {
        var settings = notification.Snapshot ?? blogConfig.Capture();
        if (!capabilityStatus.IsAvailable || !settings.NotificationSettings.EnableEmailSending)
        {
            return;
        }

        var owner = await ownerProfiles.ReadProfileAsync(ct);
        if (owner is null) return;

        var payload = new NewCommentPayload
        {
            Username = notification.Username,
            Email = notification.Email,
            IpAddress = notification.IPAddress,
            PostTitle = notification.PostTitle,
            CommentContent = ContentProcessor.MarkdownToCommentHtml(notification.CommentContent,
                settings.CommentSettings.EnableMarkdownLinks)
        };

        await queue.EnqueueAsync(new EmailNotification
        {
            MessageType = MessageTypes.NewCommentNotification,
            DistributionList = owner.Email,
            MessageBody = JsonSerializer.Serialize(payload, MoongladeJsonSerializerOptions.Default)
        }, ct);
    }
}
