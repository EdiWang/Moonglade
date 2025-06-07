using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Moonglade.IndexNow.Client;

public class IndexNowClient(ILogger<IndexNowClient> logger, IConfiguration configuration, IHttpClientFactory httpClientFactory) : IIndexNowClient
{
    private readonly string[] _pingTargets = configuration.GetSection("IndexNow:PingTargets").Get<string[]>();
    private readonly string _apiKey = configuration["IndexNow:ApiKey"] ?? throw new InvalidOperationException("IndexNow:ApiKey is not configured.");

    public static JsonSerializerOptions JsonOptions => new()
    {
        // Fix 422 issue, some search engines is fracking case sensitive!
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task SendRequestAsync(Uri uri)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            logger.LogWarning("IndexNow:ApiKey is not configured.");
            return;
        }

        if (_pingTargets == null || _pingTargets.Length == 0)
        {
            throw new InvalidOperationException("IndexNow:PingTargets is not configured.");
        }

        foreach (var pingTarget in _pingTargets)
        {
            var client = httpClientFactory.CreateClient(pingTarget);

            var requestBody = CreateRequestBody(uri);
            var content = new StringContent(JsonSerializer.Serialize(requestBody, JsonOptions), Encoding.UTF8, "application/json");

            try
            {
                var response = await client.PostAsync("/indexnow", content);
                await HandleResponseAsync(pingTarget, response);
            }
            catch (Exception e)
            {
                logger.LogError(e, $"Failed to send index request to '{pingTarget}'");
            }
        }
    }

    private IndexNowRequest CreateRequestBody(Uri uri)
    {
        // https://www.indexnow.org/documentation
        // "In this option 2, the location of a key file determines the set of URLs that can be included with this key. A key file located at http://example.com/catalog/key12457EDd.txt can include any URLs starting with http://example.com/catalog/ but cannot include URLs starting with http://example.com/help/."
        // "URLs that are not considered valid in option 2 may not be considered for indexing. It is strongly recommended that you use Option 1 and place your file key at the root directory of your web server."
        // This is why we should not set KeyLocation = $"https://{uri.Host}/xxxx.txt",

        return new()
        {
            Host = uri.Host,
            Key = _apiKey,
            UrlList = [uri.ToString()]
        };
    }

    private async Task HandleResponseAsync(string pingTarget, HttpResponseMessage response)
    {
        var responseBody = await response.Content.ReadAsStringAsync();

        switch (response.StatusCode)
        {
            // Success cases
            case HttpStatusCode.OK:
                logger.LogInformation($"Index request sent to '{pingTarget}', {response.StatusCode}: {responseBody}. URL submitted successfully.");
                break;
            case HttpStatusCode.Accepted:
                logger.LogWarning($"Index request sent to '{pingTarget}', {response.StatusCode}. URL received. IndexNow key validation pending.");
                break;

            // Error cases
            case HttpStatusCode.BadRequest:
                logger.LogError($"Index request sent to '{pingTarget}', {response.StatusCode}: {responseBody}. Invalid format.");
                break;
            case HttpStatusCode.Forbidden:
                logger.LogError($"Index request sent to '{pingTarget}', {response.StatusCode}: {responseBody}. Key not valid (e.g., key not found, file found but key not in the file).");
                break;
            case HttpStatusCode.UnprocessableEntity:
                logger.LogError($"Index request sent to '{pingTarget}', {response.StatusCode}: {responseBody}. URLs which don’t belong to the host or the key is not matching the schema in the protocol.");
                break;
            case HttpStatusCode.TooManyRequests:
                logger.LogError($"Index request sent to '{pingTarget}', {response.StatusCode}: {responseBody}. Too many requests (potential spam).");
                break;
            default:
                response.EnsureSuccessStatusCode();
                break;
        }
    }
}