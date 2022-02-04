using MediatR;
using Microsoft.Extensions.Logging;
using Moonglade.Utils;

namespace Moonglade.Notification.Client;

public record CommentNotification(
    string Username,
    string Email,
    string IPAddress,
    string PostTitle,
    string CommentContent,
    DateTime CreateTimeUtc) : INotification;

internal record CommentPayload(
    string Username,
    string Email,
    string IpAddress,
    string PostTitle,
    string CommentContent,
    DateTime CreateTimeUtc);

public class CommentNotificationHandler : INotificationHandler<CommentNotification>
{
    private readonly IBlogNotificationClient _client;
    private readonly ILogger<CommentNotificationHandler> _logger;

    public CommentNotificationHandler(IBlogNotificationClient client, ILogger<CommentNotificationHandler> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task Handle(CommentNotification notification, CancellationToken cancellationToken)
    {
        var payload = new CommentPayload(
            notification.Username,
            notification.Email,
            notification.IPAddress,
            notification.PostTitle,
            ContentProcessor.MarkdownToContent(notification.CommentContent, ContentProcessor.MarkdownConvertType.Html),
            notification.CreateTimeUtc
        );

        var response = await _client.SendNotification(MailMesageTypes.NewCommentNotification, payload);

        if (response is null)
        {
            return;
        }

        var respBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation($"Email is sent, server response: '{respBody}'");
        }
        else
        {
            throw new($"Email sending failed, response code: '{response.StatusCode}', response body: '{respBody}'");
        }
    }
}