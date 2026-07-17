using Microsoft.AspNetCore.Http;
using Moq;

namespace Moonglade.Web.Middleware.Tests;

public class SecurityHeadersMiddlewareTests
{
    private readonly Mock<RequestDelegate> _mockNext;
    private readonly SecurityHeadersMiddleware _middleware;

    public SecurityHeadersMiddlewareTests()
    {
        _mockNext = new Mock<RequestDelegate>();
        _middleware = new SecurityHeadersMiddleware(_mockNext.Object);
    }

    [Fact]
    public async Task Invoke_ShouldAddXContentTypeOptionsHeader()
    {
        // Arrange
        var context = new DefaultHttpContext();

        // Act
        await _middleware.Invoke(context);

        // Assert
        Assert.True(context.Response.Headers.ContainsKey("X-Content-Type-Options"));
        Assert.Equal("nosniff", context.Response.Headers["X-Content-Type-Options"].ToString());
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
    public async Task Invoke_ShouldNotOverrideExistingXContentTypeOptionsHeader()
    {
        // Arrange
        var context = new DefaultHttpContext();
        const string existingHeaderValue = "existing";
        context.Response.Headers.Append("X-Content-Type-Options", existingHeaderValue);

        // Act
        await _middleware.Invoke(context);

        // Assert
        Assert.Equal(existingHeaderValue, context.Response.Headers["X-Content-Type-Options"].ToString());
        _mockNext.Verify(next => next.Invoke(context), Times.Once);
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

        Assert.True(context.Response.Headers.ContainsKey("X-Content-Type-Options"));
    }
}
