using Moonglade.Web.Commands;

namespace Moonglade.Web.Tests;

public class ScheduledPublishTimeResolverTests
{
    private const string EasternTimeZoneId = "America/New_York";

    [Fact]
    public void Resolve_MissingLocalTime_ReturnsMissingTime()
    {
        var result = ScheduledPublishTimeResolver.Resolve(null, EasternTimeZoneId);

        Assert.False(result.Succeeded);
        Assert.Equal(ScheduledPublishTimeResolutionStatus.MissingTime, result.Status);
    }

    [Fact]
    public void Resolve_MissingTimeZone_ReturnsInvalidTimeZone()
    {
        var result = ScheduledPublishTimeResolver.Resolve(
            new DateTime(2030, 1, 1, 12, 0, 0, DateTimeKind.Unspecified),
            string.Empty);

        Assert.False(result.Succeeded);
        Assert.Equal(ScheduledPublishTimeResolutionStatus.InvalidTimeZone, result.Status);
    }

    [Fact]
    public void Resolve_NormalLocalTime_ReturnsUtc()
    {
        var localTime = new DateTime(2030, 3, 10, 3, 30, 0, DateTimeKind.Unspecified);

        var result = ScheduledPublishTimeResolver.Resolve(localTime, EasternTimeZoneId);

        Assert.True(result.Succeeded);
        Assert.Equal(ScheduledPublishTimeResolutionStatus.Success, result.Status);
        Assert.Equal(new DateTime(2030, 3, 10, 7, 30, 0, DateTimeKind.Utc), result.UtcTime);
    }

    [Fact]
    public void Resolve_DaylightSavingGap_ReturnsInvalidLocalTime()
    {
        var localTime = new DateTime(2030, 3, 10, 2, 30, 0, DateTimeKind.Unspecified);

        var result = ScheduledPublishTimeResolver.Resolve(localTime, EasternTimeZoneId);

        Assert.False(result.Succeeded);
        Assert.Equal(ScheduledPublishTimeResolutionStatus.InvalidLocalTime, result.Status);
    }

    [Fact]
    public void Resolve_DaylightSavingOverlap_ReturnsAmbiguousLocalTime()
    {
        var localTime = new DateTime(2030, 11, 3, 1, 30, 0, DateTimeKind.Unspecified);

        var result = ScheduledPublishTimeResolver.Resolve(localTime, EasternTimeZoneId);

        Assert.False(result.Succeeded);
        Assert.Equal(ScheduledPublishTimeResolutionStatus.AmbiguousLocalTime, result.Status);
    }

    [Fact]
    public void Resolve_UnknownTimeZone_ReturnsInvalidTimeZone()
    {
        var result = ScheduledPublishTimeResolver.Resolve(
            new DateTime(2030, 1, 1, 12, 0, 0, DateTimeKind.Unspecified),
            "Invalid/Time_Zone");

        Assert.False(result.Succeeded);
        Assert.Equal(ScheduledPublishTimeResolutionStatus.InvalidTimeZone, result.Status);
    }
}
