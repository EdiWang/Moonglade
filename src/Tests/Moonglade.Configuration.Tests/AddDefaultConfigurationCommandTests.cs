using Ardalis.Specification;
using Microsoft.Extensions.Logging;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moq;

namespace Moonglade.Configuration.Tests;

public class AddDefaultConfigurationCommandTests
{
    private readonly Mock<IRepositoryBase<BlogConfigurationEntity>> _mockRepo;
    private readonly Mock<ILogger<AddDefaultConfigurationCommandHandler>> _mockLogger;
    private readonly AddDefaultConfigurationCommandHandler _handler;

    public AddDefaultConfigurationCommandTests()
    {
        _mockRepo = new Mock<IRepositoryBase<BlogConfigurationEntity>>();
        _mockLogger = new Mock<ILogger<AddDefaultConfigurationCommandHandler>>();
        _handler = new AddDefaultConfigurationCommandHandler(_mockRepo.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task HandleAsync_ValidCommand_AddsEntityAndReturnsDone()
    {
        // Arrange
        var command = new AddDefaultConfigurationCommand("TestKey", "{\"value\":\"default\"}");

        BlogConfigurationEntity? capturedEntity = null;
        _mockRepo.Setup(r => r.AddAsync(It.IsAny<BlogConfigurationEntity>(), It.IsAny<CancellationToken>()))
            .Callback<BlogConfigurationEntity, CancellationToken>((entity, ct) =>
            {
                capturedEntity = entity;
            })
            .ReturnsAsync((BlogConfigurationEntity e, CancellationToken _) => e);

        var beforeAdd = DateTime.UtcNow;

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        Assert.Equal(OperationCode.Done, result);
        _mockRepo.Verify(r => r.AddAsync(It.IsAny<BlogConfigurationEntity>(), It.IsAny<CancellationToken>()), Times.Once);

        Assert.NotNull(capturedEntity);
        Assert.Equal("TestKey", capturedEntity.CfgKey);
        Assert.Equal("{\"value\":\"default\"}", capturedEntity.CfgValue);
        Assert.True(capturedEntity.LastModifiedTimeUtc >= beforeAdd);
    }

    [Fact]
    public async Task HandleAsync_RepositoryThrowsException_RethrowsException()
    {
        // Arrange
        var command = new AddDefaultConfigurationCommand("TestKey", "{\"value\":\"default\"}");
        var expectedException = new InvalidOperationException("Database error");

        _mockRepo.Setup(r => r.AddAsync(It.IsAny<BlogConfigurationEntity>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.HandleAsync(command, CancellationToken.None));

        Assert.Equal("Database error", exception.Message);
    }

    [Fact]
    public async Task HandleAsync_SetsCorrectCfgKeyAndCfgValue()
    {
        // Arrange
        var cfgKey = "GeneralSettings";
        var defaultJson = "{\"theme\":\"dark\",\"language\":\"en\"}";
        var command = new AddDefaultConfigurationCommand(cfgKey, defaultJson);

        BlogConfigurationEntity? capturedEntity = null;
        _mockRepo.Setup(r => r.AddAsync(It.IsAny<BlogConfigurationEntity>(), It.IsAny<CancellationToken>()))
            .Callback<BlogConfigurationEntity, CancellationToken>((entity, ct) =>
            {
                capturedEntity = entity;
            })
            .ReturnsAsync((BlogConfigurationEntity e, CancellationToken _) => e);

        // Act
        await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        Assert.NotNull(capturedEntity);
        Assert.Equal(cfgKey, capturedEntity.CfgKey);
        Assert.Equal(defaultJson, capturedEntity.CfgValue);
    }

    [Fact]
    public async Task HandleAsync_SetsLastModifiedTimeUtcToCurrentUtcTime()
    {
        // Arrange
        var command = new AddDefaultConfigurationCommand("TimeTestKey", "{\"value\":\"test\"}");

        BlogConfigurationEntity? capturedEntity = null;
        _mockRepo.Setup(r => r.AddAsync(It.IsAny<BlogConfigurationEntity>(), It.IsAny<CancellationToken>()))
            .Callback<BlogConfigurationEntity, CancellationToken>((entity, ct) =>
            {
                capturedEntity = entity;
            })
            .ReturnsAsync((BlogConfigurationEntity e, CancellationToken _) => e);

        var before = DateTime.UtcNow;

        // Act
        await _handler.HandleAsync(command, CancellationToken.None);

        var after = DateTime.UtcNow;

        // Assert
        Assert.NotNull(capturedEntity);
        Assert.NotNull(capturedEntity.LastModifiedTimeUtc);
        Assert.True(capturedEntity.LastModifiedTimeUtc >= before);
        Assert.True(capturedEntity.LastModifiedTimeUtc <= after);
    }
}
