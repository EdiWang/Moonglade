using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Moonglade.Webmention.Tests;

public class WebmentionSourceRateLimiterTests
{
    [Fact]
    public void TryAcquire_WhenLimitIsExceededForSourceHost_ReturnsFalse()
    {
        var limiter = CreateLimiter(new WebmentionSourceRateLimitOptions
        {
            Enabled = true,
            PermitLimit = 2,
            WindowMinutes = 10
        });

        Assert.True(limiter.TryAcquire(new Uri("https://source.example/post-1")));
        Assert.True(limiter.TryAcquire(new Uri("https://source.example/post-2")));
        Assert.False(limiter.TryAcquire(new Uri("https://source.example/post-3")));
    }

    [Fact]
    public void TryAcquire_TracksDifferentSourceHostsSeparately()
    {
        var limiter = CreateLimiter(new WebmentionSourceRateLimitOptions
        {
            Enabled = true,
            PermitLimit = 1,
            WindowMinutes = 10
        });

        Assert.True(limiter.TryAcquire(new Uri("https://first.example/post")));
        Assert.False(limiter.TryAcquire(new Uri("https://first.example/another-post")));
        Assert.True(limiter.TryAcquire(new Uri("https://second.example/post")));
    }

    [Fact]
    public void TryAcquire_WhenDisabled_AllowsRequests()
    {
        var limiter = CreateLimiter(new WebmentionSourceRateLimitOptions
        {
            Enabled = false,
            PermitLimit = 1,
            WindowMinutes = 10
        });

        Assert.True(limiter.TryAcquire(new Uri("https://source.example/post-1")));
        Assert.True(limiter.TryAcquire(new Uri("https://source.example/post-2")));
    }

    private static WebmentionSourceRateLimiter CreateLimiter(WebmentionSourceRateLimitOptions options)
    {
        var optionsMonitor = new Mock<IOptionsMonitor<WebmentionSourceRateLimitOptions>>();
        optionsMonitor.SetupGet(x => x.CurrentValue).Returns(options);

        return new WebmentionSourceRateLimiter(
            optionsMonitor.Object,
            TimeProvider.System,
            Mock.Of<ILogger<WebmentionSourceRateLimiter>>());
    }
}
