using System;
using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moonglade.Core;
using Moq;
using NUnit.Framework;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Moq.Protected;

namespace Moonglade.Core.Tests
{
    [TestFixture]
    public class ReleaseCheckerClientTests
    {
        private MockRepository _mockRepository;

        private Mock<IConfiguration> _mockConfiguration;
        private Mock<HttpMessageHandler> _handlerMock;
        private Mock<ILogger<ReleaseCheckerClient>> _mockLogger;
        private HttpClient _magicHttpClient;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);

            _mockConfiguration = _mockRepository.Create<IConfiguration>();
            _handlerMock = _mockRepository.Create<HttpMessageHandler>();
            _mockLogger = _mockRepository.Create<ILogger<ReleaseCheckerClient>>();
        }

        private ReleaseCheckerClient CreateReleaseCheckerClient()
        {
            _magicHttpClient = new(_handlerMock.Object);
            return new(_mockConfiguration.Object, _magicHttpClient, _mockLogger.Object);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void CheckNewReleaseAsync_EmptyApiAddress(string apiAddress)
        {
            _mockConfiguration.Setup(p => p["ReleaseCheckApiAddress"]).Returns(apiAddress);

            Assert.Throws<InvalidOperationException>(() =>
            {
                var releaseCheckerClient = CreateReleaseCheckerClient();
            });
        }

        [Test]
        public void CheckNewReleaseAsync_BadApiAddress()
        {
            _mockConfiguration.Setup(p => p["ReleaseCheckApiAddress"]).Returns("!@$#@%^$#996");

            Assert.Throws<InvalidOperationException>(() =>
            {
                var releaseCheckerClient = CreateReleaseCheckerClient();
            });
        }

        [Test]
        public void CheckNewReleaseAsync_UnsuccessResponse()
        {
            _mockConfiguration.Setup(p => p["ReleaseCheckApiAddress"]).Returns("https://996.icu");

            _handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Content = new StringContent("")
                })
                .Verifiable();

            var releaseCheckerClient = CreateReleaseCheckerClient();

            Assert.ThrowsAsync<Exception>(async () =>
            {
                var result = await releaseCheckerClient.CheckNewReleaseAsync();
            });
        }
    }
}
