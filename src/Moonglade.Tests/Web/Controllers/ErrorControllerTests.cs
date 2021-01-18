using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moonglade.Web.Controllers;
using Moq;
using NUnit.Framework;

namespace Moonglade.Tests.Web.Controllers
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class ErrorControllerTests
    {
        private Mock<ILogger<ErrorController>> _loggerMock;

        [SetUp]
        public void Setup()
        {
            _loggerMock = new();
        }

        [Test]
        public void ExceptionHandler()
        {
            var ctl = new ErrorController(_loggerMock.Object)
            {
                ControllerContext = new() { HttpContext = new DefaultHttpContext() }
            };

            var result = ctl.Error();
            Assert.IsInstanceOf(typeof(ViewResult), result);
        }
    }
}
