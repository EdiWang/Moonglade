using LiteBus.Events.Abstractions;
using MediatR;
using Moonglade.Configuration;
using Moonglade.Utils;

namespace Moonglade.Email.Client;

public record CommentEvent(
    string Username,
    string Email,
    string IPAddress,
    string PostTitle,
    string CommentContent) : IEvent;

public class CommentNotificationEventHandler(IMoongladeEmailClient moongladeEmailClient, IBlogConfig blogConfig) : IEventHandler<CommentEvent>
{
    public async Task HandleAsync(CommentEvent notification, CancellationToken ct)
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