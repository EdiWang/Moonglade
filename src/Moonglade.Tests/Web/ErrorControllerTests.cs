﻿using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moonglade.Web.Controllers;
using Moq;
using NUnit.Framework;

namespace Moonglade.Tests.Web
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class ErrorControllerTests
    {
        private Mock<ILogger<ErrorController>> _loggerMock;

        [SetUp]
        public void Setup()
        {
            _loggerMock = new Mock<ILogger<ErrorController>>();
        }

        [TestCase(404, ExpectedResult = 404)]
        [TestCase(500, ExpectedResult = 500)]
        public int KnownStatusCodes(int statusCode)
        {
            var ctl = new ErrorController(_loggerMock.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
            };

            var result = ctl.Error(statusCode);
            Assert.IsInstanceOf(typeof(VirtualFileResult), result);
            return ctl.ControllerContext.HttpContext.Response.StatusCode;
        }

        [TestCase(405, ExpectedResult = 405)]
        [TestCase(429, ExpectedResult = 429)]
        public int UnknownStatusCodes(int statusCode)
        {
            var ctl = new ErrorController(_loggerMock.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
            };

            var result = ctl.Error(statusCode);
            Assert.IsInstanceOf(typeof(StatusCodeResult), result);
            return ctl.ControllerContext.HttpContext.Response.StatusCode;
        }

        [Test]
        public void ExceptionHandler()
        {
            var ctl = new ErrorController(_loggerMock.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
            };

            var result = ctl.Error();
            Assert.IsInstanceOf(typeof(ViewResult), result);
        }
    }
}
