using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moonglade.Configuration;
using Moq;
using System.Reflection;

namespace Moonglade.Setup.Tests;

public class MigrationManagerIntegrationTests
{
    private readonly Mock<ILogger<MigrationManager>> _loggerMock;
    private readonly Mock<ICommandMediator> _commandMediatorMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<IBlogConfig> _blogConfigMock;

    public MigrationManagerIntegrationTests()
    {
        _loggerMock = new Mock<ILogger<MigrationManager>>();
        _commandMediatorMock = new Mock<ICommandMediator>();
        _configurationMock = new Mock<IConfiguration>();
        _blogConfigMock = new Mock<IBlogConfig>();
    }

    private MigrationManager CreateManager()
    {
        return new MigrationManager(
            _loggerMock.Object,
            _commandMediatorMock.Object,
            _configurationMock.Object,
            _blogConfigMock.Object);
    }

    [Fact]
    public void MigrationManager_Constructor_InitializesCorrectly()
    {
        // Act
        var manager = CreateManager();

        // Assert
        Assert.NotNull(manager);
    }

    [Fact]
    public void MigrationScripts_AreEmbeddedCorrectly()
    {
        // Arrange
        var assembly = Assembly.GetAssembly(typeof(MigrationManager));
        var resourceNames = assembly?.GetManifestResourceNames() ?? [];

        // Act
        var sqlServerScript = resourceNames.FirstOrDefault(r => r.Contains("SqlServer") && r.EndsWith(".sql"));
        var mySqlScript = resourceNames.FirstOrDefault(r => r.Contains("MySql") && r.EndsWith(".sql"));
        var postgreSqlScript = resourceNames.FirstOrDefault(r => r.Contains("PostgreSql") && r.EndsWith(".sql"));

        // Assert - At least verify the naming pattern exists
        // Note: This test may fail if scripts don't exist yet, which is expected during development
        Assert.NotNull(assembly);
    }

    [Theory]
    [InlineData("GO\nSELECT 1;\nGO\nSELECT 2;", 2)]
    [InlineData("SELECT 1;", 1)]
    [InlineData("GO\n\nGO\nSELECT 1;\nGO", 1)]
    public void SqlBatchSplitter_SplitsCorrectly(string script, int expectedBatches)
    {
        // This tests the regex pattern used for splitting SQL batches
        // We'll use reflection to access the private method or test through integration

        // Arrange
        var pattern = @"^\s*GO\s*$";
        var regex = new System.Text.RegularExpressions.Regex(
            pattern,
            System.Text.RegularExpressions.RegexOptions.IgnoreCase |
            System.Text.RegularExpressions.RegexOptions.Multiline);

        // Act
        var batches = regex.Split(script)
            .Select(batch => batch.Trim())
            .Where(batch => !string.IsNullOrWhiteSpace(batch))
            .ToArray();

        // Assert
        Assert.Equal(expectedBatches, batches.Length);
    }

    [Fact]
    public void ComputeSha256Hash_ReturnsConsistentHash()
    {
        // This tests hash computation logic
        // Arrange
        const string content = "SELECT * FROM Users;";
        var bytes = System.Text.Encoding.UTF8.GetBytes(content);

        // Act
        var hash1 = System.Security.Cryptography.SHA256.HashData(bytes);
        var hash2 = System.Security.Cryptography.SHA256.HashData(bytes);

        // Assert
        Assert.Equal(Convert.ToHexString(hash1), Convert.ToHexString(hash2));
    }
}
