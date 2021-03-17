using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Auditing;
using Moonglade.Auth;
using Moonglade.Web.Controllers;
using Moonglade.Web.Models;
using Moq;
using NUnit.Framework;

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
        public async Task SignIn_AAD()
        {
            _mockOptions.Setup(p => p.Value).Returns(new AuthenticationSettings
            {
                Provider = AuthenticationProvider.AzureAD
            });

            var mockUrlHelper = new Mock<IUrlHelper>(MockBehavior.Strict);
            Expression<Func<IUrlHelper, string>> urlSetup
                = url => url.Action(It.Is<UrlActionContext>(uac => uac.Action == "Index"));
            mockUrlHelper.Setup(urlSetup).Returns("a/mock/url/for/testing").Verifiable();

            var ctl = CreateAuthController();
            ctl.Url = mockUrlHelper.Object;

            var result = await ctl.SignIn();

            Assert.IsInstanceOf<ChallengeResult>(result);
        }

        //[Test]
        //public async Task SignIn_Local()
        //{
        //    _mockOptions.Setup(p => p.Value).Returns(new AuthenticationSettings
        //    {
        //        Provider = AuthenticationProvider.Local
        //    });

        //    var ctl = CreateAuthController();
        //    ctl.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        //    var result = await ctl.SignIn();
        //    Assert.IsInstanceOf<ViewResult>(result);
        //}

        [Test]
        public async Task SignIn_None()
        {
            _mockOptions.Setup(p => p.Value).Returns(new AuthenticationSettings
            {
                Provider = AuthenticationProvider.None
            });

            var ctl = CreateAuthController();
            ctl.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

            var result = await ctl.SignIn();

            Assert.IsInstanceOf<ContentResult>(result);
            var statusCode = ctl.HttpContext.Response.StatusCode;

            Assert.AreEqual(StatusCodes.Status501NotImplemented, statusCode);
        }

        [Test]
        public async Task SignIn_Post_Exception()
        {
            _mockLocalAccountService.Setup(p => p.ValidateAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new Exception("996"));

            var ctl = CreateAuthController();
            var result = await ctl.SignIn(new SignInViewModel() { Username = "work", Password = "996" });

            Assert.IsInstanceOf<ViewResult>(result);

            var modelState = ((ViewResult) result).ViewData.ModelState;
            Assert.IsFalse(modelState.IsValid);
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
