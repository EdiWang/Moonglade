using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moonglade.Web.Controllers;
using Moq;
using NUnit.Framework;

namespace Moonglade.Tests.Web
{
    [TestFixture]
    public class MoongladeControllerTests
    {
        private Mock<ILogger<ControllerBase>> _loggerMock;

        [SetUp]
        public void Setup()
        {
            _loggerMock = new Mock<ILogger<ControllerBase>>();
        }

        [Test]
        public void TestServerError()
        {
            var ctl = new MoongladeController(_loggerMock.Object);
            var result = ctl.ServerError();
            Assert.IsInstanceOf(typeof(StatusCodeResult), result);
            if (result is StatusCodeResult rdResult)
            {
                Assert.That(rdResult.StatusCode, Is.EqualTo(500));
            }
        }
    }
}
