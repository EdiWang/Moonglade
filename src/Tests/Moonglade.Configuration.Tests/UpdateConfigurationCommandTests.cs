using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moq;

namespace Moonglade.Configuration.Tests;

public class UpdateConfigurationCommandTests
{
    private readonly Mock<ILogger<UpdateConfigurationCommandHandler>> _mockLogger = new();

    private static BlogDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<BlogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new BlogDbContext(options);
    }

    [Fact]
    public async Task HandleAsync_ConfigurationNotFound_ReturnsObjectNotFound()
    {
        // Arrange
        using var db = CreateDbContext();
        var handler = new UpdateConfigurationCommandHandler(db, _mockLogger.Object);
        var command = new UpdateConfigurationCommand("NonExistentConfig", "{\"value\":\"test\"}");

        // Act
        var result = await handler.HandleAsync(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(OperationCode.ObjectNotFound, result);
    }

    [Fact]
    public async Task HandleAsync_ValueUnchanged_SkipsUpdateAndReturnsDone()
    {
        // Arrange
        using var db = CreateDbContext();
        var existingJson = "{\"value\":\"test\"}";
        db.BlogConfiguration.Add(new BlogConfigurationEntity
        {
            CfgKey = "ExistingConfig",
            CfgValue = existingJson,
            LastModifiedTimeUtc = DateTime.UtcNow.AddDays(-1)
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var originalTime = (await db.BlogConfiguration.FirstAsync(TestContext.Current.CancellationToken)).LastModifiedTimeUtc;

        var handler = new UpdateConfigurationCommandHandler(db, _mockLogger.Object);
        var command = new UpdateConfigurationCommand("ExistingConfig", existingJson);

        // Act
        var result = await handler.HandleAsync(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(OperationCode.Done, result);
        var entity = await db.BlogConfiguration.FirstAsync(TestContext.Current.CancellationToken);
        Assert.Equal(originalTime, entity.LastModifiedTimeUtc);
    }

    [Fact]
    public async Task HandleAsync_ValueChanged_UpdatesEntityAndReturnsDone()
    {
        // Arrange
        using var db = CreateDbContext();
        var oldJson = "{\"value\":\"old\"}";
        var newJson = "{\"value\":\"new\"}";

        db.BlogConfiguration.Add(new BlogConfigurationEntity
        {
            CfgKey = "ExistingConfig",
            CfgValue = oldJson,
            LastModifiedTimeUtc = DateTime.UtcNow.AddDays(-1)
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var beforeUpdate = DateTime.UtcNow;

        var handler = new UpdateConfigurationCommandHandler(db, _mockLogger.Object);
        var command = new UpdateConfigurationCommand("ExistingConfig", newJson);

        // Act
        var result = await handler.HandleAsync(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(OperationCode.Done, result);

        var entity = await db.BlogConfiguration.FirstAsync(TestContext.Current.CancellationToken);
        Assert.Equal(newJson, entity.CfgValue);
        Assert.NotNull(entity.LastModifiedTimeUtc);
        Assert.True(entity.LastModifiedTimeUtc >= beforeUpdate);
    }

    [Fact]
    public async Task HandleAsync_UsesCorrectConfigName()
    {
        // Arrange
        using var db = CreateDbContext();
        db.BlogConfiguration.Add(new BlogConfigurationEntity
        {
            CfgKey = "TestConfig",
            CfgValue = "{\"value\":\"old\"}",
            LastModifiedTimeUtc = DateTime.UtcNow
        });
        db.BlogConfiguration.Add(new BlogConfigurationEntity
        {
            CfgKey = "OtherConfig",
            CfgValue = "{\"value\":\"other\"}",
            LastModifiedTimeUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateConfigurationCommandHandler(db, _mockLogger.Object);
        var command = new UpdateConfigurationCommand("TestConfig", "{\"value\":\"new\"}");

        // Act
        await handler.HandleAsync(command, TestContext.Current.CancellationToken);

        // Assert
        var testConfig = await db.BlogConfiguration.FirstAsync(c => c.CfgKey == "TestConfig", TestContext.Current.CancellationToken);
        var otherConfig = await db.BlogConfiguration.FirstAsync(c => c.CfgKey == "OtherConfig", TestContext.Current.CancellationToken);
        Assert.Equal("{\"value\":\"new\"}", testConfig.CfgValue);
        Assert.Equal("{\"value\":\"other\"}", otherConfig.CfgValue);
    }

    [Fact]
    public async Task HandleAsync_ExceptionThrown_RethrowsException()
    {
        // Arrange — use a disposed context to trigger an exception
        var db = CreateDbContext();
        await db.DisposeAsync();

        var handler = new UpdateConfigurationCommandHandler(db, _mockLogger.Object);
        var command = new UpdateConfigurationCommand("TestConfig", "{\"value\":\"test\"}");

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(
            () => handler.HandleAsync(command, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task HandleAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        using var db = CreateDbContext();
        db.BlogConfiguration.Add(new BlogConfigurationEntity
        {
            CfgKey = "TestConfig",
            CfgValue = "{\"value\":\"old\"}",
            LastModifiedTimeUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateConfigurationCommandHandler(db, _mockLogger.Object);
        var command = new UpdateConfigurationCommand("TestConfig", "{\"value\":\"new\"}");
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => handler.HandleAsync(command, cts.Token));
    }
}
