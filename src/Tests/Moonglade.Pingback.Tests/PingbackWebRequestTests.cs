using Moq;
using NUnit.Framework;

namespace Moonglade.Pingback.Tests;

[TestFixture]
public class PingbackWebRequestTests
{
    private MockRepository _mockRepository;

    [SetUp]
    public void SetUp()
    {
        this._mockRepository = new(MockBehavior.Default);
    }

    private PingbackWebRequest CreatePingbackWebRequest()
    {
        return new PingbackWebRequest();
    }

    [Test]
    public void BuildHttpWebRequest_StateUnderTest_ExpectedBehavior()
    {
        // Arrange
        var pingbackWebRequest = this.CreatePingbackWebRequest();
        Uri sourceUrl = new Uri("https://251.today/hw/copy-cat");
        Uri targetUrl = new Uri("https://greenhat.today/papapa");
        Uri url = new Uri("https://996.icu/ping");

        // Act
        var result = pingbackWebRequest.BuildHttpWebRequest(sourceUrl, targetUrl, url);

        Assert.IsNotNull(result);
        Assert.AreEqual(result.Address, url);
        Assert.AreEqual(result.ContentType, "text/xml");
    }
}