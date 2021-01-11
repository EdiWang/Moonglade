using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moonglade.Pingback;
using Moq;
using NUnit.Framework;

namespace Moonglade.Tests
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class PingbackServiceTests
    {
        private Mock<ILogger<PingbackService>> _loggerMock;
        private Mock<IDbConnection> _dbConnectionMock;
        private Mock<IPingSourceInspector> _pingSourceInspectorMock;
        private Mock<IPingbackRepository> _pingTargetFinderMock;

        private string _fakePingRequest;

        [SetUp]
        public void Setup()
        {
            _loggerMock = new();
            _dbConnectionMock = new();
            _pingSourceInspectorMock = new();
            _pingTargetFinderMock = new();
            _fakePingRequest = @"<?xml version=""1.0"" encoding=""iso-8859-1""?>
                                <methodCall>
                                  <methodName>pingback.ping</methodName>
                                  <params>
                                    <param><value><string>https://sourceurl</string></value></param>
                                    <param><value><string>https://targeturl</string></value></param>
                                  </params>
                                </methodCall>";
        }

        [TestCase(" ", ExpectedResult = PingbackResponse.GenericError)]
        [TestCase("", ExpectedResult = PingbackResponse.GenericError)]
        [TestCase(null, ExpectedResult = PingbackResponse.GenericError)]
        public async Task<PingbackResponse> ProcessReceivedPayload_EmptyRequest(string requestBody)
        {
            var pingbackService = new PingbackService(
                _loggerMock.Object,
                _dbConnectionMock.Object,
                _pingSourceInspectorMock.Object,
                _pingTargetFinderMock.Object);

            var result = await pingbackService.ReceivePingAsync(requestBody, "10.0.0.1", null);
            return result;
        }

        [Test]
        public async Task ProcessReceivedPayload_NoMethod()
        {
            var pingbackService = new PingbackService(
                _loggerMock.Object,
                _dbConnectionMock.Object,
                _pingSourceInspectorMock.Object,
                _pingTargetFinderMock.Object);

            var result = await pingbackService.ReceivePingAsync("<hello></hello>", "10.0.0.1", null);
            Assert.AreEqual(result, PingbackResponse.InvalidPingRequest);
        }

        [Test]
        public async Task ProcessReceivedPayload_InvalidRequest()
        {
            var tcs = new TaskCompletionSource<PingRequest>();
            tcs.SetResult(null);

            _pingSourceInspectorMock
                .Setup(p => p.ExamineSourceAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>())).Returns(tcs.Task);

            var pingbackService = new PingbackService(
                _loggerMock.Object,
                _dbConnectionMock.Object,
                _pingSourceInspectorMock.Object,
                _pingTargetFinderMock.Object);

            var result = await pingbackService.ReceivePingAsync(_fakePingRequest, "10.0.0.1", null);
            Assert.AreEqual(result, PingbackResponse.InvalidPingRequest);
        }

        [Test]
        public async Task ProcessReceivedPayload_Error17()
        {
            var tcs = new TaskCompletionSource<PingRequest>();
            tcs.SetResult(new()
            {
                SourceHasLink = false
            });

            _pingSourceInspectorMock
                .Setup(p => p.ExamineSourceAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>())).Returns(tcs.Task);

            var pingbackService = new PingbackService(
                _loggerMock.Object,
                _dbConnectionMock.Object,
                _pingSourceInspectorMock.Object,
                _pingTargetFinderMock.Object);

            var result = await pingbackService.ReceivePingAsync(_fakePingRequest, "10.0.0.1", null);
            Assert.AreEqual(result, PingbackResponse.Error17SourceNotContainTargetUri);
        }

        [Test]
        public async Task ProcessReceivedPayload_Spam()
        {
            var tcs = new TaskCompletionSource<PingRequest>();
            tcs.SetResult(new()
            {
                SourceHasLink = true,
                ContainsHtml = true
            });

            _pingSourceInspectorMock
                .Setup(p => p.ExamineSourceAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>())).Returns(tcs.Task);

            var pingbackService = new PingbackService(
                _loggerMock.Object,
                _dbConnectionMock.Object,
                _pingSourceInspectorMock.Object,
                _pingTargetFinderMock.Object);

            var result = await pingbackService.ReceivePingAsync(_fakePingRequest, "10.0.0.1", null);
            Assert.AreEqual(result, PingbackResponse.SpamDetectedFakeNotFound);
        }

        [Test]
        public async Task ProcessReceivedPayload_TargetNotFound()
        {
            var tcsPr = new TaskCompletionSource<PingRequest>();
            tcsPr.SetResult(new()
            {
                SourceHasLink = true,
                ContainsHtml = false
            });

            var tcsPt = new TaskCompletionSource<(Guid Id, string Title)>();
            tcsPt.SetResult((Guid.Empty, string.Empty));

            _pingSourceInspectorMock
                .Setup(p => p.ExamineSourceAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>())).Returns(tcsPr.Task);
            _pingTargetFinderMock.Setup(p => p.GetPostIdTitle(It.IsAny<string>(), It.IsAny<IDbConnection>())).Returns(tcsPt.Task);

            var pingbackService = new PingbackService(
                _loggerMock.Object,
                _dbConnectionMock.Object,
                _pingSourceInspectorMock.Object,
                _pingTargetFinderMock.Object);

            var result = await pingbackService.ReceivePingAsync(_fakePingRequest, "10.0.0.1", null);
            Assert.AreEqual(result, PingbackResponse.Error32TargetUriNotExist);
        }

        [Test]
        public async Task ProcessReceivedPayload_AlreadyPinged()
        {
            var tcsPr = new TaskCompletionSource<PingRequest>();
            tcsPr.SetResult(new()
            {
                SourceHasLink = true,
                ContainsHtml = false
            });

            _pingSourceInspectorMock
                .Setup(p => p.ExamineSourceAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .Returns(tcsPr.Task);

            var tcsPt = new TaskCompletionSource<(Guid Id, string Title)>();
            tcsPt.SetResult((Guid.NewGuid(), "Pingback Unit Test"));
            _pingTargetFinderMock.Setup(p => p.GetPostIdTitle(It.IsAny<string>(), It.IsAny<IDbConnection>())).Returns(tcsPt.Task);

            var tcsAp = new TaskCompletionSource<bool>();
            tcsAp.SetResult(true);
            _pingTargetFinderMock.Setup(p => p.HasAlreadyBeenPinged(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IDbConnection>()))
                .Returns(tcsAp.Task);

            var pingbackService = new PingbackService(
                _loggerMock.Object,
                _dbConnectionMock.Object,
                _pingSourceInspectorMock.Object,
                _pingTargetFinderMock.Object);

            var result = await pingbackService.ReceivePingAsync(_fakePingRequest, "10.0.0.1", null);
            Assert.AreEqual(result, PingbackResponse.Error48PingbackAlreadyRegistered);
        }
    }
}
