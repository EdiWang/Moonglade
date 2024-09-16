using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Moonglade.IndexNow.Client;

public class IndexNowClient(ILogger<IndexNowClient> logger, IConfiguration configuration, IHttpClientFactory httpClientFactory) : IIndexNowClient
{
    public async Task SendRequestAsync(Uri uri)
    {
        string[] pingTargets = configuration.GetSection("IndexNow:PingTargets").Get<string[]>();
        var apiKey = configuration["IndexNow:ApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            logger.LogWarning("IndexNow:ApiKey is not configured.");
            return;
        }

        if (pingTargets == null || !pingTargets.Any())
        {
            throw new InvalidOperationException("IndexNow:PingTargets is not configured.");
        }

        foreach (var pingTarget in pingTargets)
        {
            var client = httpClientFactory.CreateClient(pingTarget);

            // https://www.indexnow.org/documentation
            var requestBody = new IndexNowRequest
            {
                Host = uri.Host,
                Key = apiKey,
                KeyLocation = $"https://{uri.Host}/indexnowkey.txt",
                UrlList = [uri.ToString()]
            };

            var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            try
            {
                var response = await client.PostAsync("/indexnow", content);
                response.EnsureSuccessStatusCode();

                logger.LogInformation($"Index request sent to '{pingTarget}'");
            }
            catch (Exception e)
            {
                logger.LogError(e, $"Failed to send index request to '{pingTarget}'");
            }
        }
    }
}