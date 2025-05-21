using Moonglade.Core.PostFeature;

namespace Moonglade.Web.BackgroundServices;

public class ScheduledPublishService(IServiceProvider serviceProvider, ILogger<ScheduledPublishService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndPublishPostsAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in ScheduledPublishService: {Message}", ex.Message);
            }
            finally
            {
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }

    private async Task CheckAndPublishPostsAsync()
    {
        using var scope = serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        int rows = await mediator.Send(new PublishScheduledPostCommand(), CancellationToken.None);

        if (rows > 0)
        {
            logger.LogInformation("Published {Count} scheduled posts", rows);
        }
    }
}
