using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Options;
using Moonglade.Auth;
using Moonglade.Web.Controllers;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Moonglade.Web.Tests.Controllers
{
    [TestFixture]
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

            var mockUrlHelper = CreateMockUrlHelper();
            mockUrlHelper.Setup(h => h.RouteUrl(It.IsAny<UrlRouteContext>()))
                .Returns("callbackUrl");

            var ctx = new DefaultHttpContext();
            var ctl = CreateAuthController();
            ctl.ControllerContext = new() { HttpContext = ctx };
            ctl.Url = mockUrlHelper.Object;

            var result = await ctl.SignOut();
            Assert.IsInstanceOf(typeof(SignOutResult), result);
        }

        private Mock<IUrlHelper> CreateMockUrlHelper(ActionContext context = null)
        {
            context ??= GetActionContextForPage("/Page");

            var urlHelper = _mockRepository.Create<IUrlHelper>();
            urlHelper.SetupGet(h => h.ActionContext)
                .Returns(context);
            return urlHelper;
        }

        private static ActionContext GetActionContextForPage(string page)
        {
            return new()
            {
                ActionDescriptor = new()
                {
                    RouteValues = new Dictionary<string, string>
                    {
                        { "page", page },
                    }
                },
                RouteData = new()
                {
                    Values =
                    {
                        [ "page" ] = page
                    }
                }
            };
        }
    }
}
