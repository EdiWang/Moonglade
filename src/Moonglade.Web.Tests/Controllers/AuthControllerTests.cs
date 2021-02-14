using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Auditing;
using Moonglade.Auth;
using Moonglade.Web.Controllers;
using Moq;
using NUnit.Framework;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Moonglade.Web.Tests.Controllers
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class AuthControllerTests
    {
        private MockRepository _mockRepository;

        private Mock<IOptions<AuthenticationSettings>> _mockOptions;
        private Mock<ILocalAccountService> _mockLocalAccountService;
        private Mock<IBlogAudit> _mockBlogAudit;
        private Mock<ILogger<AuthController>> _mockLogger;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);

            _mockOptions = _mockRepository.Create<IOptions<AuthenticationSettings>>();
            _mockLocalAccountService = _mockRepository.Create<ILocalAccountService>();
            _mockBlogAudit = _mockRepository.Create<IBlogAudit>();
            _mockLogger = _mockRepository.Create<ILogger<AuthController>>();
        }

        private AuthController CreateAuthController()
        {
            return new(
                _mockOptions.Object,
                _mockLocalAccountService.Object,
                _mockBlogAudit.Object,
                _mockLogger.Object);
        }

        [Test]
        public void SignedOut()
        {
            var ctl = CreateAuthController();
            var result = ctl.SignedOut();
            Assert.IsInstanceOf(typeof(RedirectToActionResult), result);
            if (result is RedirectToActionResult rdResult)
            {
                Assert.That(rdResult.ActionName, Is.EqualTo("Index"));
                Assert.That(rdResult.ControllerName, Is.EqualTo("Home"));
            }
        }

        [Test]
        public void AccessDenied()
        {
            var ctl = CreateAuthController();
            ctl.ControllerContext = new() { HttpContext = new DefaultHttpContext() };
            ctl.ControllerContext.HttpContext.Response.StatusCode = 200;

            var result = ctl.AccessDenied();

            Assert.IsInstanceOf(typeof(ForbidResult), result);
        }

        [Test]
        public async Task SignOutAAD()
        {
            _mockOptions.Setup(m => m.Value).Returns(new AuthenticationSettings
            {
                Provider = AuthenticationProvider.AzureAD
            });

            var mockUrlHelper = new Mock<IUrlHelper>(MockBehavior.Strict);
            mockUrlHelper.Setup(x => x.Action(It.IsAny<UrlActionContext>()))
                .Returns("callbackUrl")
                .Verifiable();

            var ctx = new DefaultHttpContext();
            var ctl = CreateAuthController();
            ctl.ControllerContext = new() { HttpContext = ctx };
            ctl.Url = mockUrlHelper.Object;

            var result = await ctl.SignOut();
            Assert.IsInstanceOf(typeof(SignOutResult), result);
        }

    }
}
