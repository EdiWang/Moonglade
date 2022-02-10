using MediatR;
using Microsoft.Extensions.Logging;

namespace Moonglade.Notification.Client;

public record PingbackNotification(
    string TargetPostTitle,
    DateTime PingTimeUtc,
    string Domain,
    string SourceIp,
    string SourceUrl,
    string SourceTitle) : INotification;

internal record PingPayload(
    string TargetPostTitle,
    DateTime PingTimeUtc,
    string Domain,
    string SourceIp,
    string SourceUrl,
    string SourceTitle);

public class PingbackNotificationHandler : INotificationHandler<PingbackNotification>
{
    private readonly IBlogNotificationClient _client;
    private readonly ILogger<PingbackNotificationHandler> _logger;

    public PingbackNotificationHandler(IBlogNotificationClient client, ILogger<PingbackNotificationHandler> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task Handle(PingbackNotification notification, CancellationToken cancellationToken)
    {
        var payload = new PingPayload(
            notification.TargetPostTitle,
            notification.PingTimeUtc,
            notification.Domain,
            notification.SourceIp,
            notification.SourceUrl,
            notification.SourceTitle);

        var response = await _client.SendNotification(MailMesageTypes.BeingPinged, payload);
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