using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moonglade.Core;
using Moonglade.Web.Controllers;
using Moq;
using NUnit.Framework;

namespace Moonglade.Tests.Web
{
    [TestFixture]
    public class StatisticsControllerTests
    {
        private Mock<IBlogStatistics> _statisticsMock;

        [SetUp]
        public void Setup()
        {
            _statisticsMock = new Mock<IBlogStatistics>();
        }

        [Test]
        public async Task TestHitEmptyGuid()
        {
            var ctl = new StatisticsController(_statisticsMock.Object);
            var result = await ctl.Hit(Guid.Empty);
            Assert.IsInstanceOf(typeof(BadRequestObjectResult), result);
        }

        [Test]
        public async Task TestLikeEmptyGuid()
        {
            var ctl = new StatisticsController(_statisticsMock.Object);
            var result = await ctl.Like(Guid.Empty);
            Assert.IsInstanceOf(typeof(BadRequestObjectResult), result);
        }

        [Test]
        public async Task TestHitDNTEnabled()
        {
            var httpContextMock = new Mock<HttpContext>();
            httpContextMock.Setup(c => c.Items).Returns(
                new Dictionary<object, object> { { "DNT", true } });

            var ctl = new StatisticsController(_statisticsMock.Object)
            {
                ControllerContext = { HttpContext = httpContextMock.Object }
            };

            var result = await ctl.Hit(Guid.NewGuid());
            Assert.IsInstanceOf(typeof(OkResult), result);
        }

        [Test]
        public async Task TestLikeDNTEnabled()
        {
            var httpContextMock = new Mock<HttpContext>();
            httpContextMock.Setup(c => c.Items).Returns(
                new Dictionary<object, object> { { "DNT", true } });

            var ctl = new StatisticsController(_statisticsMock.Object)
            {
                ControllerContext = { HttpContext = httpContextMock.Object }
            };

            var result = await ctl.Like(Guid.NewGuid());
            Assert.IsInstanceOf(typeof(OkResult), result);
        }
    }
}
