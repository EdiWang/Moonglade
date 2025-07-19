using LiteBus.Events.Abstractions;

namespace Moonglade.Email.Client;

public record CommentReplyEvent(
    string Email,
    string CommentContent,
    string Title,
    string ReplyContentHtml,
    string PostLink) : IEvent;

public class CommentReplyNotificationHandler(IMoongladeEmailClient moongladeEmailClient) : IEventHandler<CommentReplyEvent>
{
    public async Task HandleAsync(CommentReplyEvent notification, CancellationToken ct)
    {
        var dl = new[] { notification.Email };
        await moongladeEmailClient.SendEmail(MailMesageTypes.AdminReplyNotification, dl, notification);
    }
}