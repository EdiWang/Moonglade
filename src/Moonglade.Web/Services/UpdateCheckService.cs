namespace Moonglade.Web.Services;

public class UpdateCheckService(
    IGitHubReleaseClient gitHubReleaseClient,
    UpdateCheckerState updateCheckerState,
    IConfiguration configuration,
    ILogger<UpdateCheckService> logger) : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(24);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!configuration.GetValue("EnableUpdateCheck", true))
        {
            logger.LogInformation("UpdateCheckService is disabled via configuration.");
            return;
        }

        // Wait until the next 6:00 UTC
        var delay = GetDelayUntilNextCheckTime();
        logger.LogInformation("UpdateCheckService started. First check in {Delay}.", delay);

        try
        {
            await Task.Delay(delay, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            await CheckForUpdateAsync(stoppingToken);

            try
            {
                await Task.Delay(CheckInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
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

    internal static TimeSpan GetDelayUntilNextCheckTime()
    {
        var now = DateTime.UtcNow;
        var nextCheck = now.Date.AddHours(6);
        if (now >= nextCheck)
        {
            nextCheck = nextCheck.AddDays(1);
        }

        return nextCheck - now;
    }
}
