using System;
using System.Diagnostics.CodeAnalysis;
using Moonglade.DateTimeOps;
using NUnit.Framework;

namespace Moonglade.Tests
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class DateTimeResolverTests
    {
        [Test]
        public void TestInvalidTimeSpanFormat()
        {
            var resolver = new DateTimeResolver("996ICU");
            var utc = new DateTime(2000, 1, 1, 0, 0, 0);

            Assert.Throws<FormatException>(() =>
            {
                resolver.ToTimeZone(utc);
            });
        }

        [Test]
        public void TestGetDateTimeWithUserTZoneZero()
        {
            var resolver = new DateTimeResolver(string.Empty);

            var utc = new DateTime(2000, 1, 1, 0, 0, 0);
            var dt = resolver.ToTimeZone(utc);

            Assert.IsTrue(dt == DateTime.Parse("2000/1/1 0:00:00"));
        }

        [Test]
        public void TestGetDateTimeWithUserTZone()
        {
            var tSpan = "02:51:00";
            var resolver = new DateTimeResolver(tSpan);

            var utc = new DateTime(2000, 1, 1, 0, 0, 0);
            var dt = resolver.ToTimeZone(utc);

            Assert.IsTrue(dt == DateTime.Parse("2000/1/1 2:51:00"));
        }

        [Test]
        public void TestGetUtcTimeFromUserTZone()
        {
            var tSpan = "10:55:00";
            var resolver = new DateTimeResolver(tSpan);

            var dt = resolver.ToUtc(DateTime.Parse("2000/1/1 10:55:00"));
            var utc = new DateTime(2000, 1, 1, 0, 0, 0);

            Assert.IsTrue(dt == utc);
        }

        [Test]
        [Platform(Include = "Win")]
        public void TestGetTimeSpanByZoneId()
        {
            var tSpan = "10:55:00";
            var resolver = new DateTimeResolver(tSpan);
            var ts = resolver.GetTimeSpanByZoneId("China Standard Time");
            Assert.AreEqual(TimeSpan.FromHours(8), ts);
        }

        [Test]
        public void TestGetTimeSpanByZoneIdEmpty()
        {
            var tSpan = "10:55:00";
            var resolver = new DateTimeResolver(tSpan);
            var ts = resolver.GetTimeSpanByZoneId(string.Empty);
            Assert.AreEqual(TimeSpan.Zero, ts);
        }

        [Test]
        public void TestGetNowWithUserTZone()
        {
            var tSpan = "8:00:00";
            var resolver = new DateTimeResolver(tSpan);
            var utc = DateTime.UtcNow;
            var dt = resolver.NowOfTimeZone;
            Assert.AreEqual(utc.AddHours(8).Date, dt.Date);
            Assert.AreEqual(utc.AddHours(8).Hour, dt.Hour);
            Assert.AreEqual(utc.AddHours(8).Minute, dt.Minute);
        }
    }
}
