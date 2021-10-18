using Microsoft.Extensions.Logging;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;
using Moq;
using NUnit.Framework;
using System.Linq.Expressions;

namespace Moonglade.Pingback.Tests
{
    [TestFixture]
    public class ReceivePingCommandHandlerTests
    {
        private MockRepository _mockRepository;

        private Mock<ILogger<ReceivePingCommandHandler>> _mockLogger;
        private Mock<IPingSourceInspector> _mockPingSourceInspector;
        private Mock<IRepository<PingbackEntity>> _mockPingbackRepo;
        private Mock<IRepository<PostEntity>> _mockPostRepo;

        private string _fakePingRequest;

        [SetUp]
        public void Setup()
        {
            _mockRepository = new(MockBehavior.Default);

            _mockLogger = _mockRepository.Create<ILogger<ReceivePingCommandHandler>>();
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

        private ReceivePingCommandHandler CreateHandler() =>
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
            var handler = CreateHandler();

            var result = await handler.Handle(new(requestBody, "10.0.0.1", null), default);
            return result;
        }

        [Test]
        public async Task ProcessReceivedPayload_NoMethod()
        {
            var handler = CreateHandler();

            var result = await handler.Handle(new("<hello></hello>", "10.0.0.1", null), default);
            Assert.AreEqual(result, PingbackResponse.InvalidPingRequest);
        }

        [Test]
        public async Task ProcessReceivedPayload_InvalidRequest()
        {
            var tcs = new TaskCompletionSource<PingRequest>();
            tcs.SetResult(null);

            _mockPingSourceInspector
                .Setup(p => p.ExamineSourceAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(tcs.Task);

            var handler = CreateHandler();

            var result = await handler.Handle(new(_fakePingRequest, "10.0.0.1", null), default);
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

            var handler = CreateHandler();

            var result = await handler.Handle(new(_fakePingRequest, "10.0.0.1", null), default);
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

            var handler = CreateHandler();

            var result = await handler.Handle(new(_fakePingRequest, "10.0.0.1", null), default);
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

            var handler = CreateHandler();

            var result = await handler.Handle(new(_fakePingRequest, "10.0.0.1", null), default);
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

            var handler = CreateHandler();

            var result = await handler.Handle(new(_fakePingRequest, "10.0.0.1", null), default);
            Assert.AreEqual(PingbackResponse.Error48PingbackAlreadyRegistered, result);
        }

        [Test]
        public async Task DeletePingbackHistory_OK()
        {
            var handler = new DeletePingbackCommandHandler(_mockPingbackRepo.Object);
            await handler.Handle(new(Guid.Empty), default);

            _mockPingbackRepo.Verify(p => p.DeleteAsync(Guid.Empty));
        }

        [Test]
        public async Task GetPingbackHistoryAsync_OK()
        {
            IReadOnlyList<PingbackEntity> list = new List<PingbackEntity>
            {
                new()
                {
                    Id = Guid.Empty,
                    SourceUrl = "https://996.icu/sick",
                    SourceTitle = "Work 996",
                    SourceIp = "35.251.7.4",
                    TargetPostId = Guid.Empty,
                    Domain = "996.icu",
                    PingTimeUtc = DateTime.UtcNow,
                    TargetPostTitle = "Sick ICU"
                }
            };

            _mockPingbackRepo.Setup(p => p.GetAsync()).Returns(Task.FromResult(list));

            var handler = new GetPingbacksQueryHandler(_mockPingbackRepo.Object);
            var data = await handler.Handle(new(), default);

            Assert.IsNotNull(data);
            _mockPingbackRepo.Verify(p => p.GetAsync());
        }
    }
}
