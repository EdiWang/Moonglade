using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Moonglade.Moderation;

public interface IRemoteModerationService
{
    Task<string> MaskAsync(string input, string requestId);
    Task<bool> DetectAsync(string[] input, string requestId);
}

public class RemoteModerationService(HttpClient httpClient, ILogger<RemoteModerationService> logger) : IRemoteModerationService
{
    public async Task<string> MaskAsync(string input, string requestId)
    {
        try
        {
            var payload = CreatePayload(requestId, [input]);

            var response = await httpClient.PostAsync(
                "/api/mask",
                new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

            response.EnsureSuccessStatusCode();

            var moderatorResponse = await response.Content.ReadFromJsonAsync<ModeratorResponse>();
            return moderatorResponse?.ProcessedContents?.Length > 0 ? moderatorResponse.ProcessedContents[0]?.ProcessedText ?? input : input;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "HTTP error occurred while masking content");
            return input;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogError(ex, "Request timeout while masking content");
            return input;
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "JSON parsing error while masking content");
            return input;
        }
    }

    public async Task<bool> DetectAsync(string[] input, string requestId)
    {
        try
        {
            var payload = CreatePayload(requestId, input);

            var response = await httpClient.PostAsync(
                "/api/detect",
                new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

            response.EnsureSuccessStatusCode();

            var moderatorResponse = await response.Content.ReadFromJsonAsync<ModeratorResponse>();
            return moderatorResponse?.Positive ?? false;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "HTTP error occurred while detecting content");
            return false;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogError(ex, "Request timeout while detecting content");
            return false;
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "JSON parsing error while detecting content");
            return false;
        }
    }

    private static Payload CreatePayload(string requestId, string[] inputs)
    {
        var contents = inputs.Select((input, index) => new Content
        {
            Id = index.ToString(),
            RawText = input
        }).ToArray();

        return new Payload
        {
            OriginAspNetRequestId = requestId,
            Contents = contents
        };
    }
}