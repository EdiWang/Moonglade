using MediatR;
using Moonglade.Configuration;
using Moonglade.Utils;

namespace Moonglade.Email.Client;

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

public class CommentNotificationHandler(IBlogNotification blogNotification, IBlogConfig blogConfig) : INotificationHandler<CommentNotification>
{
    public async Task Handle(CommentNotification notification, CancellationToken ct)
    {
        var payload = new CommentPayload(
            notification.Username,
            notification.Email,
            notification.IPAddress,
            notification.PostTitle,
            ContentProcessor.MarkdownToContent(notification.CommentContent, ContentProcessor.MarkdownConvertType.Html)
        );

        var dl = new[] { blogConfig.GeneralSettings.OwnerEmail };
        await blogNotification.Enqueue(MailMesageTypes.NewCommentNotification, dl, payload);
    }
}