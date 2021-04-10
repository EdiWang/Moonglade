using Microsoft.Extensions.Logging;
using Moonglade.Configuration.Abstraction;
using Moonglade.Notification.Client;
using Moq;
using NUnit.Framework;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Moonglade.Configuration;
using Moq.Protected;

namespace Moonglade.Notification.Client.Tests
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
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
                    Content = new StringContent(""),
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
            var notificationClient = CreateNotificationClient();

            await notificationClient.TestNotificationAsync();

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

        //[Test]
        //public async Task NotifyCommentAsync_StateUnderTest_ExpectedBehavior()
        //{
        //    // Arrange
        //    var notificationClient = CreateNotificationClient();
        //    string username = null;
        //    string email = null;
        //    string ipAddress = null;
        //    string postTitle = null;
        //    string commentContent = null;
        //    DateTime createTimeUtc = default(DateTime);

        //    // Act
        //    await notificationClient.NotifyCommentAsync(
        //        username,
        //        email,
        //        ipAddress,
        //        postTitle,
        //        commentContent,
        //        createTimeUtc);

        //    // Assert
        //    Assert.Fail();
        //    _mockRepository.VerifyAll();
        //}

        //[Test]
        //public async Task NotifyCommentReplyAsync_StateUnderTest_ExpectedBehavior()
        //{
        //    // Arrange
        //    var notificationClient = CreateNotificationClient();
        //    string email = null;
        //    string commentContent = null;
        //    string title = null;
        //    string replyContentHtml = null;
        //    string postLink = null;

        //    // Act
        //    await notificationClient.NotifyCommentReplyAsync(
        //        email,
        //        commentContent,
        //        title,
        //        replyContentHtml,
        //        postLink);

        //    // Assert
        //    Assert.Fail();
        //    _mockRepository.VerifyAll();
        //}

        //[Test]
        //public async Task NotifyPingbackAsync_StateUnderTest_ExpectedBehavior()
        //{
        //    // Arrange
        //    var notificationClient = CreateNotificationClient();
        //    string targetPostTitle = null;
        //    DateTime pingTimeUtc = default(DateTime);
        //    string domain = null;
        //    string sourceIp = null;
        //    string sourceUrl = null;
        //    string sourceTitle = null;

        //    // Act
        //    await notificationClient.NotifyPingbackAsync(
        //        targetPostTitle,
        //        pingTimeUtc,
        //        domain,
        //        sourceIp,
        //        sourceUrl,
        //        sourceTitle);

        //    // Assert
        //    Assert.Fail();
        //    _mockRepository.VerifyAll();
        //}
    }
}
