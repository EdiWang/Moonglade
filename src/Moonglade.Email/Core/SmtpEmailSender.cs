using Edi.TemplateEmail;
using Edi.TemplateEmail.Smtp;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;

namespace Moonglade.Email.Core;

public class SmtpEmailSender(
    EmailSettings smtpSettings,
    IOptions<EmailServiceOptions> options,
    ILogger<SmtpEmailSender> logger) : IEmailDispatcher
{
    public async Task SendAsync(CommonMailMessage message)
    {
        var senderAddress = options.Value.EffectiveSmtpSenderAddress;
        var mimeMessage = new MimeMessage
        {
            Sender = new MailboxAddress(smtpSettings.SenderName, senderAddress),
            Subject = message.Subject,
            Body = new TextPart(message.BodyIsHtml ? TextFormat.Html : TextFormat.Plain) { Text = message.Body }
        };
        mimeMessage.From.Add(new MailboxAddress(smtpSettings.EmailDisplayName, senderAddress));
        foreach (var recipient in message.Receipts)
        {
            mimeMessage.To.Add(MailboxAddress.Parse(recipient));
        }

        foreach (var recipient in message.CcReceipts ?? [])
        {
            mimeMessage.Cc.Add(MailboxAddress.Parse(recipient));
        }

        using var smtp = new SmtpClient();
        var settings = smtpSettings.SmtpSettings;
        if (settings.SkipCertificateValidation)
        {
            smtp.ServerCertificateValidationCallback = (_, _, _, _) => true;
        }

        await smtp.ConnectAsync(
            settings.SmtpServer,
            settings.SmtpServerPort,
            GetSocketOptions(settings.SmtpServerPort, settings.EnableTls));
        if (!string.IsNullOrEmpty(settings.SmtpUserName))
        {
            await smtp.AuthenticateAsync(settings.SmtpUserName, settings.SmtpPassword);
        }

        var response = await smtp.SendAsync(mimeMessage);
        await smtp.DisconnectAsync(true);
        logger.LogInformation("SMTP response: {Response}", response);
    }

    internal static SecureSocketOptions GetSocketOptions(int port, bool enableTls) =>
        port == 465 ? SecureSocketOptions.SslOnConnect :
        enableTls ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;
}
