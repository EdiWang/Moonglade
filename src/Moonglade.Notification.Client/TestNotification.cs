using MediatR;
using Microsoft.Extensions.Logging;

namespace Moonglade.Notification.Client;

public class TestNotification : INotification
{
}

public class TestNotificationHandler : INotificationHandler<TestNotification>
{
    private readonly IBlogNotificationClient _client;
    private readonly ILogger<TestNotificationHandler> _logger;

    public TestNotificationHandler(IBlogNotificationClient client, ILogger<TestNotificationHandler> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task Handle(TestNotification notification, CancellationToken cancellationToken)
    {
        var response = await _client.SendNotification(MailMesageTypes.TestMail, EmptyPayload.Default);
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