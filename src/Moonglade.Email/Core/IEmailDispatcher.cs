using Edi.TemplateEmail;

namespace Moonglade.Email.Core;

public interface IEmailDispatcher
{
    Task SendAsync(CommonMailMessage message);
}
