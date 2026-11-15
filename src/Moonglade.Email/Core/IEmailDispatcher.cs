using Edi.TemplateEmail;
using Edi.TemplateEmail.Smtp;

namespace Moonglade.Email.Core;

public interface IEmailDispatcher
{
    Task SendAsync(CommonMailMessage message, EmailSettings settings);
}
