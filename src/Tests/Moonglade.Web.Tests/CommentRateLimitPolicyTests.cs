using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moonglade.Web.Configuration;
using Moonglade.Web.Services;
using Moq;
using System.Net;

namespace Moonglade.Web.Tests;

public class CommentRateLimitPolicyTests
{
    [Fact]
    public void GetPartition_CombinesClientIpAndPostId()
    {
        var postId = Guid.NewGuid();
        var policy = CreatePolicy(new CommentRateLimitOptions());
        var context = CreateHttpContext(IPAddress.Parse("192.0.2.10"), postId);

        var partition = policy.GetPartition(context);

        Assert.Equal($"192.0.2.10|{postId:D}", partition.PartitionKey);
    }

    [Fact]
    public void GetPartition_TracksDifferentPostsSeparatelyForSameIp()
    {
        var policy = CreatePolicy(new CommentRateLimitOptions());
        var firstContext = CreateHttpContext(IPAddress.Parse("192.0.2.10"), Guid.NewGuid());
        var secondContext = CreateHttpContext(IPAddress.Parse("192.0.2.10"), Guid.NewGuid());

        var firstPartition = policy.GetPartition(firstContext);
        var secondPartition = policy.GetPartition(secondContext);

        Assert.NotEqual(firstPartition.PartitionKey, secondPartition.PartitionKey);
    }

    [Fact]
    public void GetPartition_WhenEnabled_AppliesConfiguredFixedWindowLimit()
    {
        var policy = CreatePolicy(new CommentRateLimitOptions
        {
            Enabled = true,
            PermitLimit = 1,
            WindowMinutes = 10
        });
        var context = CreateHttpContext(IPAddress.Parse("192.0.2.10"), Guid.NewGuid());
        var partition = policy.GetPartition(context);
        using var limiter = partition.Factory(partition.PartitionKey);

        using var firstLease = limiter.AttemptAcquire();
        using var secondLease = limiter.AttemptAcquire();

        Assert.True(firstLease.IsAcquired);
        Assert.False(secondLease.IsAcquired);
    }

    [Fact]
    public void GetPartition_WhenDisabled_AllowsRequests()
    {
        var policy = CreatePolicy(new CommentRateLimitOptions
        {
            Enabled = false,
            PermitLimit = 1,
            WindowMinutes = 10
        });
        var context = CreateHttpContext(IPAddress.Parse("192.0.2.10"), Guid.NewGuid());
        var partition = policy.GetPartition(context);
        using var limiter = partition.Factory(partition.PartitionKey);

        using var firstLease = limiter.AttemptAcquire();
        using var secondLease = limiter.AttemptAcquire();

        Assert.True(firstLease.IsAcquired);
        Assert.True(secondLease.IsAcquired);
    }

    private static CommentRateLimitPolicy CreatePolicy(CommentRateLimitOptions options)
    {
        var optionsMonitor = new Mock<IOptionsMonitor<CommentRateLimitOptions>>();
        optionsMonitor.SetupGet(x => x.CurrentValue).Returns(options);

        return new CommentRateLimitPolicy(optionsMonitor.Object);
    }

    private static DefaultHttpContext CreateHttpContext(IPAddress remoteIpAddress, Guid postId)
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = remoteIpAddress;
        context.Request.RouteValues["postId"] = postId.ToString("D");

        return context;
    }
}
