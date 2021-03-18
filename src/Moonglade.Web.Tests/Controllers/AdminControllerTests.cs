using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;
using Moonglade.Auditing;
using Moonglade.Auth;
using Moonglade.Comments;
using Moonglade.Configuration;
using Moonglade.Configuration.Abstraction;
using Moonglade.Configuration.Settings;
using Moonglade.Core;
using Moonglade.FriendLink;
using Moonglade.Pages;
using Moonglade.Web.Controllers;
using Moonglade.Web.Models;
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
        private Mock<ICommentService> _mockCommentService;

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
            _mockCommentService = _mockRepository.Create<ICommentService>();
        }

        private AdminController CreateAdminController()
        {
            return new(
                _mockAuthenticationSettings.Object,
                _mockAudit.Object,
                _mockCat.Object,
                _mockFriendlinkService.Object,
                _mockPageService.Object,
                _mockCommentService.Object,
                _mockBlogConfig.Object);
        }

        readonly Page _fakePage = new()
        {
            Id = Guid.Empty,
            CreateTimeUtc = new DateTime(996, 9, 6),
            CssContent = ".jack-ma .heart {color: black !important;}",
            HideSidebar = false,
            IsPublished = false,
            MetaDescription = "Fuck Jack Ma",
            RawHtmlContent = "<p>Fuck 996</p>",
            Slug = "fuck-jack-ma",
            Title = "Fuck Jack Ma 1000 years!",
            UpdateTimeUtc = new DateTime(1996, 9, 6)
        };

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
        public void Post_View()
        {
            var ctl = CreateAdminController();
            var result = ctl.Post();

            Assert.IsInstanceOf(typeof(ViewResult), result);
        }

        [Test]
        public async Task ClearAuditLogs_Redirect()
        {
            var ctl = CreateAdminController();
            var result = await ctl.ClearAuditLogs();

            _mockAudit.Verify();

            Assert.IsInstanceOf<RedirectToActionResult>(result);
        }

        [Test]
        public async Task Page_View()
        {
            IReadOnlyList<PageSegment> fakePageSegments = new List<PageSegment>
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
            Assert.AreEqual(fakePageSegments, ((ViewResult)result).Model);
        }

        [Test]
        public void CreatePage_Success()
        {
            var ctl = CreateAdminController();
            var result = ctl.CreatePage();

            Assert.IsInstanceOf<ViewResult>(result);
            Assert.IsInstanceOf<PageEditModel>(((ViewResult)result).Model);
        }

        [Test]
        public async Task Preview_NoPage()
        {
            _mockPageService.Setup(p => p.GetAsync(It.IsAny<Guid>()))
                .Returns(Task.FromResult((Page)null));

            var ctl = CreateAdminController();
            var result = await ctl.PreviewPage(Guid.Empty);

            Assert.IsInstanceOf<NotFoundResult>(result);
        }

        [Test]
        public async Task Edit_NoPage()
        {
            _mockPageService.Setup(p => p.GetAsync(It.IsAny<Guid>()))
                .Returns(Task.FromResult((Page)null));

            var ctl = CreateAdminController();
            var result = await ctl.EditPage(Guid.Empty);

            Assert.IsInstanceOf<NotFoundResult>(result);
        }

        [Test]
        public async Task Edit_View()
        {
            _mockPageService.Setup(p => p.GetAsync(It.IsAny<Guid>()))
                .Returns(Task.FromResult(_fakePage));

            var ctl = CreateAdminController();
            var result = await ctl.EditPage(Guid.Empty);

            Assert.IsInstanceOf<ViewResult>(result);

            var model = ((ViewResult)result).Model;
            Assert.IsInstanceOf<PageEditModel>(model);
        }

        [Test]
        public async Task Preview_HasPage()
        {
            _mockPageService.Setup(p => p.GetAsync(It.IsAny<Guid>()))
                .Returns(Task.FromResult(_fakePage));

            var ctl = CreateAdminController();
            var result = await ctl.PreviewPage(Guid.Empty);

            Assert.IsInstanceOf<ViewResult>(result);

            var model = ((ViewResult)result).Model;
            Assert.IsInstanceOf<Page>(model);
            Assert.AreEqual(_fakePage.Title, ((Page)model).Title);
        }

        [Test]
        public async Task Category_View()
        {
            IReadOnlyList<Category> cats = new List<Category>
            {
                new (){Id = Guid.Empty, DisplayName = "Work 996", Note = "Fubao", RouteName = "work-996" }
            };

            _mockCat.Setup(p => p.GetAll()).Returns(Task.FromResult(cats));

            var ctl = CreateAdminController();
            var result = await ctl.Category();

            Assert.IsInstanceOf<ViewResult>(result);

            var model = ((ViewResult)result).Model;
            Assert.IsInstanceOf<CategoryManageModel>(model);
        }

        [Test]
        public async Task FriendLink_View()
        {
            IReadOnlyList<Link> links = new List<Link>();
            _mockFriendlinkService.Setup(p => p.GetAllAsync()).Returns(Task.FromResult(links));
            _mockBlogConfig.Setup(p => p.FriendLinksSettings).Returns(new FriendLinksSettings());

            var ctl = CreateAdminController();
            var result = await ctl.FriendLink();

            Assert.IsInstanceOf<ViewResult>(result);
        }

        [Test]
        public async Task Comments_View()
        {
            IReadOnlyList<CommentDetailedItem> comments = new List<CommentDetailedItem>();

            _mockCommentService.Setup(p => p.GetCommentsAsync(It.IsAny<int>(), 1))
                .Returns(Task.FromResult(comments));
            _mockCommentService.Setup(p => p.Count()).Returns(996);

            var ctl = CreateAdminController();
            var result = await ctl.Comments(1);

            Assert.IsInstanceOf<ViewResult>(result);
        }

        [Test]
        public async Task AuditLogs_FeatureDisabled()
        {
            Mock<IFeatureManager> mockFeatureMgr = new Mock<IFeatureManager>();
            mockFeatureMgr.Setup(p => p.IsEnabledAsync(nameof(FeatureFlags.EnableAudit)))
                .Returns(Task.FromResult(false));

            var ctl = CreateAdminController();
            var result = await ctl.AuditLogs(mockFeatureMgr.Object);

            Assert.IsInstanceOf<ViewResult>(result);
            Assert.IsNull(((ViewResult)result).Model);
        }

        [TestCase(0)]
        [TestCase(-1)]
        public async Task AuditLogs_FeatureEnabled_BadPageSize(int pageSize)
        {
            Mock<IFeatureManager> mockFeatureMgr = new Mock<IFeatureManager>();
            mockFeatureMgr.Setup(p => p.IsEnabledAsync(nameof(FeatureFlags.EnableAudit)))
                .Returns(Task.FromResult(true));

            var ctl = CreateAdminController();
            var result = await ctl.AuditLogs(mockFeatureMgr.Object, pageSize);

            Assert.IsInstanceOf<BadRequestObjectResult>(result);
        }

        [Test]
        public async Task AuditLogs_View()
        {
            Mock<IFeatureManager> mockFeatureMgr = new Mock<IFeatureManager>();
            mockFeatureMgr.Setup(p => p.IsEnabledAsync(nameof(FeatureFlags.EnableAudit)))
                .Returns(Task.FromResult(true));
            (IReadOnlyList<AuditEntry> Entries, int Count) data = new(new List<AuditEntry>(), 996);

            _mockAudit.Setup(p => p.GetAuditEntries(It.IsAny<int>(), It.IsAny<int>(), null, null)).Returns(Task.FromResult(data));

            var ctl = CreateAdminController();
            var result = await ctl.AuditLogs(mockFeatureMgr.Object, 1);

            Assert.IsInstanceOf<ViewResult>(result);
        }
    }
}
