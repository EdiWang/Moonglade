using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Moonglade.Email.Core;

public class EmailOutboxWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<EmailOutboxWorkerOptions> options,
    EmailCapabilityStatus capabilityStatus,
    ILogger<EmailOutboxWorker> logger) : BackgroundService
{
    private readonly string _workerId = $"{Environment.MachineName}-{Guid.NewGuid():N}";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!capabilityStatus.IsAvailable)
        {
            return;
        }

        var workerOptions = options.Value;
        logger.LogInformation("EmailOutboxWorker started with worker ID {WorkerId}.", _workerId);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(workerOptions, stoppingToken);
                await Task.Delay(workerOptions.PollInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in EmailOutboxWorker.");
                await Task.Delay(workerOptions.PollInterval, stoppingToken);
            }
        }

        logger.LogInformation("EmailOutboxWorker stopped.");
    }

    private async Task ProcessBatchAsync(EmailOutboxWorkerOptions workerOptions, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var processor = scope.ServiceProvider.GetRequiredService<IEmailOutboxMessageProcessor>();

        for (var i = 0; i < workerOptions.BatchSize; i++)
        {
            var processed = await processor.ProcessNextAsync(_workerId, cancellationToken);
            if (!processed)
            {
                break;
            }
        }
    }
}
