using Edi.TemplateEmail;

namespace Moonglade.Email.Core;

public interface IEmailProviderSender
{
    string Provider { get; }

    Task SendAsync(CommonMailMessage message);
}
