using System.Net;

namespace Moonglade.Webmention.Tests;

public class WebmentionUrlSafetyValidatorTests
{
    [Theory]
    [InlineData("http://127.0.0.1/source")]
    [InlineData("http://10.0.0.1/source")]
    [InlineData("http://172.16.0.1/source")]
    [InlineData("http://192.168.1.10/source")]
    [InlineData("http://169.254.1.10/source")]
    [InlineData("http://100.64.0.1/source")]
    [InlineData("http://198.18.0.1/source")]
    [InlineData("http://192.0.2.10/source")]
    [InlineData("http://[::1]/source")]
    [InlineData("http://[fe80::1]/source")]
    [InlineData("http://[fc00::1]/source")]
    [InlineData("http://[2001:db8::1]/source")]
    [InlineData("http://[::ffff:127.0.0.1]/source")]
    public async Task IsSafeSourceAsync_UnsafeLiteralAddress_ReturnsFalse(string sourceUrl)
    {
        var validator = new WebmentionUrlSafetyValidator(new StubDnsResolver());

        var result = await validator.IsSafeSourceAsync(new Uri(sourceUrl), TestContext.Current.CancellationToken);

        Assert.False(result);
    }

    [Theory]
    [InlineData("http://8.8.8.8/source")]
    [InlineData("https://[2001:4860:4860::8888]/source")]
    public async Task IsSafeSourceAsync_PublicLiteralAddress_ReturnsTrue(string sourceUrl)
    {
        var validator = new WebmentionUrlSafetyValidator(new StubDnsResolver());

        var result = await validator.IsSafeSourceAsync(new Uri(sourceUrl), TestContext.Current.CancellationToken);

        Assert.True(result);
    }

    [Fact]
    public async Task IsSafeSourceAsync_DnsResolvesToPrivateAddress_ReturnsFalse()
    {
        var validator = new WebmentionUrlSafetyValidator(new StubDnsResolver(
            IPAddress.Parse("8.8.8.8"),
            IPAddress.Parse("10.0.0.5")));

        var result = await validator.IsSafeSourceAsync(new Uri("https://source.example/post"), TestContext.Current.CancellationToken);

        Assert.False(result);
    }

    [Fact]
    public async Task IsSafeSourceAsync_DnsResolvesToPublicAddresses_ReturnsTrue()
    {
        var validator = new WebmentionUrlSafetyValidator(new StubDnsResolver(
            IPAddress.Parse("8.8.8.8"),
            IPAddress.Parse("2001:4860:4860::8888")));

        var result = await validator.IsSafeSourceAsync(new Uri("https://source.example/post"), TestContext.Current.CancellationToken);

        Assert.True(result);
    }

    [Fact]
    public async Task IsSafeSourceAsync_UnsupportedScheme_ReturnsFalseWithoutDnsLookup()
    {
        var dnsResolver = new StubDnsResolver(IPAddress.Parse("8.8.8.8"));
        var validator = new WebmentionUrlSafetyValidator(dnsResolver);

        var result = await validator.IsSafeSourceAsync(new Uri("ftp://source.example/post"), TestContext.Current.CancellationToken);

        Assert.False(result);
        Assert.Equal(0, dnsResolver.LookupCount);
    }

    private sealed class StubDnsResolver(params IPAddress[] addresses) : IWebmentionDnsResolver
    {
        public int LookupCount { get; private set; }

        public Task<IPAddress[]> GetHostAddressesAsync(string host, CancellationToken cancellationToken)
        {
            LookupCount++;
            return Task.FromResult(addresses);
        }
    }
}
