using System;
using Moonglade.Core;
using NUnit.Framework;

namespace Moonglade.Tests.Core
{
    [TestFixture]
    public class DateTimeResolverTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void TestGetDateTimeWithUserTZone()
        {
            var tSpan = "02:51:00";
            var resolver = new DateTimeResolver(tSpan);

            var utc = new DateTime(2000, 1, 1, 0, 0, 0);
            var dt = resolver.GetDateTimeWithUserTZone(utc);

            Assert.IsTrue(dt == DateTime.Parse("2000/1/1 2:51:00"));
        }

        [Test]
        public void TestGetUtcTimeFromUserTZone()
        {
            var tSpan = "10:55:00";
            var resolver = new DateTimeResolver(tSpan);

            var dt = resolver.GetUtcTimeFromUserTZone(DateTime.Parse("2000/1/1 10:55:00"));
            var utc = new DateTime(2000, 1, 1, 0, 0, 0);

            Assert.IsTrue(dt == utc);
        }
    }
}
