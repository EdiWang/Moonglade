using System.Net;
using Moq;
using Moq.Protected;
using NUnit.Framework;

namespace Moonglade.Pingback.Tests;

[TestFixture]
public class PingbackWebRequestTests
{
    private MockRepository _mockRepository;
    private Mock<HttpMessageHandler> _handlerMock;
    private HttpClient _magicHttpClient;

    [SetUp]
    public void SetUp()
    {
        this._mockRepository = new(MockBehavior.Default);
        _handlerMock = _mockRepository.Create<HttpMessageHandler>();
        _magicHttpClient = new(_handlerMock.Object);
    }

    private PingbackWebRequest CreatePingbackWebRequest()
    {
        return new PingbackWebRequest(_magicHttpClient);
    }

    [Test]
    public void BuildHttpWebRequest_StateUnderTest_ExpectedBehavior()
    {
        // Arrange
        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("OK"),
                Headers =
                {
                    { "x-pingback", "https://greenhat.today/ping" }
                }
            })
            .Verifiable();


        var pingbackWebRequest = this.CreatePingbackWebRequest();
        Uri sourceUrl = new Uri("https://251.today/hw/copy-cat");
        Uri targetUrl = new Uri("https://greenhat.today/papapa");
        Uri url = new Uri("https://996.icu/ping");

        // Act
        var result = pingbackWebRequest.Send(sourceUrl, targetUrl, url);
        Assert.IsNotNull(result);
    }
}