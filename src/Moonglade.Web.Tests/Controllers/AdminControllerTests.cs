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
using Moonglade.Configuration.Settings;
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
        private Mock<IPageService> _mockPageService;
        private Mock<ICommentService> _mockCommentService;

        [SetUp]
        public void Setup()
        {
            _mockRepository = new(MockBehavior.Default);

            _mockAuthenticationSettings = _mockRepository.Create<IOptions<AuthenticationSettings>>();
            _mockAudit = _mockRepository.Create<IBlogAudit>();
            _mockPageService = _mockRepository.Create<IPageService>();
            _mockCommentService = _mockRepository.Create<ICommentService>();
        }

        private AdminController CreateAdminController()
        {
            return new(
                _mockAuthenticationSettings.Object,
                _mockAudit.Object,
                _mockPageService.Object,
                _mockCommentService.Object);
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
            Assert.IsInstanceOf(typeof(RedirectResult), result);
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
            Assert.IsInstanceOf(typeof(RedirectResult), result);
        }

        [Test]
        public void KeepAlive()
        {
            var ctl = CreateAdminController();
            var result = ctl.KeepAlive("996.ICU");
            Assert.IsInstanceOf(typeof(JsonResult), result);
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
