using System.Net.Http;
using Microsoft.Extensions.Logging;
using Moq;
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

        //[Test]
        //public async Task ExamineSourceAsync_StateUnderTest_ExpectedBehavior()
        //{
        //    // Arrange
        //    var pingSourceInspector = this.CreatePingSourceInspector();
        //    string sourceUrl = null;
        //    string targetUrl = null;
        //    int timeoutSeconds = 0;

        //    // Act
        //    var result = await pingSourceInspector.ExamineSourceAsync(
        //        sourceUrl,
        //        targetUrl,
        //        timeoutSeconds);

        //    // Assert
        //    Assert.Fail();
        //    this.mockRepository.VerifyAll();
        //}
    }
}
