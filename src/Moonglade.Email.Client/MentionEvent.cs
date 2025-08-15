using LiteBus.Events.Abstractions;
using Moonglade.Configuration;

namespace Moonglade.Email.Client;

public record MentionEvent(
    string TargetPostTitle,
    string Domain,
    string SourceIp,
    string SourceUrl,
    string SourceTitle) : IEvent;

public class MentionNotificationHandler(IMoongladeEmailClient moongladeEmailClient, IBlogConfig blogConfig) : IEventHandler<MentionEvent>
{
    public async Task HandleAsync(MentionEvent notification, CancellationToken ct) =>
        await moongladeEmailClient.SendEmailAsync(MailMesageTypes.BeingPinged, [blogConfig.GeneralSettings.OwnerEmail], notification, ct);
}