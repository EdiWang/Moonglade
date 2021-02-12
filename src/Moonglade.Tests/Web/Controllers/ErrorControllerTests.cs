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
        private MockRepository _mockRepository;

        private Mock<ILogger<ErrorController>> _mockLogger;

        [SetUp]
        public void Setup()
        {
            _mockRepository = new(MockBehavior.Default);

            _mockLogger = _mockRepository.Create<ILogger<ErrorController>>();
        }

        [Test]
        public void ExceptionHandler()
        {
            var ctl = new ErrorController(_mockLogger.Object)
            {
                ControllerContext = new() { HttpContext = new DefaultHttpContext() }
            };

            var result = ctl.Error();
            Assert.IsInstanceOf(typeof(ViewResult), result);
        }
    }
}
