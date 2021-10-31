using Moonglade.Configuration;
using Moq;
using NUnit.Framework;

namespace Moonglade.Web.Tests;

[TestFixture]
public class DateTimeResolverTests
{
    private MockRepository _mockRepository;
    private Mock<IBlogConfig> _mockBlogConfig;

    [SetUp]
    public void SetUp()
    {
        _mockRepository = new(MockBehavior.Default);
        _mockBlogConfig = _mockRepository.Create<IBlogConfig>();
    }

    private BlogTimeZoneResolver CreateResolver(string timeZoneUtcOffset)
    {
        _mockBlogConfig.Setup(p => p.GeneralSettings).Returns(new GeneralSettings
        {
            TimeZoneUtcOffset = timeZoneUtcOffset
        });

        var resolver = new BlogTimeZoneResolver(_mockBlogConfig.Object);
        return resolver;
    }

    [Test]
    public void InvalidTimeSpanFormat()
    {
        var resolver = CreateResolver("996.ICU");
        var utc = new DateTime(2000, 1, 1, 0, 0, 0);

        Assert.Throws<FormatException>(() =>
        {
            resolver.ToTimeZone(utc);
        });
    }

    [Test]
    public void GetDateTime_UserTZoneZero()
    {
        var resolver = CreateResolver(string.Empty);

        var utc = new DateTime(2000, 1, 1, 0, 0, 0);
        var dt = resolver.ToTimeZone(utc);

        Assert.IsTrue(dt == DateTime.Parse("2000/1/1 0:00:00"));
    }

    [Test]
    public void GetDateTime_UserTZone()
    {
        var tSpan = "02:51:00";
        var resolver = CreateResolver(tSpan);

        var utc = new DateTime(2000, 1, 1, 0, 0, 0);
        var dt = resolver.ToTimeZone(utc);

        Assert.IsTrue(dt == DateTime.Parse("2000/1/1 2:51:00"));
    }

    [Test]
    public void GetUtcTimeFromUserTZone()
    {
        var tSpan = "10:55:00";
        var resolver = CreateResolver(tSpan);

        var dt = resolver.ToUtc(DateTime.Parse("2000/1/1 10:55:00"));
        var utc = new DateTime(2000, 1, 1, 0, 0, 0);

        Assert.IsTrue(dt == utc);
    }

    [Test]
    public void GetUtcTimeFromUserTZoneStd()
    {
        var tSpan = "8:00:00";
        var resolver = CreateResolver(tSpan);

        var dt = resolver.ToUtc(DateTime.Parse("2000/1/1 8:00:00"));
        var utc = new DateTime(2000, 1, 1, 0, 0, 0);

        Assert.IsTrue(dt == utc);
    }

    [Test]
    [Platform(Include = "Win")]
    public void GetTimeSpanByZoneId()
    {
        var tSpan = "10:55:00";
        var resolver = CreateResolver(tSpan);
        var ts = resolver.GetTimeSpanByZoneId("China Standard Time");
        Assert.AreEqual(TimeSpan.FromHours(8), ts);
    }

    [Test]
    public void GetTimeSpanByZoneId_Empty()
    {
        var tSpan = "10:55:00";
        var resolver = CreateResolver(tSpan);
        var ts = resolver.GetTimeSpanByZoneId(string.Empty);
        Assert.AreEqual(TimeSpan.Zero, ts);
    }

    [Test]
    public void GetNow_UserTZone()
    {
        var tSpan = "8:00:00";
        var resolver = CreateResolver(tSpan);
        var utc = DateTime.UtcNow;
        var dt = resolver.NowOfTimeZone;
        Assert.AreEqual(utc.AddHours(8).Date, dt.Date);
        Assert.AreEqual(utc.AddHours(8).Hour, dt.Hour);
        Assert.AreEqual(utc.AddHours(8).Minute, dt.Minute);
    }
}