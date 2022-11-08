using MediatR;
using Moonglade.Configuration;

namespace Moonglade.Notification.Client;

public record PingbackNotification(
    string TargetPostTitle,
    string Domain,
    string SourceIp,
    string SourceUrl,
    string SourceTitle) : INotification;

internal record PingPayload(
    string TargetPostTitle,
    string Domain,
    string SourceIp,
    string SourceUrl,
    string SourceTitle);

public class PingbackNotificationHandler : INotificationHandler<PingbackNotification>
{
    private readonly IMoongladeNotification _moongladeNotification;
    private readonly IBlogConfig _blogConfig;

    public PingbackNotificationHandler(IMoongladeNotification moongladeNotification, IBlogConfig blogConfig)
    {
        _moongladeNotification = moongladeNotification;
        _blogConfig = blogConfig;
    }

    public async Task Handle(PingbackNotification notification, CancellationToken ct)
    {
        var payload = new PingPayload(
            notification.TargetPostTitle,
            notification.Domain,
            notification.SourceIp,
            notification.SourceUrl,
            notification.SourceTitle);

        var dl = new[] { _blogConfig.GeneralSettings.OwnerEmail };
        await _moongladeNotification.EnqueueNotification(MailMesageTypes.BeingPinged, dl, payload);
    }
}