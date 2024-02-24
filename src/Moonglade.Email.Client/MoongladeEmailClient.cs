using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moonglade.Configuration;
using System.Text;
using System.Text.Json;

namespace Moonglade.Email.Client;

public interface IMoongladeEmailClient
{
    Task SendEmail<T>(MailMesageTypes type, string[] receipts, T payload) where T : class;
}

public class MoongladeEmailClient : IMoongladeEmailClient
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<MoongladeEmailClient> _logger;
    private readonly HttpClient _httpClient;
    private readonly IBlogConfig _blogConfig;

    private readonly bool _enabled;

    public MoongladeEmailClient(IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration,
        ILogger<MoongladeEmailClient> logger,
        HttpClient httpClient,
        IBlogConfig blogConfig)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
        _httpClient = httpClient;
        _blogConfig = blogConfig;

        if (!string.IsNullOrWhiteSpace(configuration["Email:ApiEndpoint"]))
        {
            _httpClient.BaseAddress = new(configuration["Email:ApiEndpoint"]);
            _httpClient.DefaultRequestHeaders.Add("x-functions-key", configuration["Email:ApiKey"]);
            _enabled = true;
        }
        else
        {
            _logger.LogError("Email:ApiEndpoint is empty");
            _enabled = false;
        }
    }

    /// <summary>
    /// Send email to `/api/enqueue` endpoint
    /// </summary>
    public async Task SendEmail<T>(MailMesageTypes type, string[] receipts, T payload) where T : class
    {
        if (!_blogConfig.NotificationSettings.EnableEmailSending || !_enabled) return;

        try
        {
            var en = new EmailNotification
            {
                Type = type.ToString(),
                Receipts = receipts,
                Payload = payload,
                OriginAspNetRequestId = _httpContextAccessor.HttpContext?.TraceIdentifier
            };

            // Note: Do not use `PostAsJsonAsync` here, Azure Function will blow up on encoded http request
            var json = JsonSerializer.Serialize(en);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/api/enqueue", content);
            response.EnsureSuccessStatusCode();

            _logger.LogInformation($"Email sent: {json}");
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
            throw;
        }
    }
}