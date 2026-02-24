using System.Text.Json;

namespace Moonglade.BackgroundServices;

public class GitHubReleaseClient(HttpClient httpClient) : IGitHubReleaseClient
{
    public async Task<string> GetLatestReleaseTagAsync(CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync("repos/EdiWang/Moonglade/releases/latest", cancellationToken);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        return doc.RootElement.GetProperty("tag_name").GetString()
               ?? throw new InvalidOperationException("tag_name is null in GitHub release response.");
    }
}
