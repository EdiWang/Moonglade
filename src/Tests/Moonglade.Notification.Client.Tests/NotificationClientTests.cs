using Microsoft.Extensions.Logging;
using Moonglade.Configuration;
using Moq;
using Moq.Protected;
using NUnit.Framework;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Moonglade.Notification.Client.Tests
{
    [TestFixture]
    public class NotificationClientTests
    {
        private MockRepository _mockRepository;

        private Mock<ILogger<NotificationClient>> _mockLogger;
        private Mock<IBlogConfig> _mockBlogConfig;
        private Mock<HttpMessageHandler> _handlerMock;
        private HttpClient _magicHttpClient;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);

            _handlerMock = _mockRepository.Create<HttpMessageHandler>();
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
                    Content = new StringContent("")
                })
                .Verifiable();

            _mockLogger = _mockRepository.Create<ILogger<NotificationClient>>();
            _mockBlogConfig = _mockRepository.Create<IBlogConfig>();
            _magicHttpClient = new(_handlerMock.Object);
        }

        private NotificationClient CreateNotificationClient()
        {
            _mockBlogConfig.Setup(p => p.NotificationSettings).Returns(new NotificationSettings()
            {
                EnableEmailSending = true,
                AzureFunctionEndpoint = "https://996.icu/fubao",
                EmailDisplayName = "996"
            });

            _mockBlogConfig.Setup(p => p.GeneralSettings).Returns(new GeneralSettings()
            {
                OwnerEmail = "fubao@996.icu"
            });

            return new(
                _mockLogger.Object,
                _mockBlogConfig.Object,
                _magicHttpClient);
        }

        [Test]
        public async Task TestNotificationAsync_StateUnderTest_ExpectedBehavior()
        {
            var l = _mockRepository.Create<ILogger<TestNotificationHandler>>();
            var handler = new TestNotificationHandler(CreateNotificationClient(), l.Object);
            await handler.Handle(new(), default);

            _handlerMock.Protected().Verify(
                "SendAsync",
                Times.Exactly(1),
                ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Post
                        && req.RequestUri == new Uri(_mockBlogConfig.Object.NotificationSettings.AzureFunctionEndpoint)
                ),
                ItExpr.IsAny<CancellationToken>()
            );
        }

        [Test]
        public void TestNotificationAsync_Exception()
        {
            _handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.InternalServerError,
                    Content = new StringContent("Oh shit")
                })
                .Verifiable();

            var l = _mockRepository.Create<ILogger<TestNotificationHandler>>();
            var handler = new TestNotificationHandler(CreateNotificationClient(), l.Object);

            Assert.ThrowsAsync<Exception>(async () =>
            {
                await handler.Handle(new(), default);
            });

            _handlerMock.Protected().Verify(
                "SendAsync",
                Times.Exactly(1),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post
                    && req.RequestUri == new Uri(_mockBlogConfig.Object.NotificationSettings.AzureFunctionEndpoint)
                ),
                ItExpr.IsAny<CancellationToken>()
            );
        }

        [Test]
        public async Task NotifyCommentAsync_StateUnderTest_ExpectedBehavior()
        {
            var notificationClient = CreateNotificationClient();
            string username = "Fubao";
            string email = "fubao@996.icu";
            string ipAddress = "9.9.6.007";
            string postTitle = "Work 996 and get into ICU";
            string commentContent = "This is fubao";
            DateTime createTimeUtc = default(DateTime);

            await notificationClient.NotifyCommentAsync(
                username,
                email,
                ipAddress,
                postTitle,
                commentContent,
                createTimeUtc);

            _handlerMock.Protected().Verify(
                "SendAsync",
                Times.Exactly(1),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post
                    && req.RequestUri == new Uri(_mockBlogConfig.Object.NotificationSettings.AzureFunctionEndpoint)
                ),
                ItExpr.IsAny<CancellationToken>()
            );
        }

        [Test]
        public async Task NotifyCommentReplyAsync_StateUnderTest_ExpectedBehavior()
        {
            var notificationClient = CreateNotificationClient();
            string email = "fubao@996.icu";
            string commentContent = "This is fubao";
            string title = "Work 996 and get into ICU";
            string replyContentHtml = "<p>Jack Ma's fubao</p>";
            string postLink = "https://996.icu/fuck-jack-ma";

            await notificationClient.NotifyCommentReplyAsync(
                email,
                commentContent,
                title,
                replyContentHtml,
                postLink);

            _handlerMock.Protected().Verify(
                "SendAsync",
                Times.Exactly(1),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post
                    && req.RequestUri == new Uri(_mockBlogConfig.Object.NotificationSettings.AzureFunctionEndpoint)
                ),
                ItExpr.IsAny<CancellationToken>()
            );
        }

        [Test]
        public async Task NotifyPingbackAsync_StateUnderTest_ExpectedBehavior()
        {
            var notificationClient = CreateNotificationClient();
            string targetPostTitle = "Work 996 and get into ICU";
            DateTime pingTimeUtc = default(DateTime);
            string domain = "996.icu";
            string sourceIp = "9.9.6.007";
            string sourceUrl = "https://996.icu/fuck-jack-ma";
            string sourceTitle = "996 is Fubao";

            await notificationClient.NotifyPingbackAsync(
                targetPostTitle,
                pingTimeUtc,
                domain,
                sourceIp,
                sourceUrl,
                sourceTitle);

            _handlerMock.Protected().Verify(
                "SendAsync",
                Times.Exactly(1),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post
                    && req.RequestUri == new Uri(_mockBlogConfig.Object.NotificationSettings.AzureFunctionEndpoint)
                ),
                ItExpr.IsAny<CancellationToken>()
            );
        }
    }
}
