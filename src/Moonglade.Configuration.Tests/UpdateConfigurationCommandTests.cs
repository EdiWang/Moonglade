using Ardalis.Specification;
using Microsoft.Extensions.Logging;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Data.Specifications;
using Moq;

namespace Moonglade.Configuration.Tests;

public class UpdateConfigurationCommandTests
{
    private readonly Mock<IRepositoryBase<BlogConfigurationEntity>> _mockRepo;
    private readonly Mock<ILogger<UpdateConfigurationCommandHandler>> _mockLogger;
    private readonly UpdateConfigurationCommandHandler _handler;

    public UpdateConfigurationCommandTests()
    {
        _mockRepo = new Mock<IRepositoryBase<BlogConfigurationEntity>>();
        _mockLogger = new Mock<ILogger<UpdateConfigurationCommandHandler>>();
        _handler = new UpdateConfigurationCommandHandler(_mockRepo.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task HandleAsync_ConfigurationNotFound_ReturnsObjectNotFound()
    {
        // Arrange
        var command = new UpdateConfigurationCommand("NonExistentConfig", "{\"value\":\"test\"}");

        _mockRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<BlogConfigurationSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BlogConfigurationEntity?)null);

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        Assert.Equal(OperationCode.ObjectNotFound, result);
        _mockRepo.Verify(r => r.FirstOrDefaultAsync(It.IsAny<BlogConfigurationSpec>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockRepo.Verify(r => r.UpdateAsync(It.IsAny<BlogConfigurationEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ValueUnchanged_SkipsUpdateAndReturnsDone()
    {
        // Arrange
        var existingJson = "{\"value\":\"test\"}";
        var command = new UpdateConfigurationCommand("ExistingConfig", existingJson);

        var existingEntity = new BlogConfigurationEntity
        {
            CfgKey = "ExistingConfig",
            CfgValue = existingJson,
            LastModifiedTimeUtc = DateTime.UtcNow.AddDays(-1)
        };

        _mockRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<BlogConfigurationSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        Assert.Equal(OperationCode.Done, result);
        _mockRepo.Verify(r => r.FirstOrDefaultAsync(It.IsAny<BlogConfigurationSpec>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockRepo.Verify(r => r.UpdateAsync(It.IsAny<BlogConfigurationEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ValueChanged_UpdatesEntityAndReturnsDone()
    {
        // Arrange
        var oldJson = "{\"value\":\"old\"}";
        var newJson = "{\"value\":\"new\"}";
        var command = new UpdateConfigurationCommand("ExistingConfig", newJson);

        var existingEntity = new BlogConfigurationEntity
        {
            CfgKey = "ExistingConfig",
            CfgValue = oldJson,
            LastModifiedTimeUtc = DateTime.UtcNow.AddDays(-1)
        };

        var beforeUpdate = DateTime.UtcNow;

        _mockRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<BlogConfigurationSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        BlogConfigurationEntity? capturedEntity = null;
        _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<BlogConfigurationEntity>(), It.IsAny<CancellationToken>()))
            .Callback<BlogConfigurationEntity, CancellationToken>((entity, ct) =>
            {
                capturedEntity = entity;
            })
            .ReturnsAsync(1);

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        Assert.Equal(OperationCode.Done, result);
        _mockRepo.Verify(r => r.FirstOrDefaultAsync(It.IsAny<BlogConfigurationSpec>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockRepo.Verify(r => r.UpdateAsync(It.IsAny<BlogConfigurationEntity>(), It.IsAny<CancellationToken>()), Times.Once);

        Assert.NotNull(capturedEntity);
        Assert.Equal(newJson, capturedEntity.CfgValue);
        Assert.NotNull(capturedEntity.LastModifiedTimeUtc);
        Assert.True(capturedEntity.LastModifiedTimeUtc >= beforeUpdate);
    }

    [Fact]
    public async Task HandleAsync_UsesCorrectSpecification()
    {
        // Arrange
        var configName = "TestConfig";
        var command = new UpdateConfigurationCommand(configName, "{\"value\":\"test\"}");

        ISpecification<BlogConfigurationEntity>? capturedSpec = null;
        _mockRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<BlogConfigurationSpec>(), It.IsAny<CancellationToken>()))
            .Callback<ISpecification<BlogConfigurationEntity>, CancellationToken>((spec, ct) =>
            {
                capturedSpec = spec;
            })
            .ReturnsAsync((BlogConfigurationEntity?)null);

        // Act
        await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        Assert.NotNull(capturedSpec);
        _mockRepo.Verify(r => r.FirstOrDefaultAsync(It.IsAny<BlogConfigurationSpec>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ExceptionThrown_RethrowsException()
    {
        // Arrange
        var command = new UpdateConfigurationCommand("TestConfig", "{\"value\":\"test\"}");
        var expectedException = new InvalidOperationException("Database error");

        _mockRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<BlogConfigurationSpec>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.HandleAsync(command, CancellationToken.None));

        Assert.Equal("Database error", exception.Message);
        _mockRepo.Verify(r => r.UpdateAsync(It.IsAny<BlogConfigurationEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_UpdateThrowsException_RethrowsException()
    {
        // Arrange
        var command = new UpdateConfigurationCommand("TestConfig", "{\"value\":\"new\"}");
        var existingEntity = new BlogConfigurationEntity
        {
            CfgKey = "TestConfig",
            CfgValue = "{\"value\":\"old\"}",
            LastModifiedTimeUtc = DateTime.UtcNow.AddDays(-1)
        };

        _mockRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<BlogConfigurationSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        var expectedException = new InvalidOperationException("Update failed");
        _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<BlogConfigurationEntity>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.HandleAsync(command, CancellationToken.None));

        Assert.Equal("Update failed", exception.Message);
    }

    [Fact]
    public async Task HandleAsync_CancellationRequested_PassesCancellationToken()
    {
        // Arrange
        var command = new UpdateConfigurationCommand("TestConfig", "{\"value\":\"test\"}");
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _mockRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<BlogConfigurationSpec>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _handler.HandleAsync(command, cts.Token));
    }
}
