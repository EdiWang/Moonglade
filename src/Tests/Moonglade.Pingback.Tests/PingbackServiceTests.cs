using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;
using Moq;
using NUnit.Framework;

namespace Moonglade.Pingback.Tests
{
    [TestFixture]
    public class PingbackServiceTests
    {
        private MockRepository _mockRepository;

        private Mock<ILogger<PingbackService>> _mockLogger;
        private Mock<IPingSourceInspector> _mockPingSourceInspector;
        private Mock<IRepository<PingbackEntity>> _mockPingbackRepo;
        private Mock<IRepository<PostEntity>> _mockPostRepo;

        private string _fakePingRequest;

        [SetUp]
        public void Setup()
        {
            _mockRepository = new(MockBehavior.Default);

            _mockLogger = _mockRepository.Create<ILogger<PingbackService>>();
            _mockPingSourceInspector = _mockRepository.Create<IPingSourceInspector>();
            _mockPingbackRepo = _mockRepository.Create<IRepository<PingbackEntity>>();
            _mockPostRepo = _mockRepository.Create<IRepository<PostEntity>>();

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
                _mockPingSourceInspector.Object,
                _mockPingbackRepo.Object,
                _mockPostRepo.Object);


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
                TargetUrl = "https://996.icu/post/1996/3/5/work-996-sick-icu",
                SourceHasLink = true,
                ContainsHtml = false
            });

            _mockPingSourceInspector
                .Setup(p => p.ExamineSourceAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(tcsPr.Task);

            _mockPostRepo
                .Setup(p => p.SelectFirstOrDefaultAsync(It.IsAny<PostSpec>(), x => new Tuple<Guid, string>(x.Id, x.Title)))
                .Returns(Task.FromResult(new Tuple<Guid, string>(Guid.Empty, string.Empty)));

            var pingbackService = CreateService();

            var result = await pingbackService.ReceivePingAsync(_fakePingRequest, "10.0.0.1", null);
            Assert.AreEqual(PingbackResponse.Error32TargetUriNotExist, result);
        }

        [Test]
        public async Task ProcessReceivedPayload_AlreadyPinged()
        {
            var tcsPr = new TaskCompletionSource<PingRequest>();
            tcsPr.SetResult(new()
            {
                TargetUrl = "https://996.icu/post/1996/3/5/work-996-sick-icu",
                SourceHasLink = true,
                ContainsHtml = false
            });

            _mockPingSourceInspector
                .Setup(p => p.ExamineSourceAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(tcsPr.Task);

            _mockPostRepo
                .Setup(p => p.SelectFirstOrDefaultAsync(It.IsAny<PostSpec>(), x => new Tuple<Guid, string>(x.Id, x.Title)))
                .Returns(Task.FromResult(new Tuple<Guid, string>(Guid.NewGuid(), "Pingback Unit Test")));

            _mockPingbackRepo.Setup(p => p.Any(It.IsAny<Expression<Func<PingbackEntity, bool>>>()))
                .Returns(true);

            var pingbackService = CreateService();

            var result = await pingbackService.ReceivePingAsync(_fakePingRequest, "10.0.0.1", null);
            Assert.AreEqual(PingbackResponse.Error48PingbackAlreadyRegistered, result);
        }

        [Test]
        public async Task DeletePingbackHistory_OK()
        {
            var pingbackService = CreateService();
            await pingbackService.DeletePingbackHistory(Guid.Empty);

            _mockPingbackRepo.Verify(p => p.DeleteAsync(Guid.Empty));
        }
    }
}
