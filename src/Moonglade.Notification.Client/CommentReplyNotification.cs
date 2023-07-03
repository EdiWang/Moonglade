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

public class CommentReplyNotificationHandler : INotificationHandler<CommentReplyNotification>
{
    private readonly IBlogNotification _blogNotification;

    public CommentReplyNotificationHandler(IBlogNotification blogNotification)
    {
        _blogNotification = blogNotification;
    }

    public async Task Handle(CommentReplyNotification notification, CancellationToken ct)
    {
        var payload = new CommentReplyPayload(
            notification.CommentContent,
            notification.Title,
            notification.ReplyContentHtml,
            notification.PostLink);

        var dl = new[] { notification.Email };
        await _blogNotification.Enqueue(MailMesageTypes.AdminReplyNotification, dl, payload);
    }
}