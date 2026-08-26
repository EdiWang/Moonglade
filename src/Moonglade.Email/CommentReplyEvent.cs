using LiteBus.Events.Abstractions;
using Moonglade.Configuration;
using Moonglade.Email.Core;
using System.Text.Json;

namespace Moonglade.Email;

public record CommentReplyEvent(
    string Email,
    string CommentContent,
    string Title,
    string ReplyContentHtml,
    string PostLink) : IEvent;

public class CommentReplyNotificationHandler(
    IEmailNotificationQueue queue,
    IBlogConfig blogConfig,
    EmailCapabilityStatus capabilityStatus) : IEventHandler<CommentReplyEvent>
{
    public async Task HandleAsync(CommentReplyEvent notification, CancellationToken ct)
    {
        if (!capabilityStatus.IsAvailable || !blogConfig.NotificationSettings.EnableEmailSending)
        {
            return;
        }

        var payload = new CommentReplyPayload
        {
            Email = notification.Email,
            CommentContent = notification.CommentContent,
            Title = notification.Title,
            ReplyContentHtml = notification.ReplyContentHtml,
            PostLink = notification.PostLink
        };

        await queue.EnqueueAsync(new EmailNotification
        {
            MessageType = MessageTypes.AdminReplyNotification,
            DistributionList = notification.Email,
            MessageBody = JsonSerializer.Serialize(payload, MoongladeJsonSerializerOptions.Default)
        }, ct);
    }
}
