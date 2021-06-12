using System;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Net.Http.Headers;
using Moonglade.Utils;

namespace Moonglade.Core
{
    public interface IReleaseCheckerClient
    {
        Task<ReleaseInfo> CheckNewReleaseAsync();
    }

    public class ReleaseCheckerClient : IReleaseCheckerClient
    {
        private readonly HttpClient _httpClient;

        public ReleaseCheckerClient(
            IConfiguration configuration,
            HttpClient httpClient)
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
        }

        public async Task<ReleaseInfo> CheckNewReleaseAsync()
        {
            var req = new HttpRequestMessage(HttpMethod.Get, string.Empty);
            var response = await _httpClient.SendAsync(req);

            if (!response.IsSuccessStatusCode) throw new($"CheckNewReleaseAsync() failed, response code: '{response.StatusCode}'");

            var json = await response.Content.ReadAsStringAsync();
            var info = JsonSerializer.Deserialize<ReleaseInfo>(json);
            return info;
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
