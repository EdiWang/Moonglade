using Edi.TemplateEmail;
using Edi.TemplateEmail.Smtp;
using Microsoft.Extensions.Logging;

namespace Moonglade.Email.Core;

public class SmtpEmailSender(
    EmailSettings smtpSettings,
    ILogger<SmtpEmailSender> logger) : IEmailDispatcher
{
    public async Task SendAsync(CommonMailMessage message)
    {
        var response = await message.SendAsync(smtpSettings);
        logger.LogInformation("SMTP response: {Response}", response);
    }
}
