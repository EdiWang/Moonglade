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
        private Mock<IConfiguration> _configurationMock;
        private Mock<IPingSourceInspector> _pingSourceInspectorMock;
        private Mock<IPingTargetFinder> _pingTargetFinderMock;

        private string _fakePingRequest;

        [SetUp]
        public void Setup()
        {
            _loggerMock = new Mock<ILogger<PingbackService>>();
            _configurationMock = new Mock<IConfiguration>();
            _pingSourceInspectorMock = new Mock<IPingSourceInspector>();
            _pingTargetFinderMock = new Mock<IPingTargetFinder>();
            _fakePingRequest = @"<?xml version=""1.0"" encoding=""iso-8859-1""?>
                                <methodCall>
                                  <methodName>pingback.ping</methodName>
                                  <params>
                                    <param><value><string>https://targeturl</string></value></param>
	                                <param><value><string>https://sourceurl</string></value></param>
                                  </params>
                                </methodCall>";
        }

        [TestCase(" ", ExpectedResult = PingbackResponse.GenericError)]
        [TestCase("", ExpectedResult = PingbackResponse.GenericError)]
        [TestCase(null, ExpectedResult = PingbackResponse.GenericError)]
        public async Task<PingbackResponse> TestProcessReceivedPayloadAsyncEmptyRequest(string requestBody)
        {
            var pingbackService = new PingbackService(
                _loggerMock.Object,
                _configurationMock.Object,
                _pingSourceInspectorMock.Object,
                _pingTargetFinderMock.Object);

            var result = await pingbackService.ProcessReceivedPayloadAsync(requestBody, "10.0.0.1", null);
            return result;
        }

        [Test]
        public async Task TestProcessReceivedPayloadAsyncNoMethod()
        {
            var pingbackService = new PingbackService(
                _loggerMock.Object,
                _configurationMock.Object,
                _pingSourceInspectorMock.Object,
                _pingTargetFinderMock.Object);

            var result = await pingbackService.ProcessReceivedPayloadAsync("<hello></hello>", "10.0.0.1", null);
            Assert.AreEqual(result, PingbackResponse.InvalidPingRequest);
        }

        [Test]
        public async Task TestProcessReceivedPayloadAsyncInvalidRequest()
        {
            var tcs = new TaskCompletionSource<PingRequest>();
            tcs.SetResult(null);

            _pingSourceInspectorMock
                .Setup(p => p.ExamineSourceAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>())).Returns(tcs.Task);

            var pingbackService = new PingbackService(
                _loggerMock.Object,
                _configurationMock.Object,
                _pingSourceInspectorMock.Object,
                _pingTargetFinderMock.Object);

            var result = await pingbackService.ProcessReceivedPayloadAsync(_fakePingRequest, "10.0.0.1", null);
            Assert.AreEqual(result, PingbackResponse.InvalidPingRequest);
        }

        [Test]
        public async Task TestProcessReceivedPayloadAsyncError17()
        {
            var tcs = new TaskCompletionSource<PingRequest>();
            tcs.SetResult(new PingRequest
            {
                SourceDocumentInfo = new SourceDocumentInfo
                {
                    SourceHasLink = false
                }
            });

            _pingSourceInspectorMock
                .Setup(p => p.ExamineSourceAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>())).Returns(tcs.Task);

            var pingbackService = new PingbackService(
                _loggerMock.Object,
                _configurationMock.Object,
                _pingSourceInspectorMock.Object,
                _pingTargetFinderMock.Object);

            var result = await pingbackService.ProcessReceivedPayloadAsync(_fakePingRequest, "10.0.0.1", null);
            Assert.AreEqual(result, PingbackResponse.Error17SourceNotContainTargetUri);
        }

        [Test]
        public async Task TestProcessReceivedPayloadAsyncSpam()
        {
            var tcs = new TaskCompletionSource<PingRequest>();
            tcs.SetResult(new PingRequest
            {
                SourceDocumentInfo = new SourceDocumentInfo
                {
                    SourceHasLink = true,
                    ContainsHtml = true
                }
            });

            _pingSourceInspectorMock
                .Setup(p => p.ExamineSourceAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>())).Returns(tcs.Task);

            var pingbackService = new PingbackService(
                _loggerMock.Object,
                _configurationMock.Object,
                _pingSourceInspectorMock.Object,
                _pingTargetFinderMock.Object);

            var result = await pingbackService.ProcessReceivedPayloadAsync(_fakePingRequest, "10.0.0.1", null);
            Assert.AreEqual(result, PingbackResponse.SpamDetectedFakeNotFound);
        }
    }
}
