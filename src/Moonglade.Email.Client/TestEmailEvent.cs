using LiteBus.Events.Abstractions;
using MediatR;
using Moonglade.Configuration;

namespace Moonglade.Email.Client;

public record TestEmailEvent : IEvent;

public class TestNotificationHandler(IMoongladeEmailClient moongladeEmailClient, IBlogConfig blogConfig) : IEventHandler<TestEmailEvent>
{
    public async Task HandleAsync(TestEmailEvent notification, CancellationToken ct)
    {
        var dl = new[] { blogConfig.GeneralSettings.OwnerEmail };
        await moongladeEmailClient.SendEmail(MailMesageTypes.TestMail, dl, EmptyPayload.Default);
    }
}