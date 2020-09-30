using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moonglade.Model;
using Moonglade.Pingback;
using Moq;
using NUnit.Framework;

namespace Moonglade.Tests
{
    [TestFixture]
    public class PingbackServiceTests
    {
        private Mock<ILogger<PingbackService>> _loggerMock;
        private Mock<IConfiguration> _iconfiguration;

        [SetUp]
        public void Setup()
        {
            _loggerMock = new Mock<ILogger<PingbackService>>();
            _iconfiguration = new Mock<IConfiguration>();
        }

        [TestCase(" ", ExpectedResult = PingbackResponse.GenericError)]
        [TestCase("", ExpectedResult = PingbackResponse.GenericError)]
        [TestCase(null, ExpectedResult = PingbackResponse.GenericError)]
        public async Task<PingbackResponse> TestProcessReceivedPayloadAsyncEmptyRequest(string requestBody)
        {
            var pingbackService = new PingbackService(_loggerMock.Object, _iconfiguration.Object);
            var result = await pingbackService.ProcessReceivedPayloadAsync(requestBody, "10.0.0.1", null);
            return result;
        }
    }
}
