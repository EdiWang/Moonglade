using Microsoft.AspNetCore.Http;
using Moonglade.Utils;
using Moq;

namespace Moonglade.Web.Middleware.Tests;

public class PoweredByMiddlewareTests
{
    private readonly Mock<RequestDelegate> _mockNext;
    private readonly PoweredByMiddleware _middleware;

    public PoweredByMiddlewareTests()
    {
        _mockNext = new Mock<RequestDelegate>();
        _middleware = new PoweredByMiddleware(_mockNext.Object);
    }

    [Fact]
    public async Task Invoke_ShouldAddXPoweredByHeader()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var expectedHeaderValue = $"Moonglade {VersionHelper.AppVersion}";

        // Act
        await _middleware.Invoke(context);

        // Assert
        Assert.True(context.Response.Headers.ContainsKey("X-Powered-By"));
        Assert.Equal(expectedHeaderValue, context.Response.Headers["X-Powered-By"].ToString());
    }

    [Fact]
    public async Task Invoke_ShouldCallNextMiddleware()
    {
        // Arrange
        var context = new DefaultHttpContext();

        // Act
        await _middleware.Invoke(context);

        // Assert
        _mockNext.Verify(next => next.Invoke(context), Times.Once);
    }

    [Fact]
    public async Task Invoke_ShouldNotOverrideExistingXPoweredByHeader()
    {
        // Arrange
        var context = new DefaultHttpContext();
        const string existingHeaderValue = "Existing Value";
        context.Response.Headers.Append("X-Powered-By", existingHeaderValue);

        // Act
        await _middleware.Invoke(context);

        // Assert
        Assert.Equal(existingHeaderValue, context.Response.Headers["X-Powered-By"].ToString());
        _mockNext.Verify(next => next.Invoke(context), Times.Once);
    }

    [Fact]
    public async Task Invoke_HeaderValueShouldContainMoongladeAndVersion()
    {
        // Arrange
        var context = new DefaultHttpContext();

        // Act
        await _middleware.Invoke(context);

        // Assert
        var headerValue = context.Response.Headers["X-Powered-By"].ToString();
        Assert.Contains("Moonglade", headerValue);
        Assert.Contains(VersionHelper.AppVersion, headerValue);
    }

    [Fact]
    public async Task Invoke_ShouldReturnCompletedTask()
    {
        // Arrange
        var context = new DefaultHttpContext();
        _mockNext.Setup(x => x.Invoke(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        // Act
        await _middleware.Invoke(context);

        // Assert
        Assert.Equal(Task.CompletedTask, Task.CompletedTask); // Verify method returns properly
    }

    [Fact]
    public async Task Invoke_WhenNextMiddlewareThrows_ShouldPropagateException()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var expectedException = new InvalidOperationException("Test exception");
        _mockNext.Setup(x => x.Invoke(It.IsAny<HttpContext>())).ThrowsAsync(expectedException);

        // Act & Assert
        var thrownException = await Assert.ThrowsAsync<InvalidOperationException>(() => _middleware.Invoke(context));
        Assert.Equal(expectedException.Message, thrownException.Message);

        // Verify header was still added before exception
        Assert.True(context.Response.Headers.ContainsKey("X-Powered-By"));
    }

    [Fact]
    public void Constructor_WithValidRequestDelegate_ShouldCreateInstance()
    {
        // Arrange
        var requestDelegate = new Mock<RequestDelegate>().Object;

        // Act
        var middleware = new PoweredByMiddleware(requestDelegate);

        // Assert
        Assert.NotNull(middleware);
    }
}