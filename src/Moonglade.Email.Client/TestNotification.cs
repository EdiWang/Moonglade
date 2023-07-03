using MediatR;
using Moonglade.Configuration;

namespace Moonglade.Email.Client;

public record TestNotification : INotification;

public class TestNotificationHandler : INotificationHandler<TestNotification>
{
    private readonly IBlogNotification _blogNotification;
    private readonly IBlogConfig _blogConfig;

    public TestNotificationHandler(IBlogNotification blogNotification, IBlogConfig blogConfig)
    {
        _blogNotification = blogNotification;
        _blogConfig = blogConfig;
    }

    public async Task Handle(TestNotification notification, CancellationToken ct)
    {
        var dl = new[] { _blogConfig.GeneralSettings.OwnerEmail };
        await _blogNotification.Enqueue(MailMesageTypes.TestMail, dl, EmptyPayload.Default);
    }
}