using MediatR;

namespace Moonglade.Email.Client;

public record CommentReplyNotification(
    string Email,
    string CommentContent,
    string Title,
    string ReplyContentHtml,
    string PostLink) : INotification;

internal record CommentReplyPayload(
    string CommentContent,
    string Title,
    string ReplyContentHtml,
    string PostLink);

public class CommentReplyNotificationHandler(IBlogNotification blogNotification) : INotificationHandler<CommentReplyNotification>
{
    public async Task Handle(CommentReplyNotification notification, CancellationToken ct)
    {
        var payload = new CommentReplyPayload(
            notification.CommentContent,
            notification.Title,
            notification.ReplyContentHtml,
            notification.PostLink);

        var dl = new[] { notification.Email };
        await blogNotification.Enqueue(MailMesageTypes.AdminReplyNotification, dl, payload);
    }
}