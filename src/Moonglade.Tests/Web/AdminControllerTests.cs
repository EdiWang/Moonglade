using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
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
        private Mock<HttpContext> _httpContextMock;

        [SetUp]
        public void Setup()
        {
            _authenticationSettingsMock = new Mock<IOptions<AuthenticationSettings>>();
            _loggerMock = new Mock<ILogger<AdminController>>();

            var httpResponseMock = new Mock<HttpResponse>();
            httpResponseMock.SetupGet(r => r.StatusCode).Returns(200);

            _httpContextMock = new Mock<HttpContext>();
            _httpContextMock.Setup(c => c.Response).Returns(httpResponseMock.Object);
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
        public async Task TestAccessDenied()
        {
            var ctl = new AdminController(_loggerMock.Object, _authenticationSettingsMock.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = _httpContextMock.Object }
            };

            var result = ctl.AccessDenied();

            Assert.IsInstanceOf(typeof(ViewResult), result);

            //if (result is ViewResult viewResult)
            //{
            //    Assert.That(viewResult.StatusCode, Is.EqualTo(403));
            //}
        }
    }
}
