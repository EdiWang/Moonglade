using LiteBus.Commands.Abstractions;
using LiteBus.Queries.Abstractions;
using Moonglade.Configuration;
using Moonglade.Features.Post;

namespace Moonglade.Web.BackgroundServices;

public class ScheduledPublishService(
    IServiceProvider serviceProvider,
    ILogger<ScheduledPublishService> logger,
    ScheduledPublishWakeUp wakeUp,
    IBlogConfig blogConfig
    ) : BackgroundService
{
    private static readonly TimeSpan MaxWaitInterval = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("ScheduledPublishService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!blogConfig.AdvancedSettings.EnablePostScheduler)
                {
                    await Task.Delay(MaxWaitInterval, stoppingToken);
                    continue;
                }

                DateTime? nextScheduleTime = await GetNextScheduledTimeAsync(stoppingToken);
                TimeSpan delay;

                if (nextScheduleTime.HasValue)
                {
                    logger.LogInformation("Next scheduled publish time: {NextScheduleTime}", nextScheduleTime.Value);

                    var utcNow = DateTime.UtcNow;
                    // Hit publish time or already past
                    delay = nextScheduleTime.Value > utcNow
                        ? nextScheduleTime.Value - utcNow
                        : TimeSpan.Zero;
                }
                else
                {
                    // no scheduled posts found, set a default delay
                    delay = MaxWaitInterval;

                    logger.LogInformation("No scheduled posts found, waiting for {MaxWaitInterval} before checking again.", MaxWaitInterval.TotalSeconds);
                }

                var wakeToken = wakeUp.GetWakeToken();
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, wakeToken);

                if (delay > TimeSpan.Zero)
                {
                    logger.LogInformation("Next scheduled publish in {Delay} seconds.", delay.TotalSeconds);
                    try
                    {
                        await Task.Delay(delay, linkedCts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        logger.LogWarning("Task.Delay was canceled, checking for wake-up or cancellation.");
                    }
                }

                await CheckAndPublishPostsAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in ScheduledPublishService: {Message}", ex.Message);
                // Pause for a while before retrying to avoid tight loop on errors
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }

        logger.LogInformation("ScheduledPublishService stopped.");
    }

    private async Task<DateTime?> GetNextScheduledTimeAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var queryMediator = scope.ServiceProvider.GetRequiredService<IQueryMediator>();

        var nextScheduledTime = await queryMediator.QueryAsync(new GetNextScheduledPostTimeQuery(), cancellationToken);
        return nextScheduledTime;
    }

    private async Task CheckAndPublishPostsAsync(CancellationToken cancellationToken)
    {
        // Use 'await using' if your scope implements IAsyncDisposable
        using var scope = serviceProvider.CreateScope();
        var commandMediator = scope.ServiceProvider.GetRequiredService<ICommandMediator>();

        int rows = await commandMediator.SendAsync(new PublishScheduledPostCommand(), cancellationToken);

        if (rows > 0)
        {
            logger.LogInformation("Published {Count} scheduled posts", rows);
        }
    }
}
