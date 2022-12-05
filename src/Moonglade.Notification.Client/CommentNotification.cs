using MediatR;
using Moonglade.Configuration;
using Moonglade.Utils;

namespace Moonglade.Notification.Client;

public record CommentNotification(
    string Username,
    string Email,
    string IPAddress,
    string PostTitle,
    string CommentContent) : INotification;

internal record CommentPayload(
    string Username,
    string Email,
    string IpAddress,
    string PostTitle,
    string CommentContent);

public class CommentNotificationHandler : INotificationHandler<CommentNotification>
{
    private readonly IMoongladeNotification _moongladeNotification;
    private readonly IBlogConfig _blogConfig;

    public CommentNotificationHandler(IMoongladeNotification moongladeNotification, IBlogConfig blogConfig)
    {
        _moongladeNotification = moongladeNotification;
        _blogConfig = blogConfig;
    }

    public async Task Handle(CommentNotification notification, CancellationToken ct)
    {
        var payload = new CommentPayload(
            notification.Username,
            notification.Email,
            notification.IPAddress,
            notification.PostTitle,
            ContentProcessor.MarkdownToContent(notification.CommentContent, ContentProcessor.MarkdownConvertType.Html)
        );

        var dl = new[] { _blogConfig.GeneralSettings.OwnerEmail };
        await _moongladeNotification.EnqueueNotification(MailMesageTypes.NewCommentNotification, dl, payload);
    }
}