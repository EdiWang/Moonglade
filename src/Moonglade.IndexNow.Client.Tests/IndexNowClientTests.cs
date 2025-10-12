using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;

namespace Moonglade.IndexNow.Client.Tests;

public class IndexNowClientTests
{
    private readonly Mock<ILogger<IndexNowClient>> _loggerMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;

    public IndexNowClientTests()
    {
        _loggerMock = new Mock<ILogger<IndexNowClient>>();
        _configurationMock = new Mock<IConfiguration>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
    }

    [Fact]
    public async Task SendRequestAsync_ApiKeyNotConfigured_LogsWarning()
    {
        // Arrange
        SetupConfiguration("test-key-12345", ["https://api.bing.com"]);

        var client = new IndexNowClient(_loggerMock.Object, _configurationMock.Object, _httpClientFactoryMock.Object);
        var uri = new Uri("https://example.com/post");

        // Act
        await client.SendRequestAsync(uri);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("IndexNow:ApiKey is not configured")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Never);
    }

    [Fact]
    public async Task SendRequestAsync_PingTargetsNotConfigured_LogsWarning()
    {
        // Arrange
        SetupConfiguration("test-key-12345", Array.Empty<string>());

        var client = new IndexNowClient(_loggerMock.Object, _configurationMock.Object, _httpClientFactoryMock.Object);
        var uri = new Uri("https://example.com/post");

        // Act
        await client.SendRequestAsync(uri);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("IndexNow:PingTargets is not configured")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendRequestAsync_ValidRequest_SendsToAllPingTargets()
    {
        // Arrange
        var apiKey = "test-key-12345";
        var pingTargets = new[] { "bing", "google" };
        var uri = new Uri("https://example.com/post");

        SetupConfiguration(apiKey, pingTargets);

        var handlerMock1 = CreateMockHttpMessageHandler(HttpStatusCode.OK, "Success");
        var handlerMock2 = CreateMockHttpMessageHandler(HttpStatusCode.OK, "Success");

        var httpClient1 = new HttpClient(handlerMock1.Object) { BaseAddress = new Uri("https://api.indexnow.org") };
        var httpClient2 = new HttpClient(handlerMock2.Object) { BaseAddress = new Uri("https://api.indexnow.org") };

        _httpClientFactoryMock.Setup(f => f.CreateClient("bing")).Returns(httpClient1);
        _httpClientFactoryMock.Setup(f => f.CreateClient("google")).Returns(httpClient2);

        var client = new IndexNowClient(_loggerMock.Object, _configurationMock.Object, _httpClientFactoryMock.Object);

        // Act
        await client.SendRequestAsync(uri);

        // Assert
        handlerMock1.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Post &&
                req.RequestUri.ToString().Contains("/indexnow")),
            ItExpr.IsAny<CancellationToken>());

        handlerMock2.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Post &&
                req.RequestUri.ToString().Contains("/indexnow")),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task SendRequestAsync_HttpStatusOK_LogsInformation()
    {
        // Arrange
        var apiKey = "test-key-12345";
        var pingTarget = "bing";
        var uri = new Uri("https://example.com/post");

        SetupConfiguration(apiKey, [pingTarget]);

        var handlerMock = CreateMockHttpMessageHandler(HttpStatusCode.OK, "Success");
        var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new Uri("https://api.indexnow.org") };
        _httpClientFactoryMock.Setup(f => f.CreateClient(pingTarget)).Returns(httpClient);

        var client = new IndexNowClient(_loggerMock.Object, _configurationMock.Object, _httpClientFactoryMock.Object);

        // Act
        await client.SendRequestAsync(uri);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("URL submitted successfully")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendRequestAsync_HttpStatusAccepted_LogsWarning()
    {
        // Arrange
        var apiKey = "test-key-12345";
        var pingTarget = "bing";
        var uri = new Uri("https://example.com/post");

        SetupConfiguration(apiKey, [pingTarget]);

        var handlerMock = CreateMockHttpMessageHandler(HttpStatusCode.Accepted, "");
        var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new Uri("https://api.indexnow.org") };
        _httpClientFactoryMock.Setup(f => f.CreateClient(pingTarget)).Returns(httpClient);

        var client = new IndexNowClient(_loggerMock.Object, _configurationMock.Object, _httpClientFactoryMock.Object);

        // Act
        await client.SendRequestAsync(uri);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("IndexNow key validation pending")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, "Invalid format")]
    [InlineData(HttpStatusCode.Forbidden, "Key not valid")]
    [InlineData(HttpStatusCode.UnprocessableEntity, "URL or key mismatch")]
    [InlineData(HttpStatusCode.TooManyRequests, "Too many requests")]
    public async Task SendRequestAsync_ErrorStatusCodes_LogsError(HttpStatusCode statusCode, string expectedMessage)
    {
        // Arrange
        var apiKey = "test-key-12345";
        var pingTarget = "bing";
        var uri = new Uri("https://example.com/post");

        SetupConfiguration(apiKey, [pingTarget]);

        var handlerMock = CreateMockHttpMessageHandler(statusCode, "Error");
        var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new Uri("https://api.indexnow.org") };
        _httpClientFactoryMock.Setup(f => f.CreateClient(pingTarget)).Returns(httpClient);

        var client = new IndexNowClient(_loggerMock.Object, _configurationMock.Object, _httpClientFactoryMock.Object);

        // Act
        await client.SendRequestAsync(uri);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(expectedMessage)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendRequestAsync_HttpRequestException_LogsError()
    {
        // Arrange
        var apiKey = "test-key-12345";
        var pingTarget = "bing";
        var uri = new Uri("https://example.com/post");

        SetupConfiguration(apiKey, [pingTarget]);

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new Uri("https://api.indexnow.org") };
        _httpClientFactoryMock.Setup(f => f.CreateClient(pingTarget)).Returns(httpClient);

        var client = new IndexNowClient(_loggerMock.Object, _configurationMock.Object, _httpClientFactoryMock.Object);

        // Act
        await client.SendRequestAsync(uri);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to send index request")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public void Constructor_ApiKeyNotConfigured_ThrowsInvalidOperationException()
    {
        // Arrange
        _configurationMock.Setup(c => c["IndexNow:ApiKey"]).Returns((string)null);
        SetupPingTargetsSection(["https://api.bing.com"]);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            new IndexNowClient(_loggerMock.Object, _configurationMock.Object, _httpClientFactoryMock.Object));

        Assert.Contains("IndexNow:ApiKey is not configured", exception.Message);
    }

    private void SetupConfiguration(string apiKey, string[] pingTargets)
    {
        _configurationMock.Setup(c => c["IndexNow:ApiKey"]).Returns(apiKey);
        SetupPingTargetsSection(pingTargets);
    }

    private void SetupPingTargetsSection(string[] pingTargets)
    {
        var pingTargetsSectionMock = new Mock<IConfigurationSection>();

        // Create mock configuration sections for each ping target
        var configSections = pingTargets.Select((target, index) =>
        {
            var sectionMock = new Mock<IConfigurationSection>();
            sectionMock.Setup(s => s.Value).Returns(target);
            sectionMock.Setup(s => s.Key).Returns(index.ToString());
            sectionMock.Setup(s => s.Path).Returns($"IndexNow:PingTargets:{index}");
            return sectionMock.Object;
        }).ToList();

        pingTargetsSectionMock.Setup(s => s.GetChildren()).Returns(configSections);
        _configurationMock.Setup(c => c.GetSection("IndexNow:PingTargets")).Returns(pingTargetsSectionMock.Object);
    }

    private static Mock<HttpMessageHandler> CreateMockHttpMessageHandler(HttpStatusCode statusCode, string content)
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content)
            });

        return handlerMock;
    }
}