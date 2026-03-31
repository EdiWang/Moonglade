using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moonglade.Configuration;
using Moq;
using System.Net;

namespace Moonglade.Email.Client.Tests;

public class MoongladeEmailClientTests
{
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly Mock<ILogger<MoongladeEmailClient>> _loggerMock;
    private readonly Mock<IBlogConfig> _blogConfigMock;

    public MoongladeEmailClientTests()
    {
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _loggerMock = new Mock<ILogger<MoongladeEmailClient>>();
        _blogConfigMock = new Mock<IBlogConfig>();

        var httpContext = new DefaultHttpContext();
        _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(httpContext);

        _blogConfigMock.Setup(c => c.NotificationSettings).Returns(new NotificationSettings
        {
            EnableEmailSending = true
        });
    }

    private MoongladeEmailClient CreateClient(HttpClient httpClient, IConfiguration configuration = null)
    {
        configuration ??= CreateConfiguration("https://email.example.com", "X-Api-Key", "test-key");
        return new MoongladeEmailClient(_httpContextAccessorMock.Object, configuration, _loggerMock.Object, httpClient, _blogConfigMock.Object);
    }

    private static IConfiguration CreateConfiguration(string apiEndpoint, string apiKeyHeader = "X-Api-Key", string apiKey = "test-key")
    {
        var inMemory = new Dictionary<string, string>();
        if (apiEndpoint != null) inMemory["Email:ApiEndpoint"] = apiEndpoint;
        if (apiKeyHeader != null) inMemory["Email:ApiKeyHeader"] = apiKeyHeader;
        if (apiKey != null) inMemory["Email:ApiKey"] = apiKey;

        return new ConfigurationBuilder()
            .AddInMemoryCollection(inMemory)
            .Build();
    }

    private static HttpClient CreateMockHttpClient(HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var handler = new FakeHttpMessageHandler(statusCode);
        return new HttpClient(handler);
    }

    // --- Configuration / Disabled scenarios ---

    [Fact]
    public async Task SendEmailAsync_MissingApiEndpoint_ReturnsFalse()
    {
        var config = CreateConfiguration(null);
        var client = CreateClient(CreateMockHttpClient(), config);

        var result = await client.SendEmailAsync(MailMesageTypes.TestMail, ["a@b.com"], new { }, CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task SendEmailAsync_MissingApiKey_ReturnsFalse()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["Email:ApiEndpoint"] = "https://email.example.com"
            })
            .Build();

        var client = CreateClient(CreateMockHttpClient(), config);

        var result = await client.SendEmailAsync(MailMesageTypes.TestMail, ["a@b.com"], new { }, CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task SendEmailAsync_MissingApiKeyHeader_ReturnsFalse()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["Email:ApiEndpoint"] = "https://email.example.com",
                ["Email:ApiKey"] = "test-key"
            })
            .Build();

        var client = CreateClient(CreateMockHttpClient(), config);

        var result = await client.SendEmailAsync(MailMesageTypes.TestMail, ["a@b.com"], new { }, CancellationToken.None);

        Assert.False(result);
    }

    // --- Input validation ---

    [Fact]
    public async Task SendEmailAsync_NullRecipients_ReturnsFalse()
    {
        var client = CreateClient(CreateMockHttpClient());

        var result = await client.SendEmailAsync(MailMesageTypes.TestMail, null, new { }, CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task SendEmailAsync_EmptyRecipients_ReturnsFalse()
    {
        var client = CreateClient(CreateMockHttpClient());

        var result = await client.SendEmailAsync(MailMesageTypes.TestMail, [], new { }, CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task SendEmailAsync_NullPayload_ReturnsFalse()
    {
        var client = CreateClient(CreateMockHttpClient());

        var result = await client.SendEmailAsync<object>(MailMesageTypes.TestMail, ["a@b.com"], null, CancellationToken.None);

        Assert.False(result);
    }

    // --- Email sending disabled ---

    [Fact]
    public async Task SendEmailAsync_EmailSendingDisabled_ReturnsFalse()
    {
        _blogConfigMock.Setup(c => c.NotificationSettings).Returns(new NotificationSettings
        {
            EnableEmailSending = false
        });

        var client = CreateClient(CreateMockHttpClient());

        var result = await client.SendEmailAsync(MailMesageTypes.TestMail, ["a@b.com"], new { }, CancellationToken.None);

        Assert.False(result);
    }

    // --- Successful send ---

    [Fact]
    public async Task SendEmailAsync_SuccessfulResponse_ReturnsTrue()
    {
        var client = CreateClient(CreateMockHttpClient(HttpStatusCode.OK));

        var result = await client.SendEmailAsync(MailMesageTypes.TestMail, ["a@b.com"], new { Message = "Hello" }, CancellationToken.None);

        Assert.True(result);
    }

    // --- HTTP failure ---

    [Fact]
    public async Task SendEmailAsync_ServerError_ReturnsFalse()
    {
        var client = CreateClient(CreateMockHttpClient(HttpStatusCode.InternalServerError));

        var result = await client.SendEmailAsync(MailMesageTypes.TestMail, ["a@b.com"], new { }, CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task SendEmailAsync_Unauthorized_ReturnsFalse()
    {
        var client = CreateClient(CreateMockHttpClient(HttpStatusCode.Unauthorized));

        var result = await client.SendEmailAsync(MailMesageTypes.TestMail, ["a@b.com"], new { }, CancellationToken.None);

        Assert.False(result);
    }

    // --- HttpRequestException ---

    [Fact]
    public async Task SendEmailAsync_HttpRequestException_ReturnsFalse()
    {
        var handler = new FakeHttpMessageHandler(new HttpRequestException("Connection refused"));
        var httpClient = new HttpClient(handler);
        var client = CreateClient(httpClient);

        var result = await client.SendEmailAsync(MailMesageTypes.TestMail, ["a@b.com"], new { }, CancellationToken.None);

        Assert.False(result);
    }

    // --- TraceIdentifier ---

    [Fact]
    public async Task SendEmailAsync_NullHttpContext_DoesNotThrow()
    {
        _httpContextAccessorMock.Setup(a => a.HttpContext).Returns((HttpContext)null);

        var client = CreateClient(CreateMockHttpClient());

        var result = await client.SendEmailAsync(MailMesageTypes.TestMail, ["a@b.com"], new { }, CancellationToken.None);

        Assert.True(result);
    }

    // --- Verifies request goes to /api/enqueue ---

    [Fact]
    public async Task SendEmailAsync_PostsToEnqueueEndpoint()
    {
        var handler = new CapturingHttpMessageHandler(HttpStatusCode.OK);
        var httpClient = new HttpClient(handler);
        var client = CreateClient(httpClient);

        await client.SendEmailAsync(MailMesageTypes.NewCommentNotification, ["a@b.com"], new { Title = "Test" }, CancellationToken.None);

        Assert.NotNull(handler.CapturedRequest);
        Assert.Equal("/api/enqueue", handler.CapturedRequest.RequestUri?.AbsolutePath);
        Assert.Equal(HttpMethod.Post, handler.CapturedRequest.Method);
    }

    [Fact]
    public async Task SendEmailAsync_RequestBodyContainsCamelCaseJson()
    {
        var handler = new CapturingHttpMessageHandler(HttpStatusCode.OK);
        var httpClient = new HttpClient(handler);
        var client = CreateClient(httpClient);

        await client.SendEmailAsync(MailMesageTypes.TestMail, ["a@b.com"], new { TestProp = "value" }, CancellationToken.None);

        var body = handler.CapturedBody;
        Assert.NotNull(body);
        Assert.Contains("\"testProp\"", body);
        Assert.Contains("\"type\"", body);
        Assert.Contains("\"receipts\"", body);
    }
}

internal class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpStatusCode? _statusCode;
    private readonly Exception _exception;

    public FakeHttpMessageHandler(HttpStatusCode statusCode) => _statusCode = statusCode;

    public FakeHttpMessageHandler(Exception exception) => _exception = exception;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (_exception != null) throw _exception;
        return Task.FromResult(new HttpResponseMessage(_statusCode!.Value));
    }
}

internal class CapturingHttpMessageHandler(HttpStatusCode statusCode) : HttpMessageHandler
{
    public HttpRequestMessage CapturedRequest { get; private set; }
    public string CapturedBody { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        CapturedRequest = request;
        if (request.Content != null)
        {
            CapturedBody = await request.Content.ReadAsStringAsync(cancellationToken);
        }
        return new HttpResponseMessage(statusCode);
    }
}
