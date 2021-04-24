using System;
using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moonglade.Auditing;
using Moonglade.Auth;
using Moonglade.Pages;
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
        private Mock<IBlogPageService> _mockPageService;

        [SetUp]
        public void Setup()
        {
            _mockRepository = new(MockBehavior.Default);

            _mockAuthenticationSettings = _mockRepository.Create<IOptions<AuthenticationSettings>>();
            _mockAudit = _mockRepository.Create<IBlogAudit>();
            _mockPageService = _mockRepository.Create<IBlogPageService>();
        }

        private AdminController CreateAdminController()
        {
            return new(
                _mockAuthenticationSettings.Object,
                _mockAudit.Object,
                _mockPageService.Object);
        }

        readonly BlogPage _fakeBlogPage = new()
        {
            Id = Guid.Empty,
            CreateTimeUtc = new(996, 9, 6),
            CssContent = ".jack-ma .heart {color: black !important;}",
            HideSidebar = false,
            IsPublished = false,
            MetaDescription = "Fuck Jack Ma",
            RawHtmlContent = "<p>Fuck 996</p>",
            Slug = "fuck-jack-ma",
            Title = "Fuck Jack Ma 1000 years!",
            UpdateTimeUtc = new(1996, 9, 6)
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

            Assert.IsInstanceOf<RedirectResult>(result);
        }

        [Test]
        public async Task Preview_NoPage()
        {
            _mockPageService.Setup(p => p.GetAsync(It.IsAny<Guid>()))
                .Returns(Task.FromResult((BlogPage)null));

            var ctl = CreateAdminController();
            var result = await ctl.PreviewPage(Guid.Empty);

            Assert.IsInstanceOf<NotFoundResult>(result);
        }

        [Test]
        public async Task Preview_HasPage()
        {
            _mockPageService.Setup(p => p.GetAsync(It.IsAny<Guid>()))
                .Returns(Task.FromResult(_fakeBlogPage));

            var ctl = CreateAdminController();
            var result = await ctl.PreviewPage(Guid.Empty);

            Assert.IsInstanceOf<ViewResult>(result);

            var model = ((ViewResult)result).Model;
            Assert.IsInstanceOf<BlogPage>(model);
            Assert.AreEqual(_fakeBlogPage.Title, ((BlogPage)model).Title);
        }
    }
}
