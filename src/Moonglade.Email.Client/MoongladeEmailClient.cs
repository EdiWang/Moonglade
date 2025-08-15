using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moonglade.Configuration;
using System.Text;
using System.Text.Json;

namespace Moonglade.Email.Client;

public interface IMoongladeEmailClient
{
    Task<bool> SendEmailAsync<T>(MailMesageTypes type, string[] receipts, T payload, CancellationToken cancellationToken = default) where T : class;
}

public class MoongladeEmailClient : IMoongladeEmailClient
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<MoongladeEmailClient> _logger;
    private readonly HttpClient _httpClient;
    private readonly IBlogConfig _blogConfig;
    private readonly JsonSerializerOptions _jsonOptions;

    private readonly bool _enabled;
    private const string EnqueueEndpoint = "/api/enqueue";

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

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        _enabled = ConfigureHttpClient(configuration);
    }

    private bool ConfigureHttpClient(IConfiguration configuration)
    {
        var apiEndpoint = configuration["Email:ApiEndpoint"];
        var apiKeyHeader = configuration["Email:ApiKeyHeader"];
        var apiKey = configuration["Email:ApiKey"];

        if (string.IsNullOrWhiteSpace(apiEndpoint))
        {
            _logger.LogWarning("Email:ApiEndpoint is not configured. Email functionality will be disabled.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(apiKeyHeader) || string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("Email API credentials are not properly configured. Email functionality will be disabled.");
            return false;
        }

        try
        {
            _httpClient.BaseAddress = new Uri(apiEndpoint);
            _httpClient.DefaultRequestHeaders.Add(apiKeyHeader, apiKey);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to configure HTTP client for email service. Email functionality will be disabled.");
            return false;
        }
    }

    /// <summary>
    /// Send email to `/api/enqueue` endpoint
    /// </summary>
    /// <param name="type">Type of email message</param>
    /// <param name="receipts">Email recipients</param>
    /// <param name="payload">Email payload data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if email was sent successfully, false otherwise</returns>
    public async Task<bool> SendEmailAsync<T>(MailMesageTypes type, string[] receipts, T payload, CancellationToken cancellationToken = default) where T : class
    {
        // Validate inputs
        if (receipts == null || receipts.Length == 0)
        {
            _logger.LogWarning("Cannot send email: no recipients provided");
            return false;
        }

        if (payload == null)
        {
            _logger.LogWarning("Cannot send email: payload is null");
            return false;
        }

        // Check if email sending is enabled
        if (!_blogConfig.NotificationSettings.EnableEmailSending)
        {
            _logger.LogDebug("Email sending is disabled in blog configuration");
            return false;
        }

        if (!_enabled)
        {
            _logger.LogDebug("Email client is not properly configured");
            return false;
        }

        try
        {
            var emailNotification = new EmailNotification
            {
                Type = type.ToString(),
                Receipts = receipts,
                Payload = payload,
                OriginAspNetRequestId = _httpContextAccessor.HttpContext?.TraceIdentifier ?? Guid.NewGuid().ToString()
            };

            var json = JsonSerializer.Serialize(emailNotification, _jsonOptions);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var response = await _httpClient.PostAsync(EnqueueEndpoint, content, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Email notification queued successfully. Type: {EmailType}, Recipients: {RecipientCount}",
                    type, receipts.Length);
                return true;
            }

            _logger.LogWarning("Failed to queue email notification. Status: {StatusCode}, Type: {EmailType}, Recipients: {RecipientCount}",
                response.StatusCode, type, receipts.Length);

            return false;
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogWarning("Email service request timed out. Type: {EmailType}", type);
            return false;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error occurred while sending email notification. Type: {EmailType}", type);
            return false;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to serialize email notification payload. Type: {EmailType}", type);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while sending email notification. Type: {EmailType}", type);
            return false;
        }
    }
}