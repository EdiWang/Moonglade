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

    #endregion
}