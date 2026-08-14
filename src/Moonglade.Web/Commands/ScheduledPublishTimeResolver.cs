namespace Moonglade.Web.Commands;

public enum ScheduledPublishTimeResolutionStatus
{
    Success,
    MissingTimeZone,
    InvalidTimeZone,
    InvalidLocalTime,
    AmbiguousLocalTime
}

public readonly record struct ScheduledPublishTimeResolution(
    ScheduledPublishTimeResolutionStatus Status,
    DateTime UtcTime)
{
    public bool Succeeded => Status == ScheduledPublishTimeResolutionStatus.Success;
}

public static class ScheduledPublishTimeResolver
{
    public static ScheduledPublishTimeResolution Resolve(DateTime localTime, string timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return new(ScheduledPublishTimeResolutionStatus.MissingTimeZone, default);
        }

        TimeZoneInfo timeZone;
        try
        {
            timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            return new(ScheduledPublishTimeResolutionStatus.InvalidTimeZone, default);
        }
        catch (InvalidTimeZoneException)
        {
            return new(ScheduledPublishTimeResolutionStatus.InvalidTimeZone, default);
        }

        var wallClockTime = DateTime.SpecifyKind(localTime, DateTimeKind.Unspecified);
        if (timeZone.IsInvalidTime(wallClockTime))
        {
            return new(ScheduledPublishTimeResolutionStatus.InvalidLocalTime, default);
        }

        if (timeZone.IsAmbiguousTime(wallClockTime))
        {
            return new(ScheduledPublishTimeResolutionStatus.AmbiguousLocalTime, default);
        }

        return new(
            ScheduledPublishTimeResolutionStatus.Success,
            TimeZoneInfo.ConvertTimeToUtc(wallClockTime, timeZone));
    }
}
