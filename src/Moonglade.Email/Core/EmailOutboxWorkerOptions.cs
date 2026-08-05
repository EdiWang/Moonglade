using Microsoft.Extensions.Options;

namespace Moonglade.Email.Core;

public class EmailOutboxWorkerOptions
{
    public const string SectionName = "Email:OutboxWorker";

    public bool Enabled { get; set; } = true;
    public int BatchSize { get; set; } = 5;
    public int PollIntervalSeconds { get; set; } = 30;
    public int LeaseDurationSeconds { get; set; } = 300;
    public int MaxAttempts { get; set; } = 3;
    public int InitialRetryDelaySeconds { get; set; } = 60;
    public int MaxRetryDelaySeconds { get; set; } = 3600;

    public TimeSpan PollInterval => TimeSpan.FromSeconds(PollIntervalSeconds);
    public TimeSpan LeaseDuration => TimeSpan.FromSeconds(LeaseDurationSeconds);
    public TimeSpan InitialRetryDelay => TimeSpan.FromSeconds(InitialRetryDelaySeconds);
    public TimeSpan MaxRetryDelay => TimeSpan.FromSeconds(MaxRetryDelaySeconds);
}

public class EmailOutboxWorkerOptionsValidator : IValidateOptions<EmailOutboxWorkerOptions>
{
    public ValidateOptionsResult Validate(string name, EmailOutboxWorkerOptions options)
    {
        var errors = new List<string>();

        if (options.BatchSize <= 0)
        {
            errors.Add("Email:OutboxWorker:BatchSize must be greater than 0.");
        }

        if (options.PollIntervalSeconds <= 0)
        {
            errors.Add("Email:OutboxWorker:PollIntervalSeconds must be greater than 0.");
        }

        if (options.LeaseDurationSeconds <= 0)
        {
            errors.Add("Email:OutboxWorker:LeaseDurationSeconds must be greater than 0.");
        }

        if (options.MaxAttempts <= 0)
        {
            errors.Add("Email:OutboxWorker:MaxAttempts must be greater than 0.");
        }

        if (options.InitialRetryDelaySeconds <= 0)
        {
            errors.Add("Email:OutboxWorker:InitialRetryDelaySeconds must be greater than 0.");
        }

        if (options.MaxRetryDelaySeconds < options.InitialRetryDelaySeconds)
        {
            errors.Add("Email:OutboxWorker:MaxRetryDelaySeconds must be greater than or equal to InitialRetryDelaySeconds.");
        }

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }
}
