using Microsoft.Extensions.Options;
using MimeKit;
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
        if (options.SmtpPort is < 1 or > 65535)
        {
            issues.Add(EmailServiceConfigurationIssue.Invalid(
                "Email:SmtpPort must be between 1 and 65535."));
        }

        if (string.IsNullOrWhiteSpace(options.SmtpServer))
        {
            issues.Add(EmailServiceConfigurationIssue.Missing(
                "Email:SmtpServer is required."));
        }

        if (string.IsNullOrWhiteSpace(options.SmtpUserName))
        {
            issues.Add(EmailServiceConfigurationIssue.Missing(
                "Email:SmtpUserName is required."));
        }

        if (string.IsNullOrWhiteSpace(options.SmtpPassword))
        {
            issues.Add(EmailServiceConfigurationIssue.Missing(
                "Email:SmtpPassword is required."));
        }

        if (!string.IsNullOrWhiteSpace(options.SmtpSenderAddress) &&
            (!EmailAddressAttribute.IsValid(options.SmtpSenderAddress) ||
             !MailboxAddress.TryParse(options.SmtpSenderAddress, out var sender) ||
             sender.Address != options.SmtpSenderAddress))
        {
            issues.Add(EmailServiceConfigurationIssue.Invalid(
                "Email:SmtpSenderAddress must be a valid email address."));
        }

        return issues;
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
