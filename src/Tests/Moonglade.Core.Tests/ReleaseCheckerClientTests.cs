using System;
using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moonglade.Core;
using Moq;
using NUnit.Framework;
using System.Net.Http;
using System.Threading.Tasks;

namespace Moonglade.Core.Tests
{
    [TestFixture]
    public class ReleaseCheckerClientTests
    {
        private MockRepository _mockRepository;

        private Mock<IConfiguration> _mockConfiguration;
        private Mock<HttpClient> _mockHttpClient;
        private Mock<ILogger<ReleaseCheckerClient>> _mockLogger;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);

            _mockConfiguration = _mockRepository.Create<IConfiguration>();
            _mockHttpClient = _mockRepository.Create<HttpClient>();
            _mockLogger = _mockRepository.Create<ILogger<ReleaseCheckerClient>>();
        }

        private ReleaseCheckerClient CreateReleaseCheckerClient()
        {
            return new(
                _mockConfiguration.Object,
                _mockHttpClient.Object,
                _mockLogger.Object);
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
    }
}
