using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Options;
using Moonglade.Auth;
using Moonglade.Web.Controllers;
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

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);
            _mockOptions = _mockRepository.Create<IOptions<AuthenticationSettings>>();
        }

        private AuthController CreateAuthController()
        {
            return new(_mockOptions.Object);
        }

        [Test]
        public void SignedOut()
        {
            var ctl = CreateAuthController();
            var result = ctl.SignedOut();
            Assert.IsInstanceOf(typeof(RedirectToPageResult), result);
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
