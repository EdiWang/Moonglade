using MediatR;
using Microsoft.Extensions.Logging;

namespace Moonglade.Notification.Client
{
    public class PingbackNotification : INotification
    {
        public PingbackNotification(string targetPostTitle, DateTime pingTimeUtc, string domain, string sourceIp, string sourceUrl, string sourceTitle)
        {
            TargetPostTitle = targetPostTitle;
            PingTimeUtc = pingTimeUtc;
            Domain = domain;
            SourceIp = sourceIp;
            SourceUrl = sourceUrl;
            SourceTitle = sourceTitle;
        }

        public string TargetPostTitle { get; set; }
        public DateTime PingTimeUtc { get; set; }
        public string Domain { get; set; }
        public string SourceIp { get; set; }
        public string SourceUrl { get; set; }
        public string SourceTitle { get; set; }
    }

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
}
