namespace Moonglade.BackgroundServices;

public interface IGitHubReleaseClient
{
    Task<string> GetLatestReleaseTagAsync(CancellationToken cancellationToken = default);
}
