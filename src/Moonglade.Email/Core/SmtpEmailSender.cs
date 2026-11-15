using Edi.TemplateEmail;
using Edi.TemplateEmail.Smtp;
using Microsoft.Extensions.Logging;

namespace Moonglade.Email.Core;

public class SmtpEmailSender(ILogger<SmtpEmailSender> logger) : IEmailDispatcher
{
    public async Task SendAsync(CommonMailMessage message, EmailSettings settings)
    {
        var response = await message.SendAsync(settings);
        logger.LogInformation("SMTP response: {Response}", response);
    }
}
