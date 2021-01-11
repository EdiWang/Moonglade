using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
    [ExcludeFromCodeCoverage]
    public class StatisticsControllerTests
    {
        private Mock<IBlogStatistics> _statisticsMock;

        [SetUp]
        public void Setup()
        {
            _statisticsMock = new();
        }

        [Test]
        public async Task Get_EmptyGuid()
        {
            var ctl = new StatisticsController(_statisticsMock.Object);
            var result = await ctl.Get(Guid.Empty);
            Assert.IsInstanceOf(typeof(BadRequestObjectResult), result);
        }

        [Test]
        public async Task Hit_EmptyGuid()
        {
            var ctl = new StatisticsController(_statisticsMock.Object);
            var result = await ctl.Post(new() { PostId = Guid.Empty, IsLike = false });
            Assert.IsInstanceOf(typeof(BadRequestObjectResult), result);
        }

        [Test]
        public async Task Like_EmptyGuid()
        {
            var ctl = new StatisticsController(_statisticsMock.Object);
            var result = await ctl.Post(new() { PostId = Guid.Empty, IsLike = true });
            Assert.IsInstanceOf(typeof(BadRequestObjectResult), result);
        }

        [Test]
        public async Task Hit_DNTEnabled()
        {
            var ctx = new DefaultHttpContext { Items = { ["DNT"] = true } };
            var ctl = new StatisticsController(_statisticsMock.Object)
            {
                ControllerContext = { HttpContext = ctx }
            };

            var result = await ctl.Post(new() { PostId = Guid.NewGuid(), IsLike = false });
            Assert.IsInstanceOf(typeof(OkResult), result);
        }

        [Test]
        public async Task Like_DNTEnabled()
        {
            var ctx = new DefaultHttpContext { Items = { ["DNT"] = true } };
            var ctl = new StatisticsController(_statisticsMock.Object)
            {
                ControllerContext = { HttpContext = ctx }
            };

            var result = await ctl.Post(new() { PostId = Guid.NewGuid(), IsLike = true });
            Assert.IsInstanceOf(typeof(OkResult), result);
        }

        [Test]
        public async Task Hit_SameCookie()
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

            var result = await ctl.Post(new() { PostId = uid, IsLike = false });
            Assert.IsInstanceOf(typeof(OkResult), result);
        }

        [Test]
        public async Task Hit_NewCookie()
        {
            var uid = Guid.NewGuid();

            var ctx = new DefaultHttpContext { Items = { ["DNT"] = false } };
            var ctl = new StatisticsController(_statisticsMock.Object)
            {
                ControllerContext = { HttpContext = ctx }
            };

            var result = await ctl.Post(new() { PostId = uid, IsLike = false });
            Assert.IsInstanceOf(typeof(OkResult), result);
        }

        [Test]
        public async Task Like_SameCookie()
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

            var result = await ctl.Post(new() { PostId = uid, IsLike = true });
            Assert.IsInstanceOf(typeof(ConflictResult), result);
        }

    }
}
