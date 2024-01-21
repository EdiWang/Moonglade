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

public class CommentNotificationHandler(IMoongladeEmailClient moongladeEmailClient, IBlogConfig blogConfig) : INotificationHandler<CommentNotification>
{
    public async Task Handle(CommentNotification notification, CancellationToken ct)
    {
        notification = notification with
        {
            CommentContent = ContentProcessor.MarkdownToContent(notification.CommentContent,
                ContentProcessor.MarkdownConvertType.Html)
        };

        var dl = new[] { blogConfig.GeneralSettings.OwnerEmail };
        await moongladeEmailClient.SendEmail(MailMesageTypes.NewCommentNotification, dl, notification);
    }
}