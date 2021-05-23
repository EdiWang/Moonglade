using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moonglade.Comments;
using Moonglade.Configuration;
using Moonglade.Notification.Client;
using Moonglade.Web.Controllers;
using Moq;
using NUnit.Framework;

namespace Moonglade.Web.Tests.Controllers
{
    [TestFixture]
    public class CommentControllerTests
    {
        private MockRepository _mockRepository;

        private Mock<ICommentService> _mockCommentService;
        private Mock<IBlogConfig> _mockBlogConfig;
        private Mock<IBlogNotificationClient> _mockBlogNotificationClient;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);

            _mockCommentService = _mockRepository.Create<ICommentService>();
            _mockBlogConfig = _mockRepository.Create<IBlogConfig>();
            _mockBlogNotificationClient = _mockRepository.Create<IBlogNotificationClient>();
        }

        private CommentController CreateCommentController()
        {
            return new(
                _mockCommentService.Object,
                _mockBlogConfig.Object,
                _mockBlogNotificationClient.Object);
        }

        [Test]
        public async Task List_Empty_PostId()
        {
            var ctl = CreateCommentController();
            var result = await ctl.List(Guid.Empty, null);

            Assert.IsInstanceOf<BadRequestResult>(result);
        }

        [Test]
        public async Task SetApprovalStatus_EmptyId()
        {
            var ctl = CreateCommentController();
            var result = await ctl.Approval(Guid.Empty);

            Assert.IsInstanceOf<BadRequestObjectResult>(result);
        }

        [Test]
        public async Task SetApprovalStatus_ValidId()
        {
            _mockCommentService.Setup(p => p.ToggleApprovalAsync(It.IsAny<Guid[]>()));
            var id = Guid.NewGuid();

            var ctl = CreateCommentController();
            var result = await ctl.Approval(id);

            Assert.IsInstanceOf<OkObjectResult>(result);
            Assert.AreEqual(id, ((OkObjectResult)result).Value);
        }

        [Test]
        public async Task Delete_NoIds()
        {
            var ctl = CreateCommentController();
            var result = await ctl.Delete(Array.Empty<Guid>());

            Assert.IsInstanceOf<BadRequestObjectResult>(result);
        }

        [Test]
        public async Task Delete_ValidIds()
        {
            var ids = new[] { Guid.NewGuid(), Guid.NewGuid() };

            var ctl = CreateCommentController();
            var result = await ctl.Delete(ids);

            Assert.IsInstanceOf<OkObjectResult>(result);
            Assert.AreEqual(ids, ((OkObjectResult)result).Value);
        }

        [Test]
        public async Task Reply_EmptyId()
        {
            var ctl = CreateCommentController();
            var result = await ctl.Reply(Guid.Empty, "996", null);

            Assert.IsInstanceOf<BadRequestObjectResult>(result);
        }

        [Test]
        public async Task Reply_CommentDisabled()
        {
            _mockBlogConfig.Setup(p => p.ContentSettings).Returns(new ContentSettings
            {
                EnableComments = false
            });

            var ctl = CreateCommentController();
            var result = await ctl.Reply(Guid.NewGuid(), "996", null);

            Assert.IsInstanceOf<ForbidResult>(result);
        }
    }
}
