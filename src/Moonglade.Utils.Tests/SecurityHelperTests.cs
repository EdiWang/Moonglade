namespace Moonglade.Utils.Tests;

public class SecurityHelperTests
{
    #region HashPassword Tests

    [Fact]
    public void HashPassword_WithValidPasswordAndSalt_ReturnsHashedPassword()
    {
        // Arrange
        const string password = "testPassword123";
        const string salt = "dGVzdFNhbHQ="; // base64 encoded "testSalt"

        // Act
        var result = SecurityHelper.HashPassword(password, salt);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.NotEqual(password, result);
    }

    [Fact]
    public void HashPassword_WithSamePasswordAndSalt_ReturnsConsistentHash()
    {
        // Arrange
        const string password = "testPassword123";
        const string salt = "dGVzdFNhbHQ=";

        // Act
        var result1 = SecurityHelper.HashPassword(password, salt);
        var result2 = SecurityHelper.HashPassword(password, salt);

        // Assert
        Assert.Equal(result1, result2);
    }

    [Fact]
    public void HashPassword_WithDifferentPasswords_ReturnsDifferentHashes()
    {
        // Arrange
        const string password1 = "password1";
        const string password2 = "password2";
        const string salt = "dGVzdFNhbHQ=";

        // Act
        var result1 = SecurityHelper.HashPassword(password1, salt);
        var result2 = SecurityHelper.HashPassword(password2, salt);

        // Assert
        Assert.NotEqual(result1, result2);
    }

    [Fact]
    public void HashPassword_WithDifferentSalts_ReturnsDifferentHashes()
    {
        // Arrange
        const string password = "testPassword";
        const string salt1 = "c2FsdDE="; // base64 encoded "salt1"
        const string salt2 = "c2FsdDI="; // base64 encoded "salt2"

        // Act
        var result1 = SecurityHelper.HashPassword(password, salt1);
        var result2 = SecurityHelper.HashPassword(password, salt2);

        // Assert
        Assert.NotEqual(result1, result2);
    }

    [Fact]
    public void HashPassword_ReturnsBase64EncodedString()
    {
        // Arrange
        const string password = "testPassword";
        const string salt = "dGVzdFNhbHQ=";

        // Act
        var result = SecurityHelper.HashPassword(password, salt);

        // Assert
        Exception ex = Record.Exception(() => Convert.FromBase64String(salt));
        Assert.Null(ex);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("  ")]
    public void HashPassword_WithEmptyOrWhitespacePassword_HandlesGracefully(string password)
    {
        // Arrange
        const string salt = "dGVzdFNhbHQ=";

        // Act & Assert
        var result = SecurityHelper.HashPassword(password, salt);
        Assert.NotNull(result);
    }

    [Fact]
    public void HashPassword_WithInvalidBase64Salt_ThrowsFormatException()
    {
        // Arrange
        const string password = "testPassword";
        const string invalidSalt = "invalid!base64!";

        // Act & Assert
        Assert.Throws<FormatException>(() => SecurityHelper.HashPassword(password, invalidSalt));
    }

    #endregion

    #region GenerateSalt Tests

    [Fact]
    public void GenerateSalt_ReturnsNonEmptyString()
    {
        // Act
        var salt = SecurityHelper.GenerateSalt();

        // Assert
        Assert.NotNull(salt);
        Assert.NotEmpty(salt);
    }

    [Fact]
    public void GenerateSalt_ReturnsValidBase64String()
    {
        // Act
        var salt = SecurityHelper.GenerateSalt();

        // Assert
        Exception ex = Record.Exception(() => Convert.FromBase64String(salt));
        Assert.Null(ex);
    }

    [Fact]
    public void GenerateSalt_ReturnsCorrectByteLength()
    {
        // Act
        var salt = SecurityHelper.GenerateSalt();
        var bytes = Convert.FromBase64String(salt);

        // Assert
        Assert.Equal(16, bytes.Length); // 128 bits / 8 = 16 bytes
    }

    [Fact]
    public void GenerateSalt_ReturnsDifferentValuesOnMultipleCalls()
    {
        // Act
        var salt1 = SecurityHelper.GenerateSalt();
        var salt2 = SecurityHelper.GenerateSalt();
        var salt3 = SecurityHelper.GenerateSalt();

        // Assert
        Assert.NotEqual(salt1, salt2);
        Assert.NotEqual(salt2, salt3);
        Assert.NotEqual(salt1, salt3);
    }

