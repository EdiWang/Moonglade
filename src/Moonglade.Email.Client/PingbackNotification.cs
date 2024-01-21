using MediatR;
using Moonglade.Configuration;

namespace Moonglade.Email.Client;

public record PingbackNotification(
    string TargetPostTitle,
    string Domain,
    string SourceIp,
    string SourceUrl,
    string SourceTitle) : INotification;

public class PingbackNotificationHandler(IMoongladeEmailClient moongladeEmailClient, IBlogConfig blogConfig) : INotificationHandler<PingbackNotification>
{
    public async Task Handle(PingbackNotification notification, CancellationToken ct)
    {
        var dl = new[] { blogConfig.GeneralSettings.OwnerEmail };
        await moongladeEmailClient.SendEmail(MailMesageTypes.BeingPinged, dl, notification);
    }
}