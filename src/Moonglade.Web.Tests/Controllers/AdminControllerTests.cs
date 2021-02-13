using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Auditing;
using Moonglade.Auth;
using Moonglade.Configuration.Abstraction;
using Moonglade.Core;
using Moonglade.FriendLink;
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
        private Mock<ILogger<AdminController>> _mockLogger;

        [SetUp]
        public void Setup()
        {
            _mockRepository = new(MockBehavior.Default);

            _mockAuthenticationSettings = _mockRepository.Create<IOptions<AuthenticationSettings>>();
            _mockLogger = _mockRepository.Create<ILogger<AdminController>>();
            _mockAudit = _mockRepository.Create<IBlogAudit>();
            _mockBlogConfig = _mockRepository.Create<IBlogConfig>();
            _mockCat = _mockRepository.Create<ICategoryService>();
            _mockFriendlinkService = _mockRepository.Create<IFriendLinkService>();
        }

        private AdminController CreateAdminController()
        {
            return new(
                _mockLogger.Object,
                _mockAuthenticationSettings.Object,
                _mockAudit.Object,
                null,
                _mockCat.Object,
                _mockFriendlinkService.Object,
                _mockBlogConfig.Object);
        }

        [Test]
        public async Task DefaultAction()
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
        public async Task SignOutAAD()
        {
            _mockAuthenticationSettings.Setup(m => m.Value).Returns(new AuthenticationSettings
            {
                Provider = AuthenticationProvider.AzureAD
            });

            var mockUrlHelper = new Mock<IUrlHelper>(MockBehavior.Strict);
            mockUrlHelper.Setup(x => x.Action(It.IsAny<UrlActionContext>()))
                .Returns("callbackUrl")
                .Verifiable();

            var ctx = new DefaultHttpContext();
            var ctl = CreateAdminController();
            ctl.ControllerContext = new() { HttpContext = ctx };
            ctl.Url = mockUrlHelper.Object;

            var result = await ctl.SignOut();
            Assert.IsInstanceOf(typeof(SignOutResult), result);
        }

        [Test]
        public void SignedOut()
        {
            var ctl = CreateAdminController();
            var result = ctl.SignedOut();
            Assert.IsInstanceOf(typeof(RedirectToActionResult), result);
            if (result is RedirectToActionResult rdResult)
            {
                Assert.That(rdResult.ActionName, Is.EqualTo("Index"));
                Assert.That(rdResult.ControllerName, Is.EqualTo("Home"));
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
        public void AccessDenied()
        {
            var ctl = CreateAdminController();
            ctl.ControllerContext = new() { HttpContext = new DefaultHttpContext() };
            ctl.ControllerContext.HttpContext.Response.StatusCode = 200;

            var result = ctl.AccessDenied();

            Assert.IsInstanceOf(typeof(ForbidResult), result);
        }

        [Test]
        public void About_View()
        {
            var ctl = CreateAdminController();
            var result = ctl.About();

            Assert.IsInstanceOf(typeof(ViewResult), result);
        }
    }
}
