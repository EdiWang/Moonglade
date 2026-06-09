using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace Moonglade.Webmention;

public interface IWebmentionSourceRateLimiter
{
    bool TryAcquire(Uri sourceUri);
}

public class WebmentionSourceRateLimiter(
    IOptionsMonitor<WebmentionSourceRateLimitOptions> options,
    TimeProvider timeProvider,
    ILogger<WebmentionSourceRateLimiter> logger) : IWebmentionSourceRateLimiter
{
    private readonly ConcurrentDictionary<string, RateLimitCounter> _counters = new(StringComparer.OrdinalIgnoreCase);

    public bool TryAcquire(Uri sourceUri)
    {
        var settings = options.CurrentValue;
        if (!settings.Enabled || settings.PermitLimit < 1 || settings.WindowMinutes < 1)
        {
            return true;
        }

        var partitionKey = sourceUri.Host.ToLowerInvariant();
        var window = TimeSpan.FromMinutes(settings.WindowMinutes);
        var now = timeProvider.GetUtcNow();
        var counter = _counters.GetOrAdd(partitionKey, _ => new RateLimitCounter(now));

        lock (counter)
        {
            if (now - counter.WindowStartedAt >= window)
            {
                counter.WindowStartedAt = now;
                counter.Count = 0;
            }

            if (counter.Count >= settings.PermitLimit)
            {
                logger.LogWarning(
                    "Webmention source rate limit exceeded for source host {SourceHost}. PermitLimit: {PermitLimit}, WindowMinutes: {WindowMinutes}",
                    partitionKey,
                    settings.PermitLimit,
                    settings.WindowMinutes);
                return false;
            }

            counter.Count++;
            return true;
        }
    }

    private sealed class RateLimitCounter(DateTimeOffset windowStartedAt)
    {
        public DateTimeOffset WindowStartedAt { get; set; } = windowStartedAt;
        public int Count { get; set; }
    }
}
