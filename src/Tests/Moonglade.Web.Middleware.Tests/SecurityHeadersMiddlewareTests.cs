using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Moonglade.Web.Middleware.Tests;

public class SecurityHeadersMiddlewareTests
{
    private readonly Mock<RequestDelegate> _mockNext;
    private readonly SecurityHeadersMiddleware _middleware;

    public SecurityHeadersMiddlewareTests()
    {
        _mockNext = new Mock<RequestDelegate>();
        _middleware = new SecurityHeadersMiddleware(_mockNext.Object, CreateConfiguration());
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
    public async Task Invoke_WhenCspIsEnabledAndValueConfigured_ShouldAddContentSecurityPolicyHeader()
    {
        // Arrange
        var context = new DefaultHttpContext();
        const string cspValue = "default-src 'self'; img-src 'self' https:";
        var middleware = new SecurityHeadersMiddleware(_mockNext.Object, CreateConfiguration(enableCsp: true, cspValue: cspValue));

        // Act
        await middleware.Invoke(context);

        // Assert
        Assert.True(context.Response.Headers.ContainsKey("Content-Security-Policy"));
        Assert.Equal(cspValue, context.Response.Headers["Content-Security-Policy"].ToString());
    }

    [Fact]
    public async Task Invoke_WhenCspIsDisabled_ShouldNotAddContentSecurityPolicyHeader()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var middleware = new SecurityHeadersMiddleware(_mockNext.Object, CreateConfiguration(enableCsp: false, cspValue: "default-src 'self'"));

        // Act
        await middleware.Invoke(context);

        // Assert
        Assert.False(context.Response.Headers.ContainsKey("Content-Security-Policy"));
    }

    [Fact]
    public async Task Invoke_WhenCspIsEnabledButValueIsEmpty_ShouldNotAddContentSecurityPolicyHeader()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var middleware = new SecurityHeadersMiddleware(_mockNext.Object, CreateConfiguration(enableCsp: true, cspValue: ""));

        // Act
        await middleware.Invoke(context);

        // Assert
        Assert.False(context.Response.Headers.ContainsKey("Content-Security-Policy"));
    }

    [Fact]
    public async Task Invoke_ShouldNotOverrideExistingContentSecurityPolicyHeader()
    {
        // Arrange
        var context = new DefaultHttpContext();
        const string existingHeaderValue = "default-src 'none'";
        context.Response.Headers.Append("Content-Security-Policy", existingHeaderValue);
        var middleware = new SecurityHeadersMiddleware(_mockNext.Object, CreateConfiguration(enableCsp: true, cspValue: "default-src 'self'"));

        // Act
        await middleware.Invoke(context);

        // Assert
        Assert.Equal(existingHeaderValue, context.Response.Headers["Content-Security-Policy"].ToString());
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

    private static IConfiguration CreateConfiguration(bool enableCsp = false, string cspValue = "")
    {
        var values = new Dictionary<string, string>
        {
            ["EnableCSP"] = enableCsp.ToString(),
            ["CSPValue"] = cspValue
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}
