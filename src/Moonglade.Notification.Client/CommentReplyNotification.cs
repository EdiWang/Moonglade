using MediatR;

namespace Moonglade.Notification.Client;

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
    private readonly IMoongladeNotification _moongladeNotification;

    public CommentReplyNotificationHandler(IMoongladeNotification moongladeNotification)
    {
        _moongladeNotification = moongladeNotification;
    }

    public async Task Handle(CommentReplyNotification notification, CancellationToken ct)
    {
        var payload = new CommentReplyPayload(
            notification.CommentContent,
            notification.Title,
            notification.ReplyContentHtml,
            notification.PostLink);

        var dl = new[] { notification.Email };
        await _moongladeNotification.EnqueueNotification(MailMesageTypes.AdminReplyNotification, dl, payload);
    }
}