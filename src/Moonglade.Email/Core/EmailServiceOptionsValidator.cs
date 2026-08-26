using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;

namespace Moonglade.Email.Core;

public class EmailServiceOptionsValidator : IValidateOptions<EmailServiceOptions>
{
    private static readonly EmailAddressAttribute EmailAddressAttribute = new();

    public ValidateOptionsResult Validate(string name, EmailServiceOptions options)
    {
        var issues = GetIssues(options);

        return issues.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(issues.Select(issue => issue.Message));
    }

    internal IReadOnlyList<EmailServiceConfigurationIssue> GetIssues(EmailServiceOptions options)
    {
        var issues = new List<EmailServiceConfigurationIssue>();
        var provider = options.NormalizedProvider;

        if (provider is not EmailServiceOptions.SmtpProvider and not EmailServiceOptions.AzureCommunicationProvider)
        {
            issues.Add(EmailServiceConfigurationIssue.Invalid(
                $"Email provider '{options.Provider}' is not supported. Supported values: smtp, AzureCommunication."));
        }

        if (options.SmtpPort is < 1 or > 65535)
        {
            issues.Add(EmailServiceConfigurationIssue.Invalid(
                "Email:SmtpPort must be between 1 and 65535."));
        }

        switch (provider)
        {
            case EmailServiceOptions.SmtpProvider:
                ValidateSmtp(options, issues);
                break;

            case EmailServiceOptions.AzureCommunicationProvider:
                ValidateAzureCommunication(options, issues);
                break;
        }

        return issues;
    }

    private static void ValidateSmtp(
        EmailServiceOptions options,
        List<EmailServiceConfigurationIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(options.SmtpServer))
        {
            issues.Add(EmailServiceConfigurationIssue.Missing(
                "Email:SmtpServer is required when Email:Provider is smtp."));
        }

        if (string.IsNullOrWhiteSpace(options.SmtpUserName))
        {
            issues.Add(EmailServiceConfigurationIssue.Missing(
                "Email:SmtpUserName is required when Email:Provider is smtp."));
        }

        if (string.IsNullOrWhiteSpace(options.SmtpPassword))
        {
            issues.Add(EmailServiceConfigurationIssue.Missing(
                "Email:SmtpPassword is required when Email:Provider is smtp."));
        }
    }

    private static void ValidateAzureCommunication(
        EmailServiceOptions options,
        List<EmailServiceConfigurationIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(options.AcsConnectionString))
        {
            issues.Add(EmailServiceConfigurationIssue.Missing(
                "Email:AcsConnectionString is required when Email:Provider is AzureCommunication."));
        }

        if (string.IsNullOrWhiteSpace(options.AcsSenderAddress))
        {
            issues.Add(EmailServiceConfigurationIssue.Missing(
                "Email:AcsSenderAddress is required when Email:Provider is AzureCommunication."));
            return;
        }

        if (!EmailAddressAttribute.IsValid(options.AcsSenderAddress))
        {
            issues.Add(EmailServiceConfigurationIssue.Invalid(
                "Email:AcsSenderAddress must be a valid email address."));
        }
    }
}

internal enum EmailServiceConfigurationIssueKind
{
    Missing,
    Invalid
}

internal sealed record EmailServiceConfigurationIssue(
    EmailServiceConfigurationIssueKind Kind,
    string Message)
{
    public static EmailServiceConfigurationIssue Missing(string message) =>
        new(EmailServiceConfigurationIssueKind.Missing, message);

    public static EmailServiceConfigurationIssue Invalid(string message) =>
        new(EmailServiceConfigurationIssueKind.Invalid, message);
}
