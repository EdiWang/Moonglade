using System;
using System.Collections.Generic;
using System.Text;
using Moonglade.Configuration;
using Moonglade.Configuration.Abstraction;
using Moonglade.Core;
using Moq;
using NUnit.Framework;

namespace Moonglade.Tests.Core
{
    [TestFixture]
    public class DateTimeResolverTests
    {
        private Mock<IBlogConfig> _blogConfigMock;

        [SetUp]
        public void Setup()
        {
            _blogConfigMock = new Mock<IBlogConfig>();
        }

        [Test]
        public void TestGetDateTimeWithUserTZone()
        {
            var tSpan = "02:51:00";
            _blogConfigMock.Setup(m => m.GeneralSettings).Returns(new GeneralSettings
            {
                TimeZoneUtcOffset = tSpan
            });

            var resolver = new DateTimeResolver(_blogConfigMock.Object);

            var utc = new DateTime(2000, 1, 1, 0, 0, 0);
            var dt = resolver.GetDateTimeWithUserTZone(utc);

            Assert.IsTrue(dt == DateTime.Parse("2000/1/1 2:51:00"));
        }

        [Test]
        public void TestGetUtcTimeFromUserTZone()
        {
            var tSpan = "10:55:00";
            _blogConfigMock.Setup(m => m.GeneralSettings).Returns(new GeneralSettings
            {
                TimeZoneUtcOffset = tSpan
            });

            var resolver = new DateTimeResolver(_blogConfigMock.Object);

            var dt = resolver.GetUtcTimeFromUserTZone(DateTime.Parse("2000/1/1 10:55:00"));
            var utc = new DateTime(2000, 1, 1, 0, 0, 0);

            Assert.IsTrue(dt == utc);
        }
    }
}
