using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;

namespace Moonglade.Web.Configuration;

public class PingEndpoint
{
    // Cache the JsonSerializerOptions instance to avoid recreating it for every serialization operation
    private static readonly JsonSerializerOptions CachedJsonSerializerOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static async Task WriteResponse(HttpContext context, HealthReport result)
    {
        // Set proper content type for health check responses
        context.Response.ContentType = "application/json";

        // Set HTTP status code based on health check result
        context.Response.StatusCode = result.Status switch
        {
            HealthStatus.Healthy => 200,
            HealthStatus.Degraded => 200, // Still operational but with issues
            HealthStatus.Unhealthy => 503, // Service unavailable
            _ => 500
        };

        var response = new
        {
            Status = result.Status.ToString().ToLowerInvariant(),
            ClientIP = Helper.GetClientIP(context),
            ClientIPExperimental = ClientIPHelper.GetClientIP(context),
            Duration = result.TotalDuration.TotalMilliseconds,
            Timestamp = DateTimeOffset.UtcNow,
            Version = VersionHelper.AppVersion,
            Environment = EnvironmentHelper.GetEnvironmentTags(),
            GeoMatch = context.Request.Headers["x-geo-match"].ToString(),
            Checks = result.Entries.ToDictionary(
                entry => entry.Key,
                entry => new
                {
                    Status = entry.Value.Status.ToString().ToLowerInvariant(),
                    Duration = entry.Value.Duration.TotalMilliseconds,
                    Description = entry.Value.Description,
                    Data = entry.Value.Data.Count > 0 ? entry.Value.Data : null,
                    Exception = entry.Value.Exception?.Message
                }
            )
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, CachedJsonSerializerOptions));
    }
}