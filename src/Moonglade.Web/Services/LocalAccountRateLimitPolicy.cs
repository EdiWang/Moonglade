using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Threading.RateLimiting;

namespace Moonglade.Web.Services;

public class LocalAccountRateLimitPolicy(IOptionsMonitor<LocalAccountRateLimitOptions> options) : IRateLimiterPolicy<string>
{
    public const string PolicyName = "LocalAccountAuth";

    public Func<OnRejectedContext, CancellationToken, ValueTask> OnRejected => RejectAsync;

    public RateLimitPartition<string> GetPartition(HttpContext httpContext)
    {
        var settings = options.CurrentValue;
        var partitionKey = GetPartitionKey(httpContext);

        if (!settings.Enabled || !HttpMethods.IsPost(httpContext.Request.Method))
        {
            return RateLimitPartition.GetNoLimiter(partitionKey);
        }

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = settings.PermitLimit,
                Window = TimeSpan.FromMinutes(settings.WindowMinutes),
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                AutoReplenishment = true
            });
    }

    internal static string GetPartitionKey(HttpContext httpContext)
    {
        var clientIp = NormalizeKeyPart(ClientIPHelper.GetClientIP(httpContext), "unknown-ip");
        var authStep = GetAuthStep(httpContext);
        var account = authStep == "signin"
            ? GetFormValue(httpContext, "Username")
            : httpContext.User.Identity?.Name;

        return $"{authStep}|{clientIp}|{NormalizeKeyPart(account, "unknown-account")}";
    }

    private static string GetAuthStep(HttpContext httpContext)
    {
        var path = httpContext.Request.Path.Value;
        return path?.Contains("signin", StringComparison.OrdinalIgnoreCase) == true
            ? "signin"
            : "totp";
    }

    private static string GetFormValue(HttpContext httpContext, string name)
    {
        if (!httpContext.Request.HasFormContentType)
        {
            return null;
        }

        try
        {
            return httpContext.Request.Form[name].ToString();
        }
        catch (Exception ex) when (ex is InvalidOperationException or InvalidDataException or IOException or BadHttpRequestException)
        {
            return null;
        }
    }

    private static string NormalizeKeyPart(string value, string fallback) =>
        string.IsNullOrWhiteSpace(value)
            ? fallback
            : value.Trim().ToLowerInvariant();

    private static async ValueTask RejectAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        var response = context.HttpContext.Response;
        response.StatusCode = StatusCodes.Status429TooManyRequests;

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            response.Headers.RetryAfter = Math.Ceiling(retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
        }

        var problemDetailsService = context.HttpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
        await problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = context.HttpContext,
            ProblemDetails =
            {
                Status = StatusCodes.Status429TooManyRequests,
                Title = "Too many authentication attempts",
                Detail = "Please wait before trying to sign in again.",
                Type = "https://tools.ietf.org/html/rfc6585#section-4"
            }
        });
    }
}
