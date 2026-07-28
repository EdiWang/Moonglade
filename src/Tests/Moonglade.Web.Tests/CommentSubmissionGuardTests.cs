using Microsoft.Extensions.Options;
using Moonglade.Features.Comment;
using Moonglade.Web.Configuration;
using Moonglade.Web.Services;
using Moq;

namespace Moonglade.Web.Tests;

public class CommentSubmissionGuardTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 24, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Validate_WhenDisabled_AllowsRequest()
    {
        var guard = CreateGuard(new CommentSubmissionGuardOptions
        {
            Enabled = false,
            HoneypotEnabled = true,
            MinimumElapsedSeconds = 3,
            MaxFormAgeMinutes = 240
        });

        var result = guard.Validate(new CommentRequest
        {
            Source = "filled",
            FormRenderedUtc = Now.ToUnixTimeMilliseconds()
        });

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_WhenHoneypotIsFilled_RejectsRequest()
    {
        var guard = CreateGuard(new CommentSubmissionGuardOptions());

        var result = guard.Validate(CreateRequest(source: "https://spam.example", renderedAt: Now.AddSeconds(-10)));

        Assert.False(result.Succeeded);
        Assert.Equal(nameof(CommentRequest.Source), result.ModelStateKey);
    }

    [Fact]
    public void Validate_WhenSubmittedTooQuickly_RejectsRequest()
    {
        var guard = CreateGuard(new CommentSubmissionGuardOptions
        {
            MinimumElapsedSeconds = 3,
            MaxFormAgeMinutes = 240
        });

        var result = guard.Validate(CreateRequest(renderedAt: Now.AddSeconds(-1)));

        Assert.False(result.Succeeded);
        Assert.Equal(nameof(CommentRequest.FormRenderedUtc), result.ModelStateKey);
    }

    [Fact]
    public void Validate_WhenFormTimestampIsExpired_RejectsRequest()
    {
        var guard = CreateGuard(new CommentSubmissionGuardOptions
        {
            MinimumElapsedSeconds = 3,
            MaxFormAgeMinutes = 10
        });

        var result = guard.Validate(CreateRequest(renderedAt: Now.AddMinutes(-11)));

        Assert.False(result.Succeeded);
        Assert.Equal(nameof(CommentRequest.FormRenderedUtc), result.ModelStateKey);
    }

    [Fact]
    public void Validate_WhenRequestLooksHuman_AllowsRequest()
    {
        var guard = CreateGuard(new CommentSubmissionGuardOptions
        {
            MinimumElapsedSeconds = 3,
            MaxFormAgeMinutes = 240
        });

        var result = guard.Validate(CreateRequest(renderedAt: Now.AddSeconds(-10)));

        Assert.True(result.Succeeded);
    }

    private static CommentSubmissionGuard CreateGuard(CommentSubmissionGuardOptions options)
    {
        var optionsMonitor = new Mock<IOptionsMonitor<CommentSubmissionGuardOptions>>();
        optionsMonitor.SetupGet(x => x.CurrentValue).Returns(options);

        return new CommentSubmissionGuard(optionsMonitor.Object, new FixedTimeProvider(Now));
    }

    private static CommentRequest CreateRequest(string source = "", DateTimeOffset? renderedAt = null) => new()
    {
        Source = source,
        FormRenderedUtc = (renderedAt ?? Now.AddSeconds(-10)).ToUnixTimeMilliseconds()
    };

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
