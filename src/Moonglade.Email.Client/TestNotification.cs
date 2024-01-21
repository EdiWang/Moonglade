using MediatR;
using Moonglade.Configuration;

namespace Moonglade.Email.Client;

public record TestNotification : INotification;

public class TestNotificationHandler(IMoongladeEmailClient moongladeEmailClient, IBlogConfig blogConfig) : INotificationHandler<TestNotification>
{
    public async Task Handle(TestNotification notification, CancellationToken ct)
    {
        var dl = new[] { blogConfig.GeneralSettings.OwnerEmail };
        await moongladeEmailClient.SendEmail(MailMesageTypes.TestMail, dl, EmptyPayload.Default);
    }
}