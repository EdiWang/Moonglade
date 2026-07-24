using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Threading.RateLimiting;

namespace Moonglade.Web.Services;

public class CommentRateLimitPolicy(IOptionsMonitor<CommentRateLimitOptions> options) : IRateLimiterPolicy<string>
{
    public const string PolicyName = "CommentByIpAndPost";

    public Func<OnRejectedContext, CancellationToken, ValueTask> OnRejected => RejectAsync;

    public RateLimitPartition<string> GetPartition(HttpContext httpContext)
    {
        var settings = options.CurrentValue;
        var partitionKey = GetPartitionKey(httpContext);

        if (!settings.Enabled)
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
        var clientIp = ClientIPHelper.GetClientIP(httpContext);
        if (string.IsNullOrWhiteSpace(clientIp))
        {
            clientIp = "unknown-ip";
        }

        var postId = httpContext.Request.RouteValues.TryGetValue("postId", out var routeValue)
            ? routeValue?.ToString()
            : null;

        if (!Guid.TryParse(postId, out var parsedPostId))
        {
            postId = "unknown-post";
        }
        else
        {
            postId = parsedPostId.ToString("D", CultureInfo.InvariantCulture);
        }

        return $"{clientIp.Trim().ToLowerInvariant()}|{postId}";
    }

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
                Title = "Too many comment submissions",
                Detail = "Please wait before submitting another comment.",
                Type = "https://tools.ietf.org/html/rfc6585#section-4"
            }
        });
    }
}
