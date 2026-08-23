using Microsoft.Extensions.Options;

namespace Moonglade.Email.Core;

public enum EmailCapabilityState
{
    Available,
    NotConfigured,
    Invalid,
    Disabled
}

public sealed class EmailCapabilityStatus
{
    internal EmailCapabilityStatus(
        EmailCapabilityState state,
        IEnumerable<string> validationErrors)
    {
        State = state;
        ValidationErrors = Array.AsReadOnly(validationErrors.ToArray());
    }

    public EmailCapabilityState State { get; }

    public IReadOnlyList<string> ValidationErrors { get; }

    public bool IsAvailable => State == EmailCapabilityState.Available;
}

public sealed class EmailCapabilityStatusEvaluator(
    EmailServiceOptionsValidator serviceOptionsValidator,
    EmailOutboxWorkerOptionsValidator workerOptionsValidator)
{
    public EmailCapabilityStatus Evaluate(
        EmailServiceOptions serviceOptions,
        EmailOutboxWorkerOptions workerOptions)
    {
        ArgumentNullException.ThrowIfNull(serviceOptions);
        ArgumentNullException.ThrowIfNull(workerOptions);

        var serviceIssues = serviceOptionsValidator.GetIssues(serviceOptions);
        var validationErrors = serviceIssues
            .Where(issue => issue.Kind == EmailServiceConfigurationIssueKind.Invalid)
            .Select(issue => issue.Message)
            .ToList();

        var workerValidation = workerOptionsValidator.Validate(Options.DefaultName, workerOptions);
        if (workerValidation.Failed)
        {
            validationErrors.AddRange(workerValidation.Failures ?? []);
        }

        if (validationErrors.Count > 0)
        {
            return new(EmailCapabilityState.Invalid, validationErrors);
        }

        if (serviceIssues.Any(issue => issue.Kind == EmailServiceConfigurationIssueKind.Missing))
        {
            return new(EmailCapabilityState.NotConfigured, []);
        }

        if (!workerOptions.Enabled)
        {
            return new(EmailCapabilityState.Disabled, []);
        }

        return new(EmailCapabilityState.Available, []);
    }
}
