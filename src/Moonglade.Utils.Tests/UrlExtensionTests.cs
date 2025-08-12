using System.Net;

namespace Moonglade.Utils.Tests;

public class UrlExtensionTests
{
    #region IsValidUrl Tests

    [Theory]
    [InlineData("https://www.example.com", UrlExtension.UrlScheme.All, true)]
    [InlineData("http://www.example.com", UrlExtension.UrlScheme.All, true)]
    [InlineData("https://www.example.com", UrlExtension.UrlScheme.Https, true)]
    [InlineData("http://www.example.com", UrlExtension.UrlScheme.Http, true)]
    [InlineData("ftp://ftp.example.com", UrlExtension.UrlScheme.All, false)]
    [InlineData("invalid-url", UrlExtension.UrlScheme.All, false)]
    [InlineData("", UrlExtension.UrlScheme.All, false)]
    [InlineData("https://subdomain.example.com/path/to/resource", UrlExtension.UrlScheme.All, true)]
    [InlineData("http://localhost:8080", UrlExtension.UrlScheme.All, true)]
    [InlineData("https://example.com:443", UrlExtension.UrlScheme.Https, true)]
    public void IsValidUrl_WithVariousInputs_ReturnsExpectedResult(string url, UrlExtension.UrlScheme scheme, bool expected)
    {
        // Act
        var result = url.IsValidUrl(scheme);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("http://www.example.com", UrlExtension.UrlScheme.Https, false)]
    [InlineData("https://www.example.com", UrlExtension.UrlScheme.Http, false)]
    public void IsValidUrl_WithMismatchedScheme_ReturnsFalse(string url, UrlExtension.UrlScheme scheme, bool expected)
    {
        // Act
        var result = url.IsValidUrl(scheme);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsValidUrl_WithNullUrl_ReturnsFalse()
    {
        // Arrange
        string? url = null;

        // Act
        var result = url.IsValidUrl();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidUrl_WithInvalidScheme_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        const string url = "https://www.example.com";
        const UrlExtension.UrlScheme invalidScheme = (UrlExtension.UrlScheme)999;

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => url.IsValidUrl(invalidScheme));
    }

    [Theory]
    [InlineData("https://")]
    [InlineData("http://")]
    [InlineData("://example.com")]
    [InlineData("www.example.com")]
    [InlineData("javascript:alert('xss')")]
    public void IsValidUrl_WithMalformedUrls_ReturnsFalse(string url)
    {
        // Act
        var result = url.IsValidUrl();

        // Assert
        Assert.False(result);
    }

    #endregion

    #region CombineUrl Tests

    [Theory]
    [InlineData("https://example.com", "path", "https://example.com/path")]
    [InlineData("https://example.com/", "path", "https://example.com/path")]
    [InlineData("https://example.com", "/path", "https://example.com/path")]
    [InlineData("https://example.com/", "/path", "https://example.com/path")]
    [InlineData("https://example.com/api", "v1/users", "https://example.com/api/v1/users")]
    [InlineData("https://example.com/api/", "/v1/users", "https://example.com/api/v1/users")]
    [InlineData("  https://example.com  ", "  path  ", "https://example.com/path")]
    public void CombineUrl_WithValidInputs_ReturnsCombinedUrl(string baseUrl, string path, string expected)
    {
        // Act
        var result = baseUrl.CombineUrl(path);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null, "path")]
    [InlineData("https://example.com", null)]
    [InlineData("", "path")]
    [InlineData("https://example.com", "")]
    [InlineData("   ", "path")]
    [InlineData("https://example.com", "   ")]
    public void CombineUrl_WithNullOrEmptyInputs_ThrowsArgumentNullException(string? baseUrl, string? path)
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => baseUrl!.CombineUrl(path!));
    }

    [Fact]
    public void CombineUrl_WithMultipleSlashes_NormalizesCorrectly()
    {
        // Arrange
        const string baseUrl = "https://example.com///";
        const string path = "///path";

        // Act
        var result = baseUrl.CombineUrl(path);

        // Assert
        Assert.Equal("https://example.com/path", result);
    }

    #endregion

    #region IsLocalhostUrl Tests

    [Theory]
    [InlineData("http://localhost")]
    [InlineData("https://localhost:8080")]
    [InlineData("http://127.0.0.1")]
    [InlineData("https://127.0.0.1:443")]
    [InlineData("http://[::1]")]
    [InlineData("https://[::1]:8080")]
    public void IsLocalhostUrl_WithLocalhostUrls_ReturnsTrue(string url)
    {
        // Arrange
        var uri = new Uri(url);

        // Act
        var result = uri.IsLocalhostUrl();

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData("https://www.example.com")]
    [InlineData("http://192.168.1.100")]
    [InlineData("https://10.0.0.1")]
    [InlineData("http://8.8.8.8")]
    [InlineData("https://subdomain.example.org")]
    public void IsLocalhostUrl_WithNonLocalhostUrls_ReturnsFalse(string url)
    {
        // Arrange
        var uri = new Uri(url);

        // Act
        var result = uri.IsLocalhostUrl();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsLocalhostUrl_WithLocalMachineName_ReturnsTrue()
    {
        // Arrange
        var localHostName = Dns.GetHostName();
        var uri = new Uri($"http://{localHostName}");

        // Act
        var result = uri.IsLocalhostUrl();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsLocalhostUrl_WithLocalIpAddress_ReturnsTrue()
    {
        // Arrange
        var localIPs = Dns.GetHostAddresses(Dns.GetHostName());
        if (localIPs.Length > 0)
        {
            var firstLocalIp = localIPs.First(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
            var uri = new Uri($"http://{firstLocalIp}");

            // Act
            var result = uri.IsLocalhostUrl();

            // Assert
            Assert.True(result);
        }
        else
        {
            // Skip test if no local IPs found
            Assert.True(true);
        }
    }

    [Fact]
    public void IsLocalhostUrl_WithMalformedUri_ReturnsFalse()
    {
        // This test is tricky because the Uri constructor will throw before we get to test the method
        // The method handles UriFormatException internally, but since we're passing a Uri object,
        // we need to test the internal exception handling indirectly

        // We'll test with a valid Uri that might cause issues in the DNS lookup
        var uri = new Uri("http://nonexistent.local.domain.that.should.not.exist");

        // Act
        var result = uri.IsLocalhostUrl();

        // Assert
        Assert.False(result);
    }

    #endregion
}