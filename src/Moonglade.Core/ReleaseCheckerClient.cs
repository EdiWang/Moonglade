using System;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Moonglade.Utils;
using Polly;

namespace Moonglade.Core
{
    public interface IReleaseCheckerClient
    {
        Task<ReleaseInfo> CheckNewReleaseAsync();
    }

    public class ReleaseCheckerClient : IReleaseCheckerClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ReleaseCheckerClient> _logger;

        public ReleaseCheckerClient(
            IConfiguration configuration,
            HttpClient httpClient,
            ILogger<ReleaseCheckerClient> logger)
        {
            var apiAddress = configuration["ReleaseCheckApiAddress"];
            if (string.IsNullOrWhiteSpace(apiAddress) ||
                !Uri.IsWellFormedUriString(apiAddress, UriKind.RelativeOrAbsolute))
            {
                throw new InvalidOperationException($"'{apiAddress}' is not a valid API address.");
            }

            httpClient.BaseAddress = new(apiAddress);
            httpClient.DefaultRequestHeaders.Add(HeaderNames.Accept, "application/json");
            httpClient.DefaultRequestHeaders.Add(HeaderNames.UserAgent, $"Moonglade/{Helper.AppVersion}");

            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<ReleaseInfo> CheckNewReleaseAsync()
        {
            try
            {
                var req = new HttpRequestMessage(HttpMethod.Get, string.Empty);
                var response = await _httpClient.SendAsync(req);

                if (!response.IsSuccessStatusCode) throw new($"CheckNewReleaseAsync() failed, response code: '{response.StatusCode}'");

                var json = await response.Content.ReadAsStringAsync();
                var info = JsonSerializer.Deserialize<ReleaseInfo>(json);
                return info;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }

    public static class ServiceCollectionExtensions
    {
        public static void AddReleaseCheckerClient(this IServiceCollection services)
        {
            services.AddHttpClient<IReleaseCheckerClient, ReleaseCheckerClient>()
                .AddTransientHttpErrorPolicy(builder =>
                    builder.WaitAndRetryAsync(3, retryCount => TimeSpan.FromSeconds(Math.Pow(2, retryCount))));
        }
    }

    public class ReleaseInfo
    {
        [JsonPropertyName("html_url")]
        public string HtmlUrl { get; set; }

        [JsonPropertyName("tag_name")]
        public string TagName { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("prerelease")]
        public bool PreRelease { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
