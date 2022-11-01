using MediatR;
using Moonglade.Configuration;

namespace Moonglade.Notification.Client;

public record TestNotification : INotification;

public class TestNotificationHandler : INotificationHandler<TestNotification>
{
    private readonly IMoongladeNotification _moongladeNotification;
    private readonly IBlogConfig _blogConfig;

    public TestNotificationHandler(IMoongladeNotification moongladeNotification, IBlogConfig blogConfig)
    {
        _moongladeNotification = moongladeNotification;
        _blogConfig = blogConfig;
    }

    public async Task Handle(TestNotification notification, CancellationToken cancellationToken)
    {
        var dl = new[] { _blogConfig.GeneralSettings.OwnerEmail };
        await _moongladeNotification.EnqueueNotification(MailMesageTypes.TestMail, dl, EmptyPayload.Default);
    }
}