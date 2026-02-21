using Cronos;

namespace Moonglade.Web.Services;

public class UpdateCheckService(
    IGitHubReleaseClient gitHubReleaseClient,
    UpdateCheckerState updateCheckerState,
    IConfiguration configuration,
    ILogger<UpdateCheckService> logger) : BackgroundService
{
    private const string DefaultCron = "0 6 * * *";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!configuration.GetValue("EnableUpdateCheck", true))
        {
            logger.LogInformation("UpdateCheckService is disabled via configuration.");
            return;
        }

        var cronExpression = configuration.GetValue<string>("UpdateCheckCron") ?? DefaultCron;
        CronExpression cron;
        try
        {
            cron = CronExpression.Parse(cronExpression);
        }
        catch (CronFormatException ex)
        {
            logger.LogError(ex, "Invalid UpdateCheckCron expression: '{CronExpression}'. Service will not start.", cronExpression);
            return;
        }

        logger.LogInformation("UpdateCheckService started with CRON schedule: {CronExpression}", cronExpression);

        while (!stoppingToken.IsCancellationRequested)
        {
            var utcNow = DateTime.UtcNow;
            var nextOccurrence = cron.GetNextOccurrence(utcNow, inclusive: false);
            if (nextOccurrence is null)
            {
                logger.LogWarning("No next occurrence found for CRON expression: '{CronExpression}'. Stopping.", cronExpression);
                break;
            }

            var delay = nextOccurrence.Value - utcNow;
            logger.LogInformation("Next update check at {NextCheck} (in {Delay}).", nextOccurrence.Value, delay);

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            await CheckForUpdateAsync(stoppingToken);
        }
    }

    private async Task CheckForUpdateAsync(CancellationToken cancellationToken)
    {
        try
        {
            var tagName = await gitHubReleaseClient.GetLatestReleaseTagAsync(cancellationToken);
            logger.LogInformation("Latest GitHub release tag: {TagName}", tagName);

            // Parse remote version: tag_name is like "v15.7.0"
            var remoteVersionString = tagName.TrimStart('v', 'V');
            if (!Version.TryParse(remoteVersionString, out var remoteVersion))
            {
                logger.LogWarning("Failed to parse remote version from tag: {TagName}", tagName);
                return;
            }

            // Parse local version: may contain suffix like "15.8.0-preview"
            var localVersionString = VersionHelper.AppVersionBasic;
            if (!Version.TryParse(localVersionString, out var localVersion))
            {
                logger.LogWarning("Failed to parse local version: {LocalVersion}", localVersionString);
                return;
            }

            if (remoteVersion > localVersion)
            {
                updateCheckerState.SetNewVersion(tagName);
                logger.LogInformation("New Moonglade version available: {NewVersion} (current: {CurrentVersion})", tagName, VersionHelper.AppVersion);
            }
            else
            {
                updateCheckerState.SetNewVersion(null);
                logger.LogInformation("Moonglade is up to date. Remote: {RemoteVersion}, Local: {LocalVersion}", remoteVersion, localVersion);
            }
        }
        catch (OperationCanceledException)
        {
            // Shutting down, don't log as error
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking for Moonglade updates.");
        }
    }
}
