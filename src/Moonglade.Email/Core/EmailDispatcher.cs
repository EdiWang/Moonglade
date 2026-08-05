using Edi.TemplateEmail;
using Microsoft.Extensions.Options;

namespace Moonglade.Email.Core;

public class EmailDispatcher(
    IOptions<EmailServiceOptions> options,
    IEnumerable<IEmailProviderSender> senders) : IEmailDispatcher
{
    public async Task SendAsync(CommonMailMessage message)
    {
        var sender = options.Value.NormalizedProvider;
        var providerSender = senders.FirstOrDefault(s => string.Equals(s.Provider, sender, StringComparison.OrdinalIgnoreCase));

        if (providerSender == null)
        {
            throw new InvalidOperationException($"Email provider '{sender}' is not supported.");
        }

        await providerSender.SendAsync(message);
    }
}
