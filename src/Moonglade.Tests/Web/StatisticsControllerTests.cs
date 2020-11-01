using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moonglade.Core;
using Moonglade.Model;
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
        public async Task TestGetEmptyGuid()
        {
            var ctl = new StatisticsController(_statisticsMock.Object);
            var result = await ctl.Get(Guid.Empty);
            Assert.IsInstanceOf(typeof(BadRequestObjectResult), result);
        }

        [Test]
        public async Task TestHitEmptyGuid()
        {
            var ctl = new StatisticsController(_statisticsMock.Object);
            var result = await ctl.Post(Guid.Empty);
            Assert.IsInstanceOf(typeof(BadRequestObjectResult), result);
        }

        [Test]
        public async Task TestLikeEmptyGuid()
        {
            var ctl = new StatisticsController(_statisticsMock.Object);
            var result = await ctl.Post(Guid.Empty, true);
            Assert.IsInstanceOf(typeof(BadRequestObjectResult), result);
        }

        [Test]
        public async Task TestHitDNTEnabled()
        {
            var ctx = new DefaultHttpContext { Items = { ["DNT"] = true } };
            var ctl = new StatisticsController(_statisticsMock.Object)
            {
                ControllerContext = { HttpContext = ctx }
            };

            var result = await ctl.Post(Guid.NewGuid());
            Assert.IsInstanceOf(typeof(OkResult), result);
        }

        [Test]
        public async Task TestLikeDNTEnabled()
        {
            var ctx = new DefaultHttpContext { Items = { ["DNT"] = true } };
            var ctl = new StatisticsController(_statisticsMock.Object)
            {
                ControllerContext = { HttpContext = ctx }
            };

            var result = await ctl.Post(Guid.NewGuid(), true);
            Assert.IsInstanceOf(typeof(OkResult), result);
        }

        [Test]
        public async Task TestHitSameCookie()
        {
            var uid = Guid.NewGuid();

            var reqMock = new Mock<HttpRequest>();
            reqMock.SetupGet(r => r.Cookies[CookieNames.Hit.ToString()]).Returns(uid.ToString());

            var httpContextMock = new Mock<HttpContext>();
            httpContextMock.Setup(c => c.Request).Returns(reqMock.Object);
            httpContextMock.Setup(c => c.Items).Returns(
                new Dictionary<object, object> { { "DNT", false } });

            var ctl = new StatisticsController(_statisticsMock.Object)
            {
                ControllerContext = { HttpContext = httpContextMock.Object }
            };

            var result = await ctl.Post(uid);
            Assert.IsInstanceOf(typeof(OkResult), result);
        }

        [Test]
        public async Task TestHitNewCookie()
        {
            var uid = Guid.NewGuid();

            var ctx = new DefaultHttpContext { Items = { ["DNT"] = false } };
            var ctl = new StatisticsController(_statisticsMock.Object)
            {
                ControllerContext = { HttpContext = ctx }
            };

            var result = await ctl.Post(uid);
            Assert.IsInstanceOf(typeof(OkResult), result);
        }

        [Test]
        public async Task TestLikeSameCookie()
        {
            var uid = Guid.NewGuid();

            var reqMock = new Mock<HttpRequest>();
            reqMock.SetupGet(r => r.Cookies[CookieNames.Liked.ToString()]).Returns(uid.ToString());

            var httpContextMock = new Mock<HttpContext>();
            httpContextMock.Setup(c => c.Request).Returns(reqMock.Object);
            httpContextMock.Setup(c => c.Items).Returns(
                new Dictionary<object, object> { { "DNT", false } });

            var ctl = new StatisticsController(_statisticsMock.Object)
            {
                ControllerContext = { HttpContext = httpContextMock.Object }
            };

            var result = await ctl.Post(uid, true);
            Assert.IsInstanceOf(typeof(ConflictResult), result);
        }

    }
}