    [Fact]
    public void GenerateSalt_GeneratesHighEntropyValues()
    {
        // Arrange
        var salts = new HashSet<string>();

        // Act - Generate 100 salts
        for (int i = 0; i < 100; i++)
        {
            salts.Add(SecurityHelper.GenerateSalt());
        }

        // Assert - All should be unique
        Assert.Equal(100, salts.Count);
    }

    #endregion

    #region SterilizeLink Tests

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void SterilizeLink_WithNullOrWhitespaceInput_ReturnsHash(string input)
    {
        // Act
        var result = SecurityHelper.SterilizeLink(input!);

        // Assert
        Assert.Equal("#", result);
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/home")]
    [InlineData("/about")]
    [InlineData("/posts/123")]
    [InlineData("/api/v1/data")]
    public void SterilizeLink_WithValidLocalPaths_ReturnsOriginalPath(string path)
    {
        // Act
        var result = SecurityHelper.SterilizeLink(path);

        // Assert
        Assert.Equal(path, result);
    }

    [Theory]
    [InlineData("//")]
    [InlineData("//evil.com")]
    [InlineData("/\\")]
    [InlineData("/\\evil.com")]
    [InlineData("//\\evil.com")]
    public void SterilizeLink_WithMaliciousLocalPaths_ReturnsHash(string maliciousPath)
    {
        // Act
        var result = SecurityHelper.SterilizeLink(maliciousPath);

        // Assert
        Assert.Equal("#", result);
    }

    [Theory]
    [InlineData("https://example.com")]
    [InlineData("http://example.com")]
    [InlineData("https://subdomain.example.com/path")]
    [InlineData("https://example.com:8080/api")]
    public void SterilizeLink_WithValidExternalUrls_ReturnsOriginalUrl(string url)
    {
        // Act
        var result = SecurityHelper.SterilizeLink(url);

        // Assert
        Assert.Equal(url, result);
    }

    [Theory]
    [InlineData("http://localhost")]
    [InlineData("https://localhost:8080")]
    [InlineData("http://127.0.0.1")]
    [InlineData("https://127.0.0.1:443")]
    public void SterilizeLink_WithLoopbackUrls_ReturnsHash(string loopbackUrl)
    {
        // Act
        var result = SecurityHelper.SterilizeLink(loopbackUrl);

        // Assert
        Assert.Equal("#", result);
    }

    [Theory]
    [InlineData("http://192.168.1.1")]
    [InlineData("https://192.168.0.100")]
    [InlineData("http://10.0.0.1")]
    [InlineData("https://10.255.255.255")]
    [InlineData("http://172.16.0.1")]
    [InlineData("https://172.31.255.255")]
    public void SterilizeLink_WithPrivateIpUrls_ReturnsHash(string privateIpUrl)
    {
        // Act
        var result = SecurityHelper.SterilizeLink(privateIpUrl);

        // Assert
        Assert.Equal("#", result);
    }

    [Theory]
    [InlineData("invalid-url")]
    [InlineData("not a url at all")]
    [InlineData("ftp://example.com")]
    [InlineData("javascript:alert('xss')")]
    public void SterilizeLink_WithInvalidUrls_ReturnsHash(string invalidUrl)
    {
        // Act
        var result = SecurityHelper.SterilizeLink(invalidUrl);

        // Assert
        Assert.Equal("#", result);
    }

    [Theory]
    [InlineData("http://8.8.8.8")]
    [InlineData("https://1.1.1.1")]
    [InlineData("http://173.252.74.22")] // Facebook IP
    public void SterilizeLink_WithPublicIpUrls_ReturnsOriginalUrl(string publicIpUrl)
    {
        // Act
        var result = SecurityHelper.SterilizeLink(publicIpUrl);

        // Assert
        Assert.Equal(publicIpUrl, result);
    }

    #endregion

    #region IsPrivateIP Tests

    [Theory]
    [InlineData("192.168.1.1", true)]
    [InlineData("192.168.0.255", true)]
    [InlineData("192.168.255.255", true)]
    [InlineData("10.0.0.1", true)]
    [InlineData("10.255.255.255", true)]
    [InlineData("127.0.0.1", true)]
    [InlineData("127.255.255.255", true)]
    [InlineData("172.16.0.1", true)]
    [InlineData("172.31.255.255", true)]
    public void IsPrivateIP_WithPrivateIPs_ReturnsTrue(string ip, bool expected)
    {
        // Act
        var result = SecurityHelper.IsPrivateIP(ip);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("8.8.8.8", false)]
    [InlineData("1.1.1.1", false)]
    [InlineData("173.252.74.22", false)] // Facebook
    [InlineData("151.101.193.140", false)] // Reddit
    [InlineData("172.15.255.255", false)] // Just outside private range
    [InlineData("172.32.0.1", false)] // Just outside private range
    [InlineData("193.168.1.1", false)] // Close to 192.168 but public
    [InlineData("11.0.0.1", false)] // Close to 10.x but public
    public void IsPrivateIP_WithPublicIPs_ReturnsFalse(string ip, bool expected)
    {
        // Act
        var result = SecurityHelper.IsPrivateIP(ip);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("172.16.0.0")]
    [InlineData("172.16.255.255")]
    [InlineData("172.20.0.0")]
    [InlineData("172.25.128.1")]
    [InlineData("172.31.0.0")]
    [InlineData("172.31.255.255")]
    public void IsPrivateIP_WithClass172PrivateRange_ReturnsTrue(string ip)
    {
        // Act
        var result = SecurityHelper.IsPrivateIP(ip);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData("172.15.255.255")]
    [InlineData("172.32.0.0")]
    [InlineData("172.0.0.1")]
    [InlineData("172.50.0.1")]
    public void IsPrivateIP_WithClass172PublicRange_ReturnsFalse(string ip)
    {
        // Act
        var result = SecurityHelper.IsPrivateIP(ip);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("invalid.ip")]
    [InlineData("256.256.256.256")]
    [InlineData("")]
    [InlineData("192.168.1.1.1")]
    public void IsPrivateIP_WithInvalidIPFormats_ThrowsFormatException(string invalidIp)
    {
        // Act & Assert
        Assert.Throws<FormatException>(() => SecurityHelper.IsPrivateIP(invalidIp));
    }

    [Fact]
    public void IsPrivateIP_WithNullInput_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => SecurityHelper.IsPrivateIP(null!));
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void SecurityHelper_PasswordHashingWorkflow_WorksCorrectly()
    {
        // Arrange
        const string originalPassword = "MySecurePassword123!";

        // Act - Generate salt and hash password
        var salt = SecurityHelper.GenerateSalt();
        var hashedPassword = SecurityHelper.HashPassword(originalPassword, salt);

        // Verify the same password with same salt produces same hash
        var verificationHash = SecurityHelper.HashPassword(originalPassword, salt);

        // Assert
        Assert.Equal(hashedPassword, verificationHash);
        Assert.NotEqual(originalPassword, hashedPassword);
    }

