using Azure;
using Azure.Communication.Email;
using Edi.TemplateEmail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Moonglade.Email.Core;

public class AzureCommunicationSender(
    IOptions<EmailServiceOptions> options,
    IAzureCommunicationEmailClient client,
    ILogger<AzureCommunicationSender> logger) : IEmailProviderSender
{
    public string Provider => EmailServiceOptions.AzureCommunicationProvider;

    public async Task SendAsync(CommonMailMessage message)
    {
        var emailMessage = new EmailMessage(
            senderAddress: options.Value.AcsSenderAddress,
            content: new EmailContent(message.Subject),
            recipients: new EmailRecipients(message.Receipts.Select(p => new EmailAddress(p))));

        if (message.BodyIsHtml)
        {
            emailMessage.Content.Html = message.Body;
        }
        else
        {
            emailMessage.Content.PlainText = message.Body;
        }

        var operationId = await client.SendAsync(WaitUntil.Started, emailMessage);
        logger.LogInformation("Azure Communication operation ID: {OperationId}", operationId);
    }
}
