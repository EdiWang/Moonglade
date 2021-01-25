using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Auditing;
using Moonglade.Auth;
using Moonglade.Web.Authentication;
using Moonglade.Web.Controllers;
using Moq;
using NUnit.Framework;

namespace Moonglade.Tests.Web.Controllers
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class AdminControllerTests
    {
        private Mock<IOptions<AuthenticationSettings>> _authenticationSettingsMock;
        private Mock<IBlogAudit> _auditMock;
        private Mock<ILogger<AdminController>> _loggerMock;

        [SetUp]
        public void Setup()
        {
            _authenticationSettingsMock = new();
            _loggerMock = new();
            _auditMock = new();
        }

        [Test]
        public async Task DefaultAction()
        {
            _authenticationSettingsMock.Setup(c => c.Value)
                .Returns(new AuthenticationSettings
                {
                    Provider = AuthenticationProvider.Local
                });

            var ctl = new AdminController(_loggerMock.Object, _authenticationSettingsMock.Object, _auditMock.Object, null);
            var result = await ctl.Index();
            Assert.IsInstanceOf(typeof(RedirectToActionResult), result);
            if (result is RedirectToActionResult rdResult)
            {
                Assert.That(rdResult.ActionName, Is.EqualTo("Index"));
                Assert.That(rdResult.ControllerName, Is.EqualTo("PostManage"));
            }
        }

        [Test]
        public async Task SignOutAAD()
        {
            _authenticationSettingsMock.Setup(m => m.Value).Returns(new AuthenticationSettings
            {
                Provider = AuthenticationProvider.AzureAD
            });

            var mockUrlHelper = new Mock<IUrlHelper>(MockBehavior.Strict);
            mockUrlHelper.Setup(x => x.Action(It.IsAny<UrlActionContext>()))
                .Returns("callbackUrl")
                .Verifiable();

            var ctx = new DefaultHttpContext();
            var ctl = new AdminController(_loggerMock.Object, _authenticationSettingsMock.Object, _auditMock.Object, null)
            {
                ControllerContext = new() { HttpContext = ctx },
                Url = mockUrlHelper.Object
            };

            var result = await ctl.SignOut();
            Assert.IsInstanceOf(typeof(SignOutResult), result);
        }

        [Test]
        public void SignedOut()
        {
            var ctl = new AdminController(_loggerMock.Object, _authenticationSettingsMock.Object, _auditMock.Object, null);
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
            var ctl = new AdminController(_loggerMock.Object, _authenticationSettingsMock.Object, _auditMock.Object, null);
            var result = ctl.KeepAlive("996.ICU");
            Assert.IsInstanceOf(typeof(JsonResult), result);
        }

        [Test]
        public void AccessDenied()
        {
            var ctl = new AdminController(_loggerMock.Object, _authenticationSettingsMock.Object, _auditMock.Object, null)
            {
                ControllerContext = new() { HttpContext = new DefaultHttpContext() }
            };

            ctl.ControllerContext.HttpContext.Response.StatusCode = 200;

            var result = ctl.AccessDenied();

            Assert.IsInstanceOf(typeof(ForbidResult), result);
        }
    }
}
