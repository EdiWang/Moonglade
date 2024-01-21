using MediatR;

namespace Moonglade.Email.Client;

public record CommentReplyNotification(
    string Email,
    string CommentContent,
    string Title,
    string ReplyContentHtml,
    string PostLink) : INotification;

public class CommentReplyNotificationHandler(IMoongladeEmailClient moongladeEmailClient) : INotificationHandler<CommentReplyNotification>
{
    public async Task Handle(CommentReplyNotification notification, CancellationToken ct)
    {
        var dl = new[] { notification.Email };
        await moongladeEmailClient.SendEmail(MailMesageTypes.AdminReplyNotification, dl, notification);
    }
}