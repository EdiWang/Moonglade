using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;

namespace Moonglade.Moderation.Tests;

public class RemoteModerationServiceTests
{
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly Mock<ILogger<RemoteModerationService>> _mockLogger;
    private readonly HttpClient _httpClient;
    private readonly RemoteModerationService _service;

    public RemoteModerationServiceTests()
    {
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _mockLogger = new Mock<ILogger<RemoteModerationService>>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("https://api.example.com")
        };
        _service = new RemoteModerationService(_httpClient, _mockLogger.Object);
    }

    #region MaskAsync Tests

    [Fact]
    public async Task MaskAsync_WithSuccessfulResponse_ReturnsMaskedContent()
    {
        // Arrange
        const string input = "This contains bad words";
        const string requestId = "test-request-id";
        const string maskedText = "This contains *** words";

        var expectedResponse = new ModeratorResponse
        {
            ProcessedContents = new[]
            {
                new ProcessedContent { Id = "0", ProcessedText = maskedText }
            }
        };

        var responseJson = JsonSerializer.Serialize(expectedResponse);
        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, System.Text.Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.ToString().EndsWith("/api/mask")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponseMessage);

        // Act
        var result = await _service.MaskAsync(input, requestId);

        // Assert
        Assert.Equal(maskedText, result);
    }

    [Fact]
    public async Task MaskAsync_WithNullProcessedContents_ReturnsOriginalInput()
    {
        // Arrange
        const string input = "Test input";
        const string requestId = "test-request-id";

        var expectedResponse = new ModeratorResponse
        {
            ProcessedContents = null
        };

        var responseJson = JsonSerializer.Serialize(expectedResponse);
        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, System.Text.Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponseMessage);

        // Act
        var result = await _service.MaskAsync(input, requestId);

        // Assert
        Assert.Equal(input, result);
    }

    [Fact]
    public async Task MaskAsync_WithEmptyProcessedContents_ReturnsOriginalInput()
    {
        // Arrange
        const string input = "Test input";
        const string requestId = "test-request-id";

        var expectedResponse = new ModeratorResponse
        {
            ProcessedContents = Array.Empty<ProcessedContent>()
        };

        var responseJson = JsonSerializer.Serialize(expectedResponse);
        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, System.Text.Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponseMessage);

        // Act
        var result = await _service.MaskAsync(input, requestId);

        // Assert
        Assert.Equal(input, result);
    }

    [Fact]
    public async Task MaskAsync_WithNullProcessedText_ReturnsOriginalInput()
    {
        // Arrange
        const string input = "Test input";
        const string requestId = "test-request-id";

        var expectedResponse = new ModeratorResponse
        {
            ProcessedContents = new[]
            {
                new ProcessedContent { Id = "0", ProcessedText = null }
            }
        };

        var responseJson = JsonSerializer.Serialize(expectedResponse);
        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, System.Text.Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponseMessage);

        // Act
        var result = await _service.MaskAsync(input, requestId);

        // Assert
        Assert.Equal(input, result);
    }

    [Fact]
    public async Task MaskAsync_WithHttpRequestException_LogsErrorAndReturnsOriginalInput()
    {
        // Arrange
        const string input = "Test input";
        const string requestId = "test-request-id";
        var exception = new HttpRequestException("Network error");

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(exception);

        // Act
        var result = await _service.MaskAsync(input, requestId);

        // Assert
        Assert.Equal(input, result);
        VerifyErrorLogging("HTTP error occurred while masking content", exception);
    }

    [Fact]
    public async Task MaskAsync_WithTaskCanceledException_LogsErrorAndReturnsOriginalInput()
    {
        // Arrange
        const string input = "Test input";
        const string requestId = "test-request-id";
        var exception = new TaskCanceledException("Request timeout");

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(exception);

        // Act
        var result = await _service.MaskAsync(input, requestId);

        // Assert
        Assert.Equal(input, result);
        VerifyErrorLogging("Request timeout while masking content", exception);
    }

    [Fact]
    public async Task MaskAsync_WithJsonException_LogsErrorAndReturnsOriginalInput()
    {
        // Arrange
        const string input = "Test input";
        const string requestId = "test-request-id";

        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("invalid json", System.Text.Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponseMessage);

        // Act
        var result = await _service.MaskAsync(input, requestId);

        // Assert
        Assert.Equal(input, result);
        VerifyErrorLogging("JSON parsing error while masking content");
    }

    [Fact]
    public async Task MaskAsync_WithNonSuccessStatusCode_LogsErrorAndReturnsOriginalInput()
    {
        // Arrange
        const string input = "Test input";
        const string requestId = "test-request-id";

        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("Bad Request")
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponseMessage);

        // Act
        var result = await _service.MaskAsync(input, requestId);

        // Assert
        Assert.Equal(input, result);
        VerifyErrorLogging("HTTP error occurred while masking content");
    }

    [Fact]
    public async Task MaskAsync_SendsCorrectPayload()
    {
        // Arrange
        const string input = "Test input";
        const string requestId = "test-request-id";

        var expectedResponse = new ModeratorResponse
        {
            ProcessedContents = new[]
            {
                new ProcessedContent { Id = "0", ProcessedText = "Masked input" }
            }
        };

        var responseJson = JsonSerializer.Serialize(expectedResponse);
        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, System.Text.Encoding.UTF8, "application/json")
        };

        HttpRequestMessage capturedRequest = null;
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((request, _) => capturedRequest = request)
            .ReturnsAsync(httpResponseMessage);

        // Act
        await _service.MaskAsync(input, requestId);

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Post, capturedRequest.Method);
        Assert.EndsWith("/api/mask", capturedRequest.RequestUri!.ToString());
        Assert.Equal("application/json", capturedRequest.Content!.Headers.ContentType!.MediaType);

        var requestBody = await capturedRequest.Content.ReadAsStringAsync();
        var payload = JsonSerializer.Deserialize<Payload>(requestBody);

        Assert.NotNull(payload);
        Assert.Equal(requestId, payload.OriginAspNetRequestId);
        Assert.Single(payload.Contents);
        Assert.Equal("0", payload.Contents[0].Id);
        Assert.Equal(input, payload.Contents[0].RawText);
    }

    #endregion

    #region DetectAsync Tests

    [Fact]
    public async Task DetectAsync_WithPositiveDetection_ReturnsTrue()
    {
        // Arrange
        var input = new[] { "This contains bad words", "Another sentence" };
        const string requestId = "test-request-id";

        var expectedResponse = new ModeratorResponse
        {
            Positive = true
        };

        var responseJson = JsonSerializer.Serialize(expectedResponse);
        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, System.Text.Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.ToString().EndsWith("/api/detect")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponseMessage);

        // Act
        var result = await _service.DetectAsync(input, requestId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task DetectAsync_WithNegativeDetection_ReturnsFalse()
    {
        // Arrange
        var input = new[] { "This is clean content", "Another clean sentence" };
        const string requestId = "test-request-id";

        var expectedResponse = new ModeratorResponse
        {
            Positive = false
        };

        var responseJson = JsonSerializer.Serialize(expectedResponse);
        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, System.Text.Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponseMessage);

        // Act
        var result = await _service.DetectAsync(input, requestId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DetectAsync_WithNullPositive_ReturnsFalse()
    {
        // Arrange
        var input = new[] { "Test input" };
        const string requestId = "test-request-id";

        var expectedResponse = new ModeratorResponse
        {
            Positive = null
        };

        var responseJson = JsonSerializer.Serialize(expectedResponse);
        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, System.Text.Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponseMessage);

        // Act
        var result = await _service.DetectAsync(input, requestId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DetectAsync_WithHttpRequestException_LogsErrorAndReturnsFalse()
    {
        // Arrange
        var input = new[] { "Test input" };
        const string requestId = "test-request-id";
        var exception = new HttpRequestException("Network error");

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(exception);

        // Act
        var result = await _service.DetectAsync(input, requestId);

        // Assert
        Assert.False(result);
        VerifyErrorLogging("HTTP error occurred while detecting content", exception);
    }

    [Fact]
    public async Task DetectAsync_WithTaskCanceledException_LogsErrorAndReturnsFalse()
    {
        // Arrange
        var input = new[] { "Test input" };
        const string requestId = "test-request-id";
        var exception = new TaskCanceledException("Request timeout");

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(exception);

        // Act
        var result = await _service.DetectAsync(input, requestId);

        // Assert
        Assert.False(result);
        VerifyErrorLogging("Request timeout while detecting content", exception);
    }

    [Fact]
    public async Task DetectAsync_WithJsonException_LogsErrorAndReturnsFalse()
    {
        // Arrange
        var input = new[] { "Test input" };
        const string requestId = "test-request-id";

        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("invalid json", System.Text.Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponseMessage);

        // Act
        var result = await _service.DetectAsync(input, requestId);

        // Assert
        Assert.False(result);
        VerifyErrorLogging("JSON parsing error while detecting content");
    }

    [Fact]
    public async Task DetectAsync_WithNonSuccessStatusCode_LogsErrorAndReturnsFalse()
    {
        // Arrange
        var input = new[] { "Test input" };
        const string requestId = "test-request-id";

        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("Internal Server Error")
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponseMessage);

        // Act
        var result = await _service.DetectAsync(input, requestId);

        // Assert
        Assert.False(result);
        VerifyErrorLogging("HTTP error occurred while detecting content");
    }

    [Fact]
    public async Task DetectAsync_SendsCorrectPayload()
    {
        // Arrange
        var input = new[] { "First input", "Second input", "Third input" };
        const string requestId = "test-request-id";

        var expectedResponse = new ModeratorResponse
        {
            Positive = true
        };

        var responseJson = JsonSerializer.Serialize(expectedResponse);
        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, System.Text.Encoding.UTF8, "application/json")
        };

        HttpRequestMessage capturedRequest = null;
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((request, _) => capturedRequest = request)
            .ReturnsAsync(httpResponseMessage);

        // Act
        await _service.DetectAsync(input, requestId);

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Post, capturedRequest.Method);
        Assert.EndsWith("/api/detect", capturedRequest.RequestUri!.ToString());
        Assert.Equal("application/json", capturedRequest.Content!.Headers.ContentType!.MediaType);

        var requestBody = await capturedRequest.Content.ReadAsStringAsync();
        var payload = JsonSerializer.Deserialize<Payload>(requestBody);

        Assert.NotNull(payload);
        Assert.Equal(requestId, payload.OriginAspNetRequestId);
        Assert.Equal(3, payload.Contents.Length);

        for (int i = 0; i < input.Length; i++)
        {
            Assert.Equal(i.ToString(), payload.Contents[i].Id);
            Assert.Equal(input[i], payload.Contents[i].RawText);
        }
    }

    [Fact]
    public async Task DetectAsync_WithEmptyInputArray_SendsEmptyContents()
    {
        // Arrange
        var input = Array.Empty<string>();
        const string requestId = "test-request-id";

        var expectedResponse = new ModeratorResponse
        {
            Positive = false
        };

        var responseJson = JsonSerializer.Serialize(expectedResponse);
        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, System.Text.Encoding.UTF8, "application/json")
        };

        HttpRequestMessage capturedRequest = null;
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((request, _) => capturedRequest = request)
            .ReturnsAsync(httpResponseMessage);

        // Act
        var result = await _service.DetectAsync(input, requestId);

        // Assert
        Assert.False(result);
        Assert.NotNull(capturedRequest);

        var requestBody = await capturedRequest.Content.ReadAsStringAsync();
        var payload = JsonSerializer.Deserialize<Payload>(requestBody);

        Assert.NotNull(payload);
        Assert.Equal(requestId, payload.OriginAspNetRequestId);
        Assert.Empty(payload.Contents);
    }

    #endregion

    #region Interface Compliance Tests

    [Fact]
    public void RemoteModerationService_ImplementsIRemoteModerationService()
    {
        // Act & Assert
        Assert.IsAssignableFrom<IRemoteModerationService>(_service);
    }

    [Fact]
    public async Task IRemoteModerationService_MaskAsyncMethod_WorksCorrectly()
    {
        // Arrange
        const string input = "test input";
        const string requestId = "test-id";
        const string maskedText = "masked input";

        var expectedResponse = new ModeratorResponse
        {
            ProcessedContents = new[]
            {
                new ProcessedContent { Id = "0", ProcessedText = maskedText }
            }
        };

        var responseJson = JsonSerializer.Serialize(expectedResponse);
        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, System.Text.Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponseMessage);

        IRemoteModerationService service = _service;

        // Act
        var result = await service.MaskAsync(input, requestId);

        // Assert
        Assert.Equal(maskedText, result);
    }

    [Fact]
    public async Task IRemoteModerationService_DetectAsyncMethod_WorksCorrectly()
    {
        // Arrange
        var input = new[] { "test input" };
        const string requestId = "test-id";

        var expectedResponse = new ModeratorResponse
        {
            Positive = true
        };

        var responseJson = JsonSerializer.Serialize(expectedResponse);
        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, System.Text.Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponseMessage);

        IRemoteModerationService service = _service;

        // Act
        var result = await service.DetectAsync(input, requestId);

        // Assert
        Assert.True(result);
    }

    #endregion

    #region Helper Methods

    private void VerifyErrorLogging(string expectedMessage, Exception expectedException = null)
    {
        if (expectedException != null)
        {
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedMessage)),
                    It.Is<Exception>(ex => ex == expectedException),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
        else
        {
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedMessage)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
    }

    #endregion

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _httpClient?.Dispose();
        }
    }
}