using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Moonglade.BackgroundServices.Tests;

public class CannonServiceTests
{
    private readonly Mock<ILogger<CannonService>> _loggerMock;
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock;
    private readonly Mock<IServiceScope> _scopeMock;
    private readonly Mock<IServiceProvider> _serviceProviderMock;

    public CannonServiceTests()
    {
        _loggerMock = new Mock<ILogger<CannonService>>();
        _scopeFactoryMock = new Mock<IServiceScopeFactory>();
        _scopeMock = new Mock<IServiceScope>();
        _serviceProviderMock = new Mock<IServiceProvider>();

        _scopeFactoryMock.Setup(x => x.CreateScope()).Returns(_scopeMock.Object);
        _scopeMock.Setup(x => x.ServiceProvider).Returns(_serviceProviderMock.Object);
    }

    [Fact]
    public void CannonService_CanBeConstructed()
    {
        // Arrange & Act
        var service = new CannonService(_loggerMock.Object, _scopeFactoryMock.Object);

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public async Task FireAsync_ExecutesWorkItem_Successfully()
    {
        // Arrange
        var service = new CannonService(_loggerMock.Object, _scopeFactoryMock.Object);
        var testService = new Mock<ITestService>();
        var executed = false;

        _serviceProviderMock.Setup(x => x.GetService(typeof(ITestService)))
            .Returns(testService.Object);

        testService.Setup(x => x.DoWorkAsync())
            .Callback(() => executed = true)
            .Returns(Task.CompletedTask);

        // Act
        service.FireAsync<ITestService>(async svc => await svc.DoWorkAsync());

        // Start the background service
        using var cts = new CancellationTokenSource();
        await service.StartAsync(cts.Token);

        // Give it some time to process
        await Task.Delay(100, TestContext.Current.CancellationToken);

        // Stop the service
        cts.Cancel();
        await service.StopAsync(CancellationToken.None);

        // Assert
        Assert.True(executed);
        testService.Verify(x => x.DoWorkAsync(), Times.Once);
    }

    [Fact]
    public async Task FireAsync_WithException_LogsError()
    {
        // Arrange
        var service = new CannonService(_loggerMock.Object, _scopeFactoryMock.Object);
        var testService = new Mock<ITestService>();
        var expectedException = new InvalidOperationException("Test exception");

        _serviceProviderMock.Setup(x => x.GetService(typeof(ITestService)))
            .Returns(testService.Object);

        testService.Setup(x => x.DoWorkAsync())
            .ThrowsAsync(expectedException);

        // Act
        service.FireAsync<ITestService>(async svc => await svc.DoWorkAsync());

        using var cts = new CancellationTokenSource();
        await service.StartAsync(cts.Token);
        await Task.Delay(100, TestContext.Current.CancellationToken);
        cts.Cancel();
        await service.StopAsync(CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error executing background work item")),
                expectedException,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task FireAsync_WithExceptionAndHandler_CallsHandler()
    {
        // Arrange
        var service = new CannonService(_loggerMock.Object, _scopeFactoryMock.Object);
        var testService = new Mock<ITestService>();
        var expectedException = new InvalidOperationException("Test exception");
        Exception? caughtException = null;

        _serviceProviderMock.Setup(x => x.GetService(typeof(ITestService)))
            .Returns(testService.Object);

        testService.Setup(x => x.DoWorkAsync())
            .ThrowsAsync(expectedException);

        // Act
        service.FireAsync<ITestService>(
            async svc => await svc.DoWorkAsync(),
            handler: ex => caughtException = ex);

        using var cts = new CancellationTokenSource();
        await service.StartAsync(cts.Token);
        await Task.Delay(100, TestContext.Current.CancellationToken);
        cts.Cancel();
        await service.StopAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(caughtException);
        Assert.Same(expectedException, caughtException);
    }

    [Fact]
    public async Task FireAsync_MultipleWorkItems_ExecutesAllSequentially()
    {
        // Arrange
        var service = new CannonService(_loggerMock.Object, _scopeFactoryMock.Object);
        var testService = new Mock<ITestService>();
        var executionOrder = new List<int>();

        _serviceProviderMock.Setup(x => x.GetService(typeof(ITestService)))
            .Returns(testService.Object);

        testService.Setup(x => x.DoWorkAsync())
            .Returns(Task.CompletedTask);

        // Act
        service.FireAsync<ITestService>(async svc =>
        {
            executionOrder.Add(1);
            await svc.DoWorkAsync();
        });

        service.FireAsync<ITestService>(async svc =>
        {
            executionOrder.Add(2);
            await svc.DoWorkAsync();
        });

        service.FireAsync<ITestService>(async svc =>
        {
            executionOrder.Add(3);
            await svc.DoWorkAsync();
        });

        using var cts = new CancellationTokenSource();
        await service.StartAsync(cts.Token);
        await Task.Delay(200, TestContext.Current.CancellationToken);
        cts.Cancel();
        await service.StopAsync(CancellationToken.None);

        // Assert
        Assert.Equal(3, executionOrder.Count);
        Assert.Equal([1, 2, 3], executionOrder);
        testService.Verify(x => x.DoWorkAsync(), Times.Exactly(3));
    }

    [Fact]
    public async Task StopAsync_CompletesQueueWriter_AndDrainsRemainingItems()
    {
        // Arrange
        var service = new CannonService(_loggerMock.Object, _scopeFactoryMock.Object);
        var testService = new Mock<ITestService>();
        var executedCount = 0;

        _serviceProviderMock.Setup(x => x.GetService(typeof(ITestService)))
            .Returns(testService.Object);

        testService.Setup(x => x.DoWorkAsync())
            .Callback(() => executedCount++)
            .Returns(Task.CompletedTask);

        // Act
        service.FireAsync<ITestService>(async svc => await svc.DoWorkAsync());
        service.FireAsync<ITestService>(async svc => await svc.DoWorkAsync());

        using var cts = new CancellationTokenSource();
        await service.StartAsync(cts.Token);
        await Task.Delay(50, TestContext.Current.CancellationToken);

        // Stop should drain remaining items
        await service.StopAsync(CancellationToken.None);

        // Assert
        Assert.Equal(2, executedCount);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Draining remaining work items")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_LogsStartup()
    {
        // Arrange
        var service = new CannonService(_loggerMock.Object, _scopeFactoryMock.Object);

        // Act
        using var cts = new CancellationTokenSource();
        await service.StartAsync(cts.Token);
        await Task.Delay(50, TestContext.Current.CancellationToken);
        cts.Cancel();
        await service.StopAsync(CancellationToken.None);

        // Assert - Verify startup log
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("background queue is starting")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task FireAsync_CreatesNewScope_ForEachWorkItem()
    {
        // Arrange
        var service = new CannonService(_loggerMock.Object, _scopeFactoryMock.Object);
        var testService = new Mock<ITestService>();

        _serviceProviderMock.Setup(x => x.GetService(typeof(ITestService)))
            .Returns(testService.Object);

        testService.Setup(x => x.DoWorkAsync())
            .Returns(Task.CompletedTask);

        // Act
        service.FireAsync<ITestService>(async svc => await svc.DoWorkAsync());
        service.FireAsync<ITestService>(async svc => await svc.DoWorkAsync());

        using var cts = new CancellationTokenSource();
        await service.StartAsync(cts.Token);
        await Task.Delay(100, TestContext.Current.CancellationToken);
        cts.Cancel();
        await service.StopAsync(CancellationToken.None);

        // Assert
        _scopeFactoryMock.Verify(x => x.CreateScope(), Times.Exactly(2));
        _scopeMock.Verify(x => x.Dispose(), Times.Exactly(2));
    }

    public interface ITestService
    {
        Task DoWorkAsync();
    }
}
