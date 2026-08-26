using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Moonglade.Email.Core;

internal sealed class EmailCapabilityStatusReporter(
    EmailCapabilityStatus status,
    ILogger<EmailCapabilityStatusReporter> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        switch (status.State)
        {
            case EmailCapabilityState.Available:
                logger.LogInformation("Email notifications are available.");
                break;

            case EmailCapabilityState.NotConfigured:
                logger.LogWarning(
                    "Email notifications are not configured. Add the required Email provider settings to enable the feature.");
                break;

            case EmailCapabilityState.Invalid:
                logger.LogError(
                    "Email notification configuration is invalid: {ValidationErrors}",
                    string.Join(" ", status.ValidationErrors));
                break;

            case EmailCapabilityState.Disabled:
                logger.LogInformation(
                    "Email notifications are disabled because Email:OutboxWorker:Enabled is false.");
                break;
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
