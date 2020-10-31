using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
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
    }
}
