using MediatR;
using Microsoft.Extensions.Logging;

namespace Moonglade.Notification.Client;

public record CommentReplyNotification(
    string Email,
    string CommentContent,
    string Title,
    string ReplyContentHtml,
    string PostLink) : INotification;

internal record CommentReplyPayload(
    string Email,
    string CommentContent,
    string Title,
    string ReplyContentHtml,
    string PostLink);

public class CommentReplyNotificationHandler : INotificationHandler<CommentReplyNotification>
{
    private readonly IBlogNotificationClient _client;
    private readonly ILogger<CommentReplyNotificationHandler> _logger;

    public CommentReplyNotificationHandler(IBlogNotificationClient client, ILogger<CommentReplyNotificationHandler> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task Handle(CommentReplyNotification notification, CancellationToken cancellationToken)
    {
        var payload = new CommentReplyPayload(
            notification.Email,
            notification.CommentContent,
            notification.Title,
            notification.ReplyContentHtml,
            notification.PostLink);

        var response = await _client.SendNotification(MailMesageTypes.AdminReplyNotification, payload);
        var respBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation($"Test email is sent, server response: '{respBody}'");
        }
        else
        {
            throw new($"Test email sending failed, response code: '{response.StatusCode}', response body: '{respBody}'");
        }
    }
}