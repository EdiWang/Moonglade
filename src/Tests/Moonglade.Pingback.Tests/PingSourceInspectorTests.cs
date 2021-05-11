using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using NUnit.Framework;

namespace Moonglade.Pingback.Tests
{
    [TestFixture]
    public class PingSourceInspectorTests
    {
        private MockRepository _mockRepository;

        private Mock<ILogger<PingSourceInspector>> _mockLogger;
        private Mock<HttpMessageHandler> _handlerMock;
        private HttpClient _magicHttpClient;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);
            _mockLogger = _mockRepository.Create<ILogger<PingSourceInspector>>();
            _handlerMock = _mockRepository.Create<HttpMessageHandler>();
        }

        private PingSourceInspector CreatePingSourceInspector()
        {
            _magicHttpClient = new(_handlerMock.Object);
            return new(_mockLogger.Object, _magicHttpClient);
        }

        [Test]
        public async Task ExamineSourceAsync_StateUnderTest_ExpectedBehavior()
        {
            string sourceUrl = "https://996.icu/work-996-sick-icu";
            string targetUrl = "https://greenhat.today/programmers-special-gift";

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
                    Content = new StringContent($"<html>" +
                                                $"<head>" +
                                                $"<title>Programmer's Gift</title>" +
                                                $"</head>" +
                                                $"<body>Work 996 and have a <a href=\"{targetUrl}\">green hat</a>!</body>" +
                                                $"</html>")
                })
                .Verifiable();
            var pingSourceInspector = CreatePingSourceInspector();

            var result = await pingSourceInspector.ExamineSourceAsync(sourceUrl, targetUrl);
            Assert.IsFalse(result.ContainsHtml);
            Assert.IsTrue(result.SourceHasLink);
            Assert.AreEqual("Programmer's Gift", result.Title);
            Assert.AreEqual(targetUrl, result.TargetUrl);
            Assert.AreEqual(sourceUrl, result.SourceUrl);
        }
    }
}
