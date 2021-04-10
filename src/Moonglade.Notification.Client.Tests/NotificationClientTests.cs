using Microsoft.Extensions.Logging;
using Moonglade.Configuration.Abstraction;
using Moonglade.Notification.Client;
using Moq;
using NUnit.Framework;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Threading.Tasks;

namespace Moonglade.Notification.Client.Tests
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class NotificationClientTests
    {
        private MockRepository _mockRepository;

        private Mock<ILogger<NotificationClient>> _mockLogger;
        private Mock<IBlogConfig> _mockBlogConfig;
        private Mock<HttpClient> _mockHttpClient;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);

            _mockLogger = _mockRepository.Create<ILogger<NotificationClient>>();
            _mockBlogConfig = _mockRepository.Create<IBlogConfig>();
            _mockHttpClient = _mockRepository.Create<HttpClient>();
        }

        private NotificationClient CreateNotificationClient()
        {
            return new(
                _mockLogger.Object,
                _mockBlogConfig.Object,
                _mockHttpClient.Object);
        }

        //[Test]
        //public async Task TestNotificationAsync_StateUnderTest_ExpectedBehavior()
        //{
        //    // Arrange
        //    var notificationClient = CreateNotificationClient();

        //    // Act
        //    await notificationClient.TestNotificationAsync();

        //    // Assert
        //    Assert.Fail();
        //    _mockRepository.VerifyAll();
        //}

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
