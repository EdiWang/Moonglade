using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
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
        public async Task List_OK()
        {
            IReadOnlyList<Comment> comments = new List<Comment>
            {
                new()
                {
                    Username = "Jack Ma", Email = "fubao@996.icu", CommentContent = "996 is fubao", CreateTimeUtc = DateTime.Today
                }
            };

            _mockCommentService.Setup(p => p.GetApprovedCommentsAsync(It.IsAny<Guid>()))
                .Returns(Task.FromResult(comments));

            var mockTz = _mockRepository.Create<ITimeZoneResolver>();
            mockTz.Setup(p => p.ToTimeZone(It.IsAny<DateTime>())).Returns(DateTime.Today);

            var ctl = CreateCommentController();
            var result = await ctl.List(FakeData.Uid2, mockTz.Object);

            Assert.IsInstanceOf<OkObjectResult>(result);
        }

        [Test]
        public async Task Create_InvalidEmail()
        {
            var ctl = CreateCommentController();
            var result = await ctl.Create(FakeData.Uid1, new()
            {
                Email = "work996",
                CaptchaCode = "0996",
                Content = "Get your fubao",
                Username = "Jack Ma"
            });

            Assert.IsInstanceOf<BadRequestObjectResult>(result);
        }

        [Test]
        public async Task Create_CommentDisabled()
        {
            _mockBlogConfig.Setup(p => p.ContentSettings).Returns(new ContentSettings()
            {
                EnableComments = false
            });

            var ctl = CreateCommentController();
            var result = await ctl.Create(FakeData.Uid1, new()
            {
                Email = "work996@996.icu",
                CaptchaCode = "0996",
                Content = "Get your fubao",
                Username = "Jack Ma"
            });

            Assert.IsInstanceOf<ForbidResult>(result);
        }

        [Test]
        public async Task Create_ServiceBoom()
        {
            _mockBlogConfig.Setup(p => p.ContentSettings).Returns(new ContentSettings()
            {
                EnableComments = true
            });

            _mockCommentService.Setup(p => p.CreateAsync(It.IsAny<CommentRequest>()))
                .Returns(Task.FromResult((CommentDetailedItem)null));

            var ctl = CreateCommentController();
            ctl.ControllerContext = new()
            {
                HttpContext = new DefaultHttpContext()
                {
                    Items = { ["DNT"] = true }
                }
            };

            var result = await ctl.Create(FakeData.Uid1, new()
            {
                Email = "work996@996.icu",
                CaptchaCode = "0996",
                Content = "Get your fubao",
                Username = "Jack Ma"
            });

            Assert.IsInstanceOf<ConflictObjectResult>(result);
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
