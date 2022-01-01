using MediatR;
using Microsoft.Extensions.Logging;
using Moonglade.Utils;

namespace Moonglade.Notification.Client;

public class CommentNotification : INotification
{
    public CommentNotification(string username, string email, string ipAddress, string postTitle, string commentContent, DateTime createTimeUtc)
    {
        Username = username;
        Email = email;
        IPAddress = ipAddress;
        PostTitle = postTitle;
        CommentContent = commentContent;
        CreateTimeUtc = createTimeUtc;
    }

    public string Username { get; set; }
    public string Email { get; set; }
    public string IPAddress { get; set; }
    public string PostTitle { get; set; }
    public string CommentContent { get; set; }
    public DateTime CreateTimeUtc { get; set; }
}

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