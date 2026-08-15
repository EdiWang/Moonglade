namespace Moonglade.Web.Commands;

public static class ScheduledPublishValidationMessages
{
    public const string MissingTime = "Please select a scheduled publish time.";
    public const string InvalidTimeZone = "Client time zone information is invalid. Please reload the page and choose the time again.";
    public const string InvalidLocalTime = "The selected time does not exist in this time zone because of a daylight-saving transition. Please choose another time.";
    public const string AmbiguousLocalTime = "The selected time occurs twice in this time zone because of a daylight-saving transition. Please choose another time.";
}

public enum ScheduledPublishTimeResolutionStatus
{
    Success,
    MissingTime,
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
    public static ScheduledPublishTimeResolution Resolve(DateTime? localTime, string timeZoneId)
    {
        if (!localTime.HasValue)
        {
            return new(ScheduledPublishTimeResolutionStatus.MissingTime, default);
        }

        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return new(ScheduledPublishTimeResolutionStatus.InvalidTimeZone, default);
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

        var wallClockTime = DateTime.SpecifyKind(localTime.Value, DateTimeKind.Unspecified);
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
