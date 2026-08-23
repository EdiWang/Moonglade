using LiteBus.Events.Abstractions;
using Moonglade.Configuration;
using Moonglade.Email.Core;
using Moonglade.Utils;
using System.Text.Json;

namespace Moonglade.Email;

public record CommentEvent(
    string Username,
    string Email,
    string IPAddress,
    string PostTitle,
    string CommentContent) : IEvent;

public class CommentNotificationEventHandler(
    IEmailNotificationQueue queue,
    IBlogConfig blogConfig,
    EmailCapabilityStatus capabilityStatus) : IEventHandler<CommentEvent>
{
    public async Task HandleAsync(CommentEvent notification, CancellationToken ct)
    {
        if (!capabilityStatus.IsAvailable || !blogConfig.NotificationSettings.EnableEmailSending)
        {
            return;
        }

        var payload = new NewCommentPayload
        {
            Username = notification.Username,
            Email = notification.Email,
            IpAddress = notification.IPAddress,
            PostTitle = notification.PostTitle,
            CommentContent = ContentProcessor.MarkdownToCommentHtml(notification.CommentContent)
        };

        await queue.EnqueueAsync(new EmailNotification
        {
            MessageType = MessageTypes.NewCommentNotification,
            DistributionList = blogConfig.GeneralSettings.OwnerEmail,
            MessageBody = JsonSerializer.Serialize(payload, MoongladeJsonSerializerOptions.Default)
        }, ct);
    }
}
