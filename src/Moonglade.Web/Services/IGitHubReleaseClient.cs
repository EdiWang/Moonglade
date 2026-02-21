namespace Moonglade.Web.Services;

public interface IGitHubReleaseClient
{
    Task<string> GetLatestReleaseTagAsync(CancellationToken cancellationToken = default);
}
