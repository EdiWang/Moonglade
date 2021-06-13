using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moonglade.Configuration;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Notification.Client;
using Moonglade.Pingback;
using Moonglade.Web.Controllers;
using Moq;
using NUnit.Framework;

namespace Moonglade.Web.Tests.Controllers
{
    [TestFixture]
    public class PingbackControllerTests
    {
        private MockRepository _mockRepository;

        private Mock<ILogger<PingbackController>> _mockLogger;
        private Mock<IBlogConfig> _mockBlogConfig;
        private Mock<IPingbackService> _mockPingbackService;
        private Mock<IBlogNotificationClient> _mockBlogNotificationClient;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Loose);

            _mockLogger = _mockRepository.Create<ILogger<PingbackController>>();
            _mockBlogConfig = _mockRepository.Create<IBlogConfig>();
            _mockPingbackService = _mockRepository.Create<IPingbackService>();
            _mockBlogNotificationClient = _mockRepository.Create<IBlogNotificationClient>();
        }

        private PingbackController CreatePingbackController()
        {
            return new(
                _mockLogger.Object,
                _mockBlogConfig.Object,
                _mockPingbackService.Object,
                _mockBlogNotificationClient.Object);
        }

        [Test]
        public async Task Process_PingbackDisabled()
        {
            _mockBlogConfig.Setup(p => p.AdvancedSettings).Returns(new AdvancedSettings
            {
                EnablePingBackReceive = false
            });

            var pingbackController = CreatePingbackController();
            var result = await pingbackController.Process();
            Assert.IsInstanceOf(typeof(ForbidResult), result);
        }

        [Test]
        public async Task Process_OK()
        {
            _mockBlogConfig.Setup(p => p.AdvancedSettings).Returns(new AdvancedSettings
            {
                EnablePingBackReceive = true
            });

            _mockPingbackService
                .Setup(p => p.ReceivePingAsync(It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<Action<PingbackEntity>>())).Returns(Task.FromResult(PingbackResponse.Success));

            var pingbackController = CreatePingbackController();
            pingbackController.ControllerContext = new()
            {
                HttpContext = new DefaultHttpContext()
            };

            var result = await pingbackController.Process();
            Assert.IsInstanceOf(typeof(PingbackResult), result);
        }

        [Test]
        public async Task Delete_Success()
        {
            var mockBlogAudit = new Mock<IBlogAudit>();
            var pingbackController = CreatePingbackController();

            var result = await pingbackController.Delete(Guid.Empty, mockBlogAudit.Object);
            Assert.IsInstanceOf(typeof(OkResult), result);
        }
    }
}
