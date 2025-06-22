using Moonglade.Core.PostFeature;

namespace Moonglade.Web.BackgroundServices;

public class ScheduledPublishService(
    IServiceProvider serviceProvider,
    IConfiguration configuration,
    ILogger<ScheduledPublishService> logger
    ) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("ScheduledPublishService started.");

        int taskDelay = configuration.GetValue("PostScheduler:TaskIntervalMinutes", 1);
        if (taskDelay <= 0)
        {
            logger.LogWarning("Invalid TaskIntervalMinutes: {Value}. Using default of 1 minute.", taskDelay);
            taskDelay = 1;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(taskDelay));
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await CheckAndPublishPostsAsync(stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogError(ex, "Error in ScheduledPublishService: {Message}", ex.Message);
                }
            }
        }
        finally
        {
            logger.LogInformation("ScheduledPublishService stopped.");
        }
    }

    private async Task CheckAndPublishPostsAsync(CancellationToken cancellationToken)
    {
        // Use 'await using' if your scope implements IAsyncDisposable
        using var scope = serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        int rows = await mediator.Send(new PublishScheduledPostCommand(), cancellationToken);

        if (rows > 0)
        {
            logger.LogInformation("Published {Count} scheduled posts", rows);
        }
    }
}
