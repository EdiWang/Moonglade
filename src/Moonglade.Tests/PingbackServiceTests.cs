using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Edi.Blog.Pingback;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moonglade.Core;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Notification;
using Moq;
using NUnit.Framework;

namespace Moonglade.Tests
{
    [TestFixture]
    public class PingbackServiceTests
    {
        private Mock<ILogger<PingbackService>> _loggerMock;
        private Mock<IMoongladeNotification> _notificationMock;
        private Mock<IRepository<PingbackHistoryEntity>> _pingbackRepositoryMock;

        [SetUp]
        public void Setup()
        {
            _loggerMock = new Mock<ILogger<PingbackService>>();
            _notificationMock = new Mock<IMoongladeNotification>();
            _pingbackRepositoryMock = new Mock<IRepository<PingbackHistoryEntity>>();
        }

        [TestCase(PingbackValidationResult.GenericError, ExpectedResult = PingbackServiceResponse.InvalidPingRequest)]
        [TestCase(PingbackValidationResult.TerminatedMethodNotFound, ExpectedResult = PingbackServiceResponse.InvalidPingRequest)]
        [TestCase(PingbackValidationResult.TerminatedUrlNotFound, ExpectedResult = PingbackServiceResponse.InvalidPingRequest)]
        public async Task<PingbackServiceResponse> TestProcessReceivedPingbackInvalidRequest(PingbackValidationResult result)
        {
            var tcs = new TaskCompletionSource<PingbackValidationResult>();
            tcs.SetResult(result);

            var pingbackReceiverMock = new Mock<IPingbackReceiver>();
            pingbackReceiverMock.Setup(p => p.ValidatePingRequest(It.IsAny<HttpContext>())).Returns(tcs.Task);

            //var postMock = new Mock<PostService>(MockBehavior.Loose);
            var httpContextMock = new Mock<HttpContext>();

            var svc = new PingbackService(
                _loggerMock.Object,
                _notificationMock.Object,
                null,
                pingbackReceiverMock.Object,
                _pingbackRepositoryMock.Object);

            return await svc.ProcessReceivedPingback(httpContextMock.Object);
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
                _notificationMock.Object,
                null,
                pingbackReceiverMock.Object,
                _pingbackRepositoryMock.Object);

            var response = svc.DeleteReceivedPingback(Guid.NewGuid());
            return response.IsSuccess;
        }
    }
}
