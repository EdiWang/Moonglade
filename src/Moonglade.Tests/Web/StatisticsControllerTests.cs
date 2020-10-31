using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
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


        }
    }
}
