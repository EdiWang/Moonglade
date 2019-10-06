using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Web.Authentication;
using Moonglade.Web.Controllers;
using Moq;
using NUnit.Framework;

namespace Moonglade.Tests.Web
{
    [TestFixture]
    public class AdminControllerTests
    {
        private Mock<IOptions<AuthenticationSettings>> _authenticationSettingsMock;
        private Mock<ILogger<AdminController>> _loggerMock;

        [SetUp]
        public void Setup()
        {
            _authenticationSettingsMock = new Mock<IOptions<AuthenticationSettings>>();
            _loggerMock = new Mock<ILogger<AdminController>>();
        }

        [Test]
        public void TestDefaultAction()
        {
            var ctl = new AdminController(_loggerMock.Object, _authenticationSettingsMock.Object);
            var result = ctl.Index();
            Assert.IsInstanceOf(typeof(RedirectToActionResult), result);
            if (result is RedirectToActionResult rdResult)
            {
                Assert.That(rdResult.ActionName, Is.EqualTo("Manage"));
                Assert.That(rdResult.ControllerName, Is.EqualTo("Post"));
            }
        }

        [Test]
        public void TestKeepAlive()
        {
            var ctl = new AdminController(_loggerMock.Object, _authenticationSettingsMock.Object);
            var result = ctl.KeepAlive("996.ICU");
            Assert.IsInstanceOf(typeof(JsonResult), result);
        }

        [Test]
        public void TestAccessDenied()
        {
            var ctl = new AdminController(_loggerMock.Object, _authenticationSettingsMock.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
            };

            ctl.ControllerContext.HttpContext.Response.StatusCode = 200;

            var result = ctl.AccessDenied();

            Assert.IsInstanceOf(typeof(ViewResult), result);
            Assert.That(ctl.ControllerContext.HttpContext.Response.StatusCode, Is.EqualTo(403));
        }
    }
}
