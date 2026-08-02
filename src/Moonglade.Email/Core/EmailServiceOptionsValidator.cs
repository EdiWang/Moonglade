using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;

namespace Moonglade.Email.Core;

public class EmailServiceOptionsValidator : IValidateOptions<EmailServiceOptions>
{
    private static readonly EmailAddressAttribute EmailAddressAttribute = new();

    public ValidateOptionsResult Validate(string name, EmailServiceOptions options)
    {
        var errors = new List<string>();
        var provider = options.NormalizedProvider;

        if (provider is not EmailServiceOptions.SmtpProvider and not EmailServiceOptions.AzureCommunicationProvider)
        {
            errors.Add($"Email provider '{options.Provider}' is not supported. Supported values: smtp, AzureCommunication.");
        }

        if (options.SmtpPort is < 1 or > 65535)
        {
            errors.Add("Email:SmtpPort must be between 1 and 65535.");
        }

        switch (provider)
        {
            case EmailServiceOptions.SmtpProvider:
                ValidateSmtp(options, errors);
                break;

            case EmailServiceOptions.AzureCommunicationProvider:
                ValidateAzureCommunication(options, errors);
                break;
        }

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }

    private static void ValidateSmtp(EmailServiceOptions options, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(options.SmtpServer))
        {
            errors.Add("Email:SmtpServer is required when Email:Provider is smtp.");
        }

        if (string.IsNullOrWhiteSpace(options.SmtpUserName))
        {
            errors.Add("Email:SmtpUserName is required when Email:Provider is smtp.");
        }

        if (string.IsNullOrWhiteSpace(options.SmtpPassword))
        {
            errors.Add("Email:SmtpPassword is required when Email:Provider is smtp.");
        }
    }

    private static void ValidateAzureCommunication(EmailServiceOptions options, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(options.AcsConnectionString))
        {
            errors.Add("Email:AcsConnectionString is required when Email:Provider is AzureCommunication.");
        }

        if (string.IsNullOrWhiteSpace(options.AcsSenderAddress))
        {
            errors.Add("Email:AcsSenderAddress is required when Email:Provider is AzureCommunication.");
            return;
        }

        if (!EmailAddressAttribute.IsValid(options.AcsSenderAddress))
        {
            errors.Add("Email:AcsSenderAddress must be a valid email address.");
        }
    }
}
