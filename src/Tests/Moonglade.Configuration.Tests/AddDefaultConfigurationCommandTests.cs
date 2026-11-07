using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moonglade.Data;
using Moonglade.Data.Entities;
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
    public async Task HandleAsync_ExistingInvalidRow_ReplacesValueWithoutInsertingDuplicate()
    {
        var ct = TestContext.Current.CancellationToken;
        using var db = CreateDbContext();
        var oldTimestamp = DateTime.UtcNow.AddDays(-1);
        db.BlogConfiguration.Add(new BlogConfigurationEntity
        {
            CfgKey = nameof(FeedSettings),
            CfgValue = "not-json",
            LastModifiedTimeUtc = oldTimestamp
        });
        await db.SaveChangesAsync(ct);
        db.ChangeTracker.Clear();
        var handler = new AddDefaultConfigurationCommandHandler(db, _mockLogger.Object);
        var defaults = FeedSettings.DefaultValue.ToJson();

        var result = await handler.HandleAsync(new AddDefaultConfigurationCommand(nameof(FeedSettings), defaults), ct);

        db.ChangeTracker.Clear();
        var saved = await db.BlogConfiguration.SingleAsync(ct);
        Assert.Equal(OperationCode.Done, result);
        Assert.Equal(nameof(FeedSettings), saved.CfgKey);
        Assert.Equal(defaults, saved.CfgValue);
        Assert.True(saved.LastModifiedTimeUtc > oldTimestamp);
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
        var result = await handler.HandleAsync(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(OperationCode.Done, result);

        var entity = await db.BlogConfiguration.FirstOrDefaultAsync(c => c.CfgKey == "TestKey", TestContext.Current.CancellationToken);
        Assert.NotNull(entity);
        Assert.Equal("TestKey", entity.CfgKey);
        Assert.Equal("{\"value\":\"default\"}", entity.CfgValue);
        Assert.InRange(entity.LastModifiedTimeUtc!.Value, beforeAdd, DateTime.UtcNow);
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
            () => handler.HandleAsync(command, TestContext.Current.CancellationToken));
    }

}
