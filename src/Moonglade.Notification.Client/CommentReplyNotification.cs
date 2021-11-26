using MediatR;
using Microsoft.Extensions.Logging;

namespace Moonglade.Notification.Client;

public class CommentReplyNotification : INotification
{
    public CommentReplyNotification(string email, string commentContent, string title, string replyContentHtml, string postLink)
    {
        Email = email;
        CommentContent = commentContent;
        Title = title;
        ReplyContentHtml = replyContentHtml;
        PostLink = postLink;
    }

    public string Email { get; set; }
    public string CommentContent { get; set; }
    public string Title { get; set; }
    public string ReplyContentHtml { get; set; }
    public string PostLink { get; set; }
}

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