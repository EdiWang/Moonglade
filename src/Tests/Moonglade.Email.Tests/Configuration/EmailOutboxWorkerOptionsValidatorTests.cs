using Microsoft.Extensions.Options;
using Moonglade.Email.Core;

namespace Moonglade.Email.Tests.Configuration;

public class EmailOutboxWorkerOptionsValidatorTests
{
    [Fact]
    public void Validate_InvalidValues_Fails()
    {
        var validator = new EmailOutboxWorkerOptionsValidator();
        var options = new EmailOutboxWorkerOptions
        {
            BatchSize = 0,
            PollIntervalSeconds = 0,
            LeaseDurationSeconds = 0,
            MaxAttempts = 0,
            InitialRetryDelaySeconds = 60,
            MaxRetryDelaySeconds = 30
        };

        var result = validator.Validate(Options.DefaultName, options);

        Assert.True(result.Failed);
        Assert.Contains("BatchSize", result.FailureMessage);
        Assert.Contains("MaxRetryDelaySeconds", result.FailureMessage);
    }

}
