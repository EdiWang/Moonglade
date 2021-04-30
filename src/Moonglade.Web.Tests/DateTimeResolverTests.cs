using System;
using NUnit.Framework;

namespace Moonglade.Web.Tests
{
    [TestFixture]
    public class DateTimeResolverTests
    {
        [Test]
        public void InvalidTimeSpanFormat()
        {
            var resolver = new BlogTimeZoneResolver("996ICU");
            var utc = new DateTime(2000, 1, 1, 0, 0, 0);

            Assert.Throws<FormatException>(() =>
            {
                resolver.ToTimeZone(utc);
            });
        }

        [Test]
        public void GetDateTime_UserTZoneZero()
        {
            var resolver = new BlogTimeZoneResolver(string.Empty);

            var utc = new DateTime(2000, 1, 1, 0, 0, 0);
            var dt = resolver.ToTimeZone(utc);

            Assert.IsTrue(dt == DateTime.Parse("2000/1/1 0:00:00"));
        }

        [Test]
        public void GetDateTime_UserTZone()
        {
            var tSpan = "02:51:00";
            var resolver = new BlogTimeZoneResolver(tSpan);

            var utc = new DateTime(2000, 1, 1, 0, 0, 0);
            var dt = resolver.ToTimeZone(utc);

            Assert.IsTrue(dt == DateTime.Parse("2000/1/1 2:51:00"));
        }

        [Test]
        public void GetUtcTimeFromUserTZone()
        {
            var tSpan = "10:55:00";
            var resolver = new BlogTimeZoneResolver(tSpan);

            var dt = resolver.ToUtc(DateTime.Parse("2000/1/1 10:55:00"));
            var utc = new DateTime(2000, 1, 1, 0, 0, 0);

            Assert.IsTrue(dt == utc);
        }

        [Test]
        public void GetUtcTimeFromUserTZoneStd()
        {
            var tSpan = "8:00:00";
            var resolver = new BlogTimeZoneResolver(tSpan);

            var dt = resolver.ToUtc(DateTime.Parse("2000/1/1 8:00:00"));
            var utc = new DateTime(2000, 1, 1, 0, 0, 0);

            Assert.IsTrue(dt == utc);
        }

        [Test]
        [Platform(Include = "Win")]
        public void GetTimeSpanByZoneId()
        {
            var tSpan = "10:55:00";
            var resolver = new BlogTimeZoneResolver(tSpan);
            var ts = resolver.GetTimeSpanByZoneId("China Standard Time");
            Assert.AreEqual(TimeSpan.FromHours(8), ts);
        }

        [Test]
        public void GetTimeSpanByZoneId_Empty()
        {
            var tSpan = "10:55:00";
            var resolver = new BlogTimeZoneResolver(tSpan);
            var ts = resolver.GetTimeSpanByZoneId(string.Empty);
            Assert.AreEqual(TimeSpan.Zero, ts);
        }

        [Test]
        public void GetNow_UserTZone()
        {
            var tSpan = "8:00:00";
            var resolver = new BlogTimeZoneResolver(tSpan);
            var utc = DateTime.UtcNow;
            var dt = resolver.NowOfTimeZone;
            Assert.AreEqual(utc.AddHours(8).Date, dt.Date);
            Assert.AreEqual(utc.AddHours(8).Hour, dt.Hour);
            Assert.AreEqual(utc.AddHours(8).Minute, dt.Minute);
        }
    }
}
