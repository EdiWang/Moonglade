using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Moonglade.Web.Configuration;
using Moonglade.Web.Pages;
using Moonglade.Web.Services;
using Moq;
using System.Net;
using System.Security.Claims;

namespace Moonglade.Web.Tests;

public class LocalAccountRateLimitPolicyTests
{
    [Fact]
    public void GetPartition_CombinesSignInStepClientIpAndUsername()
    {
        var policy = CreatePolicy(new LocalAccountRateLimitOptions());
        var context = CreateHttpContext("/SignIn", "POST", IPAddress.Parse("192.0.2.10"));
        context.Request.ContentType = "application/x-www-form-urlencoded";
        context.Request.Form = new FormCollection(new Dictionary<string, StringValues>
        {
            ["Username"] = "Admin"
        });

        var partition = policy.GetPartition(context);

        Assert.Equal("signin|192.0.2.10|admin", partition.PartitionKey);
    }

    [Fact]
    public void GetPartition_CombinesTotpStepClientIpAndAuthenticatedAccount()
    {
        var policy = CreatePolicy(new LocalAccountRateLimitOptions());
        var context = CreateHttpContext("/VerifyAuthenticator", "POST", IPAddress.Parse("192.0.2.10"));
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Name, "Admin")
        ], "TestAuth"));

        var partition = policy.GetPartition(context);

        Assert.Equal("totp|192.0.2.10|admin", partition.PartitionKey);
    }

    [Fact]
    public void GetPartition_WhenEnabled_AppliesConfiguredFixedWindowLimit()
    {
        var policy = CreatePolicy(new LocalAccountRateLimitOptions
        {
            Enabled = true,
            PermitLimit = 1,
            WindowMinutes = 1
        });
        var context = CreateHttpContext("/SignIn", "POST", IPAddress.Parse("192.0.2.10"));
        var partition = policy.GetPartition(context);
        using var limiter = partition.Factory(partition.PartitionKey);

        using var firstLease = limiter.AttemptAcquire();
        using var secondLease = limiter.AttemptAcquire();

        Assert.True(firstLease.IsAcquired);
        Assert.False(secondLease.IsAcquired);
    }

    [Fact]
    public void GetPartition_DefaultOptionsAllowTenAttemptsPerWindow()
    {
        var policy = CreatePolicy(new LocalAccountRateLimitOptions());
        var context = CreateHttpContext("/SignIn", "POST", IPAddress.Parse("192.0.2.10"));
        var partition = policy.GetPartition(context);
        using var limiter = partition.Factory(partition.PartitionKey);

        var leases = Enumerable.Range(0, 11)
            .Select(_ => limiter.AttemptAcquire())
            .ToArray();

        try
        {
            Assert.All(leases.Take(10), lease => Assert.True(lease.IsAcquired));
            Assert.False(leases[10].IsAcquired);
        }
        finally
        {
            foreach (var lease in leases)
            {
                lease.Dispose();
            }
        }
    }

    [Fact]
    public void GetPartition_WhenDisabled_AllowsRequests()
    {
        var policy = CreatePolicy(new LocalAccountRateLimitOptions
        {
            Enabled = false,
            PermitLimit = 1,
            WindowMinutes = 1
        });
        var context = CreateHttpContext("/SignIn", "POST", IPAddress.Parse("192.0.2.10"));
        var partition = policy.GetPartition(context);
        using var limiter = partition.Factory(partition.PartitionKey);

        using var firstLease = limiter.AttemptAcquire();
        using var secondLease = limiter.AttemptAcquire();

        Assert.True(firstLease.IsAcquired);
        Assert.True(secondLease.IsAcquired);
    }

    [Fact]
    public void GetPartition_WhenRequestIsGet_AllowsRequests()
    {
        var policy = CreatePolicy(new LocalAccountRateLimitOptions
        {
            Enabled = true,
            PermitLimit = 1,
            WindowMinutes = 1
        });
        var context = CreateHttpContext("/SignIn", "GET", IPAddress.Parse("192.0.2.10"));
        var partition = policy.GetPartition(context);
        using var limiter = partition.Factory(partition.PartitionKey);

        using var firstLease = limiter.AttemptAcquire();
        using var secondLease = limiter.AttemptAcquire();

        Assert.True(firstLease.IsAcquired);
        Assert.True(secondLease.IsAcquired);
    }

    [Theory]
    [InlineData(typeof(SignInModel))]
    [InlineData(typeof(SetupAuthenticatorModel))]
    [InlineData(typeof(VerifyAuthenticatorModel))]
    public void LocalAccountPages_UseLocalAccountRateLimitPolicy(Type pageModelType)
    {
        var attribute = pageModelType.GetCustomAttributes(typeof(EnableRateLimitingAttribute), inherit: false)
            .OfType<EnableRateLimitingAttribute>()
            .SingleOrDefault();

        Assert.NotNull(attribute);
        Assert.Equal(LocalAccountRateLimitPolicy.PolicyName, attribute!.PolicyName);
    }

    private static LocalAccountRateLimitPolicy CreatePolicy(LocalAccountRateLimitOptions options)
    {
        var optionsMonitor = new Mock<IOptionsMonitor<LocalAccountRateLimitOptions>>();
        optionsMonitor.SetupGet(x => x.CurrentValue).Returns(options);

        return new LocalAccountRateLimitPolicy(optionsMonitor.Object);
    }

    private static DefaultHttpContext CreateHttpContext(string path, string method, IPAddress remoteIpAddress)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Request.Method = method;
        context.Connection.RemoteIpAddress = remoteIpAddress;

        return context;
    }
}
