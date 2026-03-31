using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moonglade.Data;
using Moq;

namespace Moonglade.Configuration.Tests;

public class AddDefaultConfigurationCommandTests
{
    private readonly Mock<ILogger<AddDefaultConfigurationCommandHandler>> _mockLogger = new();

    private static BlogDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<BlogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new BlogDbContext(options);
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_AddsEntityAndReturnsDone()
    {
        // Arrange
        using var db = CreateDbContext();
        var handler = new AddDefaultConfigurationCommandHandler(db, _mockLogger.Object);
        var command = new AddDefaultConfigurationCommand("TestKey", "{\"value\":\"default\"}");

        var beforeAdd = DateTime.UtcNow;

        // Act
        var result = await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        Assert.Equal(OperationCode.Done, result);

        var entity = await db.BlogConfiguration.FirstOrDefaultAsync(c => c.CfgKey == "TestKey");
        Assert.NotNull(entity);
        Assert.Equal("TestKey", entity.CfgKey);
        Assert.Equal("{\"value\":\"default\"}", entity.CfgValue);
        Assert.True(entity.LastModifiedTimeUtc >= beforeAdd);
    }

    [Fact]
    public async Task HandleAsync_ExceptionThrown_RethrowsException()
    {
        // Arrange — use a disposed context to trigger an exception
        var db = CreateDbContext();
        await db.DisposeAsync();

        var handler = new AddDefaultConfigurationCommandHandler(db, _mockLogger.Object);
        var command = new AddDefaultConfigurationCommand("TestKey", "{\"value\":\"default\"}");

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(
            () => handler.HandleAsync(command, CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_SetsCorrectCfgKeyAndCfgValue()
    {
        // Arrange
        using var db = CreateDbContext();
        var handler = new AddDefaultConfigurationCommandHandler(db, _mockLogger.Object);
        var cfgKey = "GeneralSettings";
        var defaultJson = "{\"theme\":\"dark\",\"language\":\"en\"}";
        var command = new AddDefaultConfigurationCommand(cfgKey, defaultJson);

        // Act
        await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        var entity = await db.BlogConfiguration.FirstOrDefaultAsync(c => c.CfgKey == cfgKey);
        Assert.NotNull(entity);
        Assert.Equal(cfgKey, entity.CfgKey);
        Assert.Equal(defaultJson, entity.CfgValue);
    }

    [Fact]
    public async Task HandleAsync_SetsLastModifiedTimeUtcToCurrentUtcTime()
    {
        // Arrange
        using var db = CreateDbContext();
        var handler = new AddDefaultConfigurationCommandHandler(db, _mockLogger.Object);
        var command = new AddDefaultConfigurationCommand("TimeTestKey", "{\"value\":\"test\"}");

        var before = DateTime.UtcNow;

        // Act
        await handler.HandleAsync(command, CancellationToken.None);

        var after = DateTime.UtcNow;

        // Assert
        var entity = await db.BlogConfiguration.FirstOrDefaultAsync(c => c.CfgKey == "TimeTestKey");
        Assert.NotNull(entity);
        Assert.NotNull(entity.LastModifiedTimeUtc);
        Assert.True(entity.LastModifiedTimeUtc >= before);
        Assert.True(entity.LastModifiedTimeUtc <= after);
    }
}
