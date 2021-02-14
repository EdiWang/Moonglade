using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moonglade.Auditing;
using Moonglade.Auth;
using Moonglade.Comments;
using Moonglade.Configuration.Abstraction;
using Moonglade.Core;
using Moonglade.FriendLink;
using Moonglade.Pages;
using Moonglade.Pingback;
using Moonglade.Web.Controllers;
using Moq;
using NUnit.Framework;

namespace Moonglade.Web.Tests.Controllers
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class AdminControllerTests
    {
        private MockRepository _mockRepository;

        private Mock<IOptions<AuthenticationSettings>> _mockAuthenticationSettings;
        private Mock<IBlogAudit> _mockAudit;
        private Mock<IBlogConfig> _mockBlogConfig;
        private Mock<ICategoryService> _mockCat;
        private Mock<IFriendLinkService> _mockFriendlinkService;
        private Mock<IPageService> _mockPageService;
        private Mock<ITagService> _mockTagService;
        private Mock<ICommentService> _mockCommentService;
        private Mock<IPingbackService> _mockPingbackService;

        [SetUp]
        public void Setup()
        {
            _mockRepository = new(MockBehavior.Default);

            _mockAuthenticationSettings = _mockRepository.Create<IOptions<AuthenticationSettings>>();
            _mockAudit = _mockRepository.Create<IBlogAudit>();
            _mockBlogConfig = _mockRepository.Create<IBlogConfig>();
            _mockCat = _mockRepository.Create<ICategoryService>();
            _mockFriendlinkService = _mockRepository.Create<IFriendLinkService>();
            _mockPageService = _mockRepository.Create<IPageService>();
            _mockTagService = _mockRepository.Create<ITagService>();
            _mockCommentService = _mockRepository.Create<ICommentService>();
            _mockPingbackService = _mockRepository.Create<IPingbackService>();
        }

        private AdminController CreateAdminController()
        {
            return new(
                _mockAuthenticationSettings.Object,
                _mockAudit.Object,
                _mockCat.Object,
                _mockFriendlinkService.Object,
                _mockPageService.Object,
                _mockTagService.Object,
                _mockCommentService.Object,
                _mockPingbackService.Object,
                _mockBlogConfig.Object);
        }

        [Test]
        public async Task Index_Local()
        {
            _mockAuthenticationSettings.Setup(c => c.Value)
                .Returns(new AuthenticationSettings
                {
                    Provider = AuthenticationProvider.Local
                });

            var ctl = CreateAdminController();
            var result = await ctl.Index();
            Assert.IsInstanceOf(typeof(RedirectToActionResult), result);
            if (result is RedirectToActionResult rdResult)
            {
                Assert.That(rdResult.ActionName, Is.EqualTo("Post"));
            }
        }

        [Test]
        public async Task Index_AAD()
        {
            _mockAuthenticationSettings.Setup(c => c.Value)
                .Returns(new AuthenticationSettings
                {
                    Provider = AuthenticationProvider.AzureAD
                });

            var ctl = CreateAdminController();
            ctl.ControllerContext = new()
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new(new ClaimsIdentity(new[] { new Claim("Name", "Edi") }))
                }
            };

            var result = await ctl.Index();
            Assert.IsInstanceOf(typeof(RedirectToActionResult), result);
            if (result is RedirectToActionResult rdResult)
            {
                Assert.That(rdResult.ActionName, Is.EqualTo("Post"));
            }
        }

        [Test]
        public void KeepAlive()
        {
            var ctl = CreateAdminController();
            var result = ctl.KeepAlive("996.ICU");
            Assert.IsInstanceOf(typeof(JsonResult), result);
        }

        [Test]
        public void About_View()
        {
            var ctl = CreateAdminController();
            var result = ctl.About();

            Assert.IsInstanceOf(typeof(ViewResult), result);
        }

        [Test]
        public void Post_View()
        {
            var ctl = CreateAdminController();
            var result = ctl.Post();

            Assert.IsInstanceOf(typeof(ViewResult), result);
        }

        [Test]
        public async Task Page_View()
        {
            IReadOnlyList<PageSegment> fakePageSegments = new List<PageSegment>()
            {
                new ()
                {
                    IsPublished = true,
                    CreateTimeUtc = DateTime.UtcNow,
                    Id = Guid.Empty,
                    Slug = "fuck-996",
                    Title = "Fuck Jack Ma's Fu Bao"
                }
            };
            _mockPageService.Setup(p => p.ListSegment()).Returns(Task.FromResult(fakePageSegments));

            var ctl = CreateAdminController();
            var result = await ctl.Page();

            Assert.IsInstanceOf(typeof(ViewResult), result);
            Assert.AreEqual(fakePageSegments, ((ViewResult) result).Model);
        }
    }
}
