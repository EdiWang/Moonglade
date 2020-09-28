using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moonglade.Core;
using Moonglade.Core.Notification;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Pingback;
using Moq;
using NUnit.Framework;

namespace Moonglade.Tests.Core
{
    [TestFixture]
    public class PingbackServiceTests
    {
        private Mock<ILogger<PingbackService>> _loggerMock;
        private Mock<IBlogNotificationClient> _notificationMock;
        private Mock<IRepository<PingbackHistoryEntity>> _pingbackRepositoryMock;
        private Mock<IRepository<PostEntity>> _postRepositoryMock;

        [SetUp]
        public void Setup()
        {
            _loggerMock = new Mock<ILogger<PingbackService>>();
            _notificationMock = new Mock<IBlogNotificationClient>();
            _pingbackRepositoryMock = new Mock<IRepository<PingbackHistoryEntity>>();
            _postRepositoryMock = new Mock<IRepository<PostEntity>>();
        }

        [TestCase(-1, ExpectedResult = false)]
        [TestCase(0, ExpectedResult = false)]
        [TestCase(1, ExpectedResult = true)]
        public bool TestDeleteReceivedPingback(int deleteReturn)
        {
            _pingbackRepositoryMock.Setup(p => p.Delete(It.IsAny<Guid>())).Returns(deleteReturn);
            var pingbackReceiverMock = new Mock<IPingbackReceiver>();

            var svc = new PingbackService(
                _loggerMock.Object,
                pingbackReceiverMock.Object,
                _pingbackRepositoryMock.Object,
                _postRepositoryMock.Object,
                _notificationMock.Object);

            var response = svc.DeleteReceivedPingback(Guid.NewGuid());
            return response.IsSuccess;
        }
    }
}
