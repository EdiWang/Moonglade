using Microsoft.AspNetCore.Http;
using System.Net;

namespace Moonglade.Utils.Tests;

public class ClientIPHelperTests
{
    #region GetClientIP Tests

    [Fact]
    public void GetClientIP_WithNullContext_ReturnsNull()
    {
        // Act
        var result = ClientIPHelper.GetClientIP(null!);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetClientIP_WithNullConnection_ReturnsNull()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = null;

        // Act
        var result = ClientIPHelper.GetClientIP(context);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetClientIP_WithValidRemoteIpAddress_ReturnsIpAddress()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.1"); // Public IP

        // Act
        var result = ClientIPHelper.GetClientIP(context);

        // Assert
        Assert.Equal("203.0.113.1", result);
    }

    [Fact]
    public void GetClientIP_WithPrivateRemoteIpAddress_ReturnsPrivateIp()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("192.168.1.100"); // Private IP

        // Act
        var result = ClientIPHelper.GetClientIP(context);

        // Assert
        Assert.Equal("192.168.1.100", result);
    }

    [Theory]
    [InlineData("X-Azure-ClientIP", "203.0.113.1")]
    [InlineData("CF-Connecting-IP", "203.0.113.2")]
    [InlineData("X-Forwarded-For", "203.0.113.3")]
    [InlineData("X-Real-IP", "203.0.113.4")]
    [InlineData("X-Client-IP", "203.0.113.5")]
    [InlineData("True-Client-IP", "203.0.113.6")]
    [InlineData("HTTP_X_FORWARDED_FOR", "203.0.113.7")]
    [InlineData("HTTP_CLIENT_IP", "203.0.113.8")]
    public void GetClientIP_WithForwardedHeaders_ReturnsHeaderValue(string headerName, string expectedIp)
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("192.168.1.1");
        context.Request.Headers[headerName] = expectedIp;

        // Act
        var result = ClientIPHelper.GetClientIP(context);

        // Assert
        Assert.Equal(expectedIp, result);
    }

    [Fact]
    public void GetClientIP_WithMultipleForwardedIPs_ReturnsFirstValidPublicIP()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("192.168.1.1");
        context.Request.Headers["X-Forwarded-For"] = "192.168.1.100, 203.0.113.1, 198.51.100.1";

        // Act
        var result = ClientIPHelper.GetClientIP(context);

        // Assert
        Assert.Equal("203.0.113.1", result);
    }

    [Fact]
    public void GetClientIP_WithAllPrivateForwardedIPs_ReturnsRemoteIp()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.1");
        context.Request.Headers["X-Forwarded-For"] = "192.168.1.100, 10.0.0.1, 172.16.0.1";

        // Act
        var result = ClientIPHelper.GetClientIP(context);

        // Assert
        Assert.Equal("203.0.113.1", result);
    }

    [Fact]
    public void GetClientIP_WithHeaderPrecedence_ReturnsFirstHeaderWithValidIP()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("192.168.1.1");
        context.Request.Headers["CF-Connecting-IP"] = "203.0.113.1";
        context.Request.Headers["X-Azure-ClientIP"] = "203.0.113.2";

        // Act
        var result = ClientIPHelper.GetClientIP(context);

        // Assert
        Assert.Equal("203.0.113.2", result); // X-Azure-ClientIP has higher precedence
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void GetClientIP_WithEmptyOrWhitespaceHeader_SkipsToNextHeader(string headerValue)
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.1");
        context.Request.Headers["X-Azure-ClientIP"] = headerValue;
        context.Request.Headers["CF-Connecting-IP"] = "203.0.113.2";

        // Act
        var result = ClientIPHelper.GetClientIP(context);

        // Assert
        Assert.Equal("203.0.113.2", result);
    }

    [Fact]
    public void GetClientIP_WithInvalidIPInHeader_SkipsToNextHeader()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.1");
        context.Request.Headers["X-Azure-ClientIP"] = "invalid-ip";
        context.Request.Headers["CF-Connecting-IP"] = "203.0.113.2";

        // Act
        var result = ClientIPHelper.GetClientIP(context);

        // Assert
        Assert.Equal("203.0.113.2", result);
    }

    [Fact]
    public void GetClientIP_WithIPv6Address_HandlesCorrectly()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("2001:db8::1"); // Public IPv6
        context.Request.Headers["X-Forwarded-For"] = "2001:db8::2";

        // Act
        var result = ClientIPHelper.GetClientIP(context);

        // Assert
        Assert.Equal("2001:db8::2", result);
    }

    [Fact]
    public void GetClientIP_WithLoopbackIPv6_ReturnsLoopbackAddress()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.IPv6Loopback;

        // Act
        var result = ClientIPHelper.GetClientIP(context);

        // Assert
        Assert.Equal("::1", result);
    }

    #endregion

    #region IsValidPublicIP Tests

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("\t", false)]
    [InlineData("\n", false)]
    public void IsValidPublicIP_WithNullOrWhitespaceIP_ReturnsFalse(string ipAddress, bool expected)
    {
        // Act
        var result = CallIsValidPublicIPMethod(ipAddress);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("invalid-ip", false)]
    [InlineData("256.256.256.256", false)]
    [InlineData("192.168.1.1.1", false)]
    [InlineData("not.an.ip.address", false)]
    public void IsValidPublicIP_WithInvalidIPFormat_ReturnsFalse(string ipAddress, bool expected)
    {
        // Act
        var result = CallIsValidPublicIPMethod(ipAddress);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("8.8.8.8", true)]           // Google DNS
    [InlineData("1.1.1.1", true)]           // Cloudflare DNS
    [InlineData("203.0.113.1", true)]       // TEST-NET-3
    [InlineData("198.51.100.1", true)]      // TEST-NET-2
    [InlineData("173.252.74.22", true)]     // Facebook IP range
    public void IsValidPublicIP_WithValidPublicIPv4_ReturnsTrue(string ipAddress, bool expected)
    {
        // Act
        var result = CallIsValidPublicIPMethod(ipAddress);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("192.168.1.1", false)]      // Private range
    [InlineData("192.168.0.255", false)]    // Private range
    [InlineData("10.0.0.1", false)]         // Private range
    [InlineData("10.255.255.255", false)]   // Private range
    [InlineData("172.16.0.1", false)]       // Private range
    [InlineData("172.31.255.255", false)]   // Private range
    [InlineData("127.0.0.1", false)]        // Loopback
    [InlineData("127.255.255.255", false)]  // Loopback
    [InlineData("169.254.1.1", false)]      // Link-local
    [InlineData("0.0.0.0", false)]          // Any address
    public void IsValidPublicIP_WithPrivateOrSpecialIPv4_ReturnsFalse(string ipAddress, bool expected)
    {
        // Act
        var result = CallIsValidPublicIPMethod(ipAddress);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("2001:db8::1", true)]       // Documentation range (but treated as public for testing)
    [InlineData("2001:4860:4860::8888", true)] // Google DNS IPv6
    public void IsValidPublicIP_WithValidPublicIPv6_ReturnsTrue(string ipAddress, bool expected)
    {
        // Act
        var result = CallIsValidPublicIPMethod(ipAddress);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("::1", false)]              // Loopback
    [InlineData("::", false)]               // Any
    [InlineData("fe80::1", false)]          // Link-local
    [InlineData("fec0::1", false)]          // Site-local
    [InlineData("ff00::1", false)]          // Multicast
    public void IsValidPublicIP_WithPrivateOrSpecialIPv6_ReturnsFalse(string ipAddress, bool expected)
    {
        // Act
        var result = CallIsValidPublicIPMethod(ipAddress);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("172.15.255.255", true)]    // Just outside private range
    [InlineData("172.32.0.0", true)]        // Just outside private range
    [InlineData("172.16.0.0", false)]       // Start of private range
    [InlineData("172.31.255.255", false)]   // End of private range
    [InlineData("172.20.128.1", false)]     // Middle of private range
    public void IsValidPublicIP_WithClass172BoundaryIPs_ReturnsCorrectResult(string ipAddress, bool expected)
    {
        // Act
        var result = CallIsValidPublicIPMethod(ipAddress);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void GetClientIP_WithComplexScenario_ReturnsExpectedResult()
    {
        // Arrange - Simulate a complex proxy scenario
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("192.168.1.1"); // Private proxy IP
        context.Request.Headers["X-Forwarded-For"] = "10.0.0.1, 192.168.100.50, 203.0.113.195"; // Mixed IPs
        context.Request.Headers["CF-Connecting-IP"] = "198.51.100.25"; // Valid public IP
        context.Request.Headers["X-Azure-ClientIP"] = ""; // Empty header

        // Act
        var result = ClientIPHelper.GetClientIP(context);

        // Assert
        Assert.Equal("198.51.100.25", result); // Should return first valid public IP from CF-Connecting-IP
    }

    [Fact]
    public void GetClientIP_WithOnlyPrivateIPs_ReturnsRemoteIP()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.100");
        context.Request.Headers["X-Forwarded-For"] = "192.168.1.1, 172.16.0.1";

        // Act
        var result = ClientIPHelper.GetClientIP(context);

        // Assert
        Assert.Equal("10.0.0.100", result); // Falls back to remote IP even if private
    }

    [Fact]
    public void GetClientIP_WithMixedIPv4AndIPv6_HandlesCorrectly()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("::1");
        context.Request.Headers["X-Forwarded-For"] = "192.168.1.1, 2001:db8::1";

        // Act
        var result = ClientIPHelper.GetClientIP(context);

        // Assert
        Assert.Equal("2001:db8::1", result);
    }

    [Fact]
    public void GetClientIP_WithCommaAndSpaceSeparatedIPs_ParsesCorrectly()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");
        context.Request.Headers["X-Forwarded-For"] = "  192.168.1.1  ,  203.0.113.1  ,  198.51.100.1  ";

        // Act
        var result = ClientIPHelper.GetClientIP(context);

        // Assert
        Assert.Equal("203.0.113.1", result);
    }

    [Fact]
    public void GetClientIP_WithAllHeadersEmpty_ReturnsRemoteIP()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.1");

        // Add empty headers
        foreach (var header in new[] { "X-Azure-ClientIP", "CF-Connecting-IP", "X-Forwarded-For",
                                     "X-Real-IP", "X-Client-IP", "True-Client-IP",
                                     "HTTP_X_FORWARDED_FOR", "HTTP_CLIENT_IP" })
        {
            context.Request.Headers[header] = "";
        }

        // Act
        var result = ClientIPHelper.GetClientIP(context);

        // Assert
        Assert.Equal("203.0.113.1", result);
    }

    [Fact]
    public void GetClientIP_WithHeaderContainingOnlyInvalidIPs_FallsBackToRemoteIP()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.1");
        context.Request.Headers["X-Forwarded-For"] = "invalid-ip, 256.256.256.256, not-an-ip";

        // Act
        var result = ClientIPHelper.GetClientIP(context);

        // Assert
        Assert.Equal("203.0.113.1", result);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Helper method to access the private IsValidPublicIP method via reflection
    /// </summary>
    private static bool CallIsValidPublicIPMethod(string ipAddress)
    {
        var method = typeof(ClientIPHelper).GetMethod("IsValidPublicIP",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        return (bool)method!.Invoke(null, new object[] { ipAddress! })!;
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void GetClientIP_WithLoopbackRemoteIP_ReturnsLoopbackIP()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Loopback; // 127.0.0.1

        // Act
        var result = ClientIPHelper.GetClientIP(context);

        // Assert
        Assert.Equal("127.0.0.1", result);
    }

    [Fact]
    public void GetClientIP_WithAnyIPv4Address_ReturnsAnyAddress()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Any; // 0.0.0.0

        // Act
        var result = ClientIPHelper.GetClientIP(context);

        // Assert
        Assert.Equal("0.0.0.0", result);
    }

    [Fact]
    public void GetClientIP_WithAnyIPv6Address_ReturnsAnyIPv6Address()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.IPv6Any; // ::

        // Act
        var result = ClientIPHelper.GetClientIP(context);

        // Assert
        Assert.Equal("::", result);
    }

    [Theory]
    [InlineData("X-Forwarded-For")]
    [InlineData("X-Real-IP")]
    [InlineData("CF-Connecting-IP")]
    public void GetClientIP_WithSingleValidIPInHeader_ReturnsHeaderIP(string headerName)
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("192.168.1.1");
        context.Request.Headers[headerName] = "203.0.113.100";

        // Act
        var result = ClientIPHelper.GetClientIP(context);

        // Assert
        Assert.Equal("203.0.113.100", result);
    }

    #endregion
}