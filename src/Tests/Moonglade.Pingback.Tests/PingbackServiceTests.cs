using System;
using System.Data;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moq;
using NUnit.Framework;

namespace Moonglade.Pingback.Tests
{
    [TestFixture]
    public class PingbackServiceTests
    {
        private MockRepository _mockRepository;

        private Mock<ILogger<PingbackService>> _mockLogger;
        private Mock<IDbConnection> _mockDbConnection;
        private Mock<IPingSourceInspector> _mockPingSourceInspector;
        private Mock<IPingbackRepository> _mockPingTargetFinder;
        private Mock<IRepository<PingbackEntity>> _mockPingbackRepo;

        private string _fakePingRequest;

        [SetUp]
        public void Setup()
        {
            _mockRepository = new(MockBehavior.Default);

            _mockLogger = _mockRepository.Create<ILogger<PingbackService>>();
            _mockDbConnection = _mockRepository.Create<IDbConnection>();
            _mockPingSourceInspector = _mockRepository.Create<IPingSourceInspector>();
            _mockPingTargetFinder = _mockRepository.Create<IPingbackRepository>();
            _mockPingbackRepo = _mockRepository.Create<IRepository<PingbackEntity>>();

            _fakePingRequest = @"<?xml version=""1.0"" encoding=""iso-8859-1""?>
                                <methodCall>
                                  <methodName>pingback.ping</methodName>
                                  <params>
                                    <param><value><string>https://sourceurl</string></value></param>
                                    <param><value><string>https://targeturl</string></value></param>
                                  </params>
                                </methodCall>";
        }

        private PingbackService CreateService() =>
             new(
                _mockLogger.Object,
                _mockDbConnection.Object,
                _mockPingSourceInspector.Object,
                _mockPingTargetFinder.Object,
                _mockPingbackRepo.Object);


        [TestCase(" ", ExpectedResult = PingbackResponse.GenericError)]
        [TestCase("", ExpectedResult = PingbackResponse.GenericError)]
        [TestCase(null, ExpectedResult = PingbackResponse.GenericError)]
        public async Task<PingbackResponse> ProcessReceivedPayload_EmptyRequest(string requestBody)
        {
            var pingbackService = CreateService();

            var result = await pingbackService.ReceivePingAsync(requestBody, "10.0.0.1", null);
            return result;
        }

        [Test]
        public async Task ProcessReceivedPayload_NoMethod()
        {
            var pingbackService = CreateService();

            var result = await pingbackService.ReceivePingAsync("<hello></hello>", "10.0.0.1", null);
            Assert.AreEqual(result, PingbackResponse.InvalidPingRequest);
        }

        [Test]
        public async Task ProcessReceivedPayload_InvalidRequest()
        {
            var tcs = new TaskCompletionSource<PingRequest>();
            tcs.SetResult(null);

            _mockPingSourceInspector
                .Setup(p => p.ExamineSourceAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(tcs.Task);

            var pingbackService = CreateService();

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

            _mockPingSourceInspector
                .Setup(p => p.ExamineSourceAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(tcs.Task);

            var pingbackService = CreateService();

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

            _mockPingSourceInspector
                .Setup(p => p.ExamineSourceAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(tcs.Task);

            var pingbackService = CreateService();

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

            _mockPingSourceInspector
                .Setup(p => p.ExamineSourceAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(tcsPr.Task);
            _mockPingTargetFinder.Setup(p => p.GetPostIdTitle(It.IsAny<string>(), It.IsAny<IDbConnection>())).Returns(tcsPt.Task);

            var pingbackService = CreateService();

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

            _mockPingSourceInspector
                .Setup(p => p.ExamineSourceAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(tcsPr.Task);

            var tcsPt = new TaskCompletionSource<(Guid Id, string Title)>();
            tcsPt.SetResult((Guid.NewGuid(), "Pingback Unit Test"));
            _mockPingTargetFinder.Setup(p => p.GetPostIdTitle(It.IsAny<string>(), It.IsAny<IDbConnection>())).Returns(tcsPt.Task);

            _mockPingbackRepo.Setup(p => p.Any(It.IsAny<Expression<Func<PingbackEntity, bool>>>()))
                .Returns(true);

            var pingbackService = CreateService();

            var result = await pingbackService.ReceivePingAsync(_fakePingRequest, "10.0.0.1", null);
            Assert.AreEqual(result, PingbackResponse.Error48PingbackAlreadyRegistered);
        }
    }
}
