using MediatR;
using Moonglade.Configuration;

namespace Moonglade.Email.Client;

public record PingbackNotification(
    string TargetPostTitle,
    string Domain,
    string SourceIp,
    string SourceUrl,
    string SourceTitle) : INotification;

public class PingbackNotificationHandler : INotificationHandler<PingbackNotification>
{
    private readonly IBlogNotification _blogNotification;
    private readonly IBlogConfig _blogConfig;

    public PingbackNotificationHandler(IBlogNotification blogNotification, IBlogConfig blogConfig)
    {
        _blogNotification = blogNotification;
        _blogConfig = blogConfig;
    }

    public async Task Handle(PingbackNotification notification, CancellationToken ct)
    {
        var dl = new[] { _blogConfig.GeneralSettings.OwnerEmail };
        await _blogNotification.Enqueue(MailMesageTypes.BeingPinged, dl, notification);
    }
}