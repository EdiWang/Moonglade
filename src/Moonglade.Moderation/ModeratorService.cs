using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Moonglade.Moderation;

public interface IModeratorService
{
    public Task<string> Mask(string input);

    public Task<bool> Detect(params string[] input);
}

public class MoongladeModeratorService : IModeratorService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<MoongladeModeratorService> _logger;
    private readonly string _provider;
    private readonly string _localKeywords;
    private readonly HttpClient _httpClient;
    private readonly bool _enabled;

    public MoongladeModeratorService(
        IHttpContextAccessor httpContextAccessor, ILogger<MoongladeModeratorService> logger, HttpClient httpClient, IConfiguration configuration)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
        _provider = configuration["ContentModerator:Provider"]!.ToLower();
        _localKeywords = configuration["ContentModerator:LocalKeywords"];
        _httpClient = httpClient;

        var apiEndpoint = configuration["ContentModerator:ApiEndpoint"];
        var apiKey = configuration["ContentModerator:ApiKey"];

        if (_provider != "local" && string.IsNullOrWhiteSpace(apiEndpoint) && string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogError("ContentModerator API configuration is empty");
            _enabled = false;
        }

        _httpClient.Timeout = TimeSpan.FromSeconds(30);
        _httpClient.BaseAddress = new(apiEndpoint);
        _httpClient.DefaultRequestHeaders.Add("x-functions-key", apiKey);
        _enabled = true;
    }

    public async Task<string> Mask(string input)
    {
        if (!_enabled) return input;

        try
        {
            if (_provider == "local")
            {
                var localWordFilter = new LocalWordFilter(_localKeywords);
                return localWordFilter.ModerateContent(input);
            }

            var payload = new Payload
            {
                OriginAspNetRequestId = _httpContextAccessor.HttpContext?.TraceIdentifier,
                Contents =
                [
                    new Content
                    {
                        Id = "0",
                        RawText = input
                    }
                ]
            };

            var response = await _httpClient.PostAsync(
                $"/api/{_provider}/mask",
                new StringContent(JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json"));

            response.EnsureSuccessStatusCode();

            var moderatorResponse = await response.Content.ReadFromJsonAsync<ModeratorResponse>();
            return moderatorResponse?.ProcessedContents?[0].ProcessedText ?? input;
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message, e);
            return input;
        }
    }

    public async Task<bool> Detect(params string[] input)
    {
        if (!_enabled) return false;

        try
        {
            if (_provider == "local")
            {
                var localWordFilter = new LocalWordFilter(_localKeywords);
                return localWordFilter.HasBadWord(input);
            }

            var contents = input.Select(p => new Content()
            {
                Id = Guid.NewGuid().ToString(),
                RawText = p
            });

            var payload = new Payload
            {
                OriginAspNetRequestId = _httpContextAccessor.HttpContext?.TraceIdentifier,
                Contents = [.. contents]
            };

            var response = await _httpClient.PostAsync(
                $"/api/{_provider}/detect",
                new StringContent(JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json"));

            response.EnsureSuccessStatusCode();
            var moderatorResponse = await response.Content.ReadFromJsonAsync<ModeratorResponse>();
            return moderatorResponse?.Positive ?? false;
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message, e);
            return false;
        }
    }
}