using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Moonglade.IndexNow.Client;

public class IndexNowClient(
    ILogger<IndexNowClient> logger,
    IConfiguration configuration,
    IHttpClientFactory httpClientFactory) : IIndexNowClient
{
    private readonly string[] _pingTargets = configuration
        .GetSection("IndexNow:PingTargets")
        .Get<string[]>() ?? [];

    private readonly string _apiKey = configuration["IndexNow:ApiKey"]
        ?? throw new InvalidOperationException("IndexNow:ApiKey is not configured.");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        // Fix 422 issue: some search engines are case sensitive!
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task SendRequestAsync(Uri uri)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            logger.LogWarning("IndexNow:ApiKey is not configured.");
            return;
        }

        if (_pingTargets.Length == 0)
        {
            logger.LogWarning("IndexNow:PingTargets is not configured.");
            return;
        }

        var requestBody = CreateRequestBody(uri);
        var content = new StringContent(
            JsonSerializer.Serialize(requestBody, JsonOptions),
            Encoding.UTF8,
            "application/json");

        var tasks = _pingTargets.Select(pingTarget => SendToPingTargetAsync(pingTarget, content));
        await Task.WhenAll(tasks);
    }

    private async Task SendToPingTargetAsync(string pingTarget, HttpContent content)
    {
        var client = httpClientFactory.CreateClient(pingTarget);

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/indexnow") { Content = content };
            var response = await client.SendAsync(request);
            await HandleResponseAsync(pingTarget, response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send index request to '{PingTarget}'.", pingTarget);
        }
    }

    private IndexNowRequest CreateRequestBody(Uri uri)
    {
        // https://www.indexnow.org/documentation
        // "In this option 2, the location of a key file determines the set of URLs that can be included with this key. A key file located at http://example.com/catalog/key12457EDd.txt can include any URLs starting with http://example.com/catalog/ but cannot include URLs starting with http://example.com/help/."
        // "URLs that are not considered valid in option 2 may not be considered for indexing. It is strongly recommended that you use Option 1 and place your file key at the root directory of your web server."
        // This is why we should not set KeyLocation = $"https://{uri.Host}/xxxx.txt",

        return new IndexNowRequest
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
            case HttpStatusCode.OK:
                logger.LogInformation("Index request sent to '{PingTarget}', {StatusCode}: {ResponseBody}. URL submitted successfully.",
                    pingTarget, response.StatusCode, responseBody);
                break;
            case HttpStatusCode.Accepted:
                logger.LogWarning("Index request sent to '{PingTarget}', {StatusCode}. URL received. IndexNow key validation pending.",
                    pingTarget, response.StatusCode);
                break;
            case HttpStatusCode.BadRequest:
                logger.LogError("Index request sent to '{PingTarget}', {StatusCode}: {ResponseBody}. Invalid format.",
                    pingTarget, response.StatusCode, responseBody);
                break;
            case HttpStatusCode.Forbidden:
                logger.LogError("Index request sent to '{PingTarget}', {StatusCode}: {ResponseBody}. Key not valid.",
                    pingTarget, response.StatusCode, responseBody);
                break;
            case HttpStatusCode.UnprocessableEntity:
                logger.LogError("Index request sent to '{PingTarget}', {StatusCode}: {ResponseBody}. URL or key mismatch.",
                    pingTarget, response.StatusCode, responseBody);
                break;
            case HttpStatusCode.TooManyRequests:
                logger.LogError("Index request sent to '{PingTarget}', {StatusCode}: {ResponseBody}. Too many requests.",
                    pingTarget, response.StatusCode, responseBody);
                break;
            default:
                if (!response.IsSuccessStatusCode)
                {
                    logger.LogError("Index request sent to '{PingTarget}', {StatusCode}: {ResponseBody}. Unexpected error.",
                        pingTarget, response.StatusCode, responseBody);
                }
                break;
        }
    }
}