    [Fact]
    public void SecurityHelper_LinkSterilization_HandlesComplexScenarios()
    {
        // Arrange & Act & Assert
        Assert.Equal("https://example.com/safe", SecurityHelper.SterilizeLink("https://example.com/safe"));
        Assert.Equal("#", SecurityHelper.SterilizeLink("http://192.168.1.1/malicious"));
        Assert.Equal("/safe/local/path", SecurityHelper.SterilizeLink("/safe/local/path"));
        Assert.Equal("#", SecurityHelper.SterilizeLink("//malicious.com"));
        Assert.Equal("#", SecurityHelper.SterilizeLink("javascript:alert('xss')"));
    }

    [Theory]
    [InlineData("192.168.1.100")]
    [InlineData("10.0.0.50")]
    [InlineData("172.20.5.1")]
    [InlineData("127.0.0.1")]
    public void SecurityHelper_PrivateIPDetection_IntegratesWithSterilizeLink(string privateIp)
    {
        // Arrange
        var url = $"http://{privateIp}/test";

        // Act
        var sterilizedResult = SecurityHelper.SterilizeLink(url);
        var isPrivateResult = SecurityHelper.IsPrivateIP(privateIp);

        // Assert
        Assert.True(isPrivateResult);
        Assert.Equal("#", sterilizedResult);
    }

    #endregion
}