namespace Moonglade.Web;

public interface ITimeZoneResolver
{
    DateTime NowInTimeZone { get; }

    DateTime ToTimeZone(DateTime utcTime);
    DateTime ToUtc(DateTime userTime);
    IEnumerable<TimeZoneInfo> ListTimeZones();
    TimeSpan GetTimeSpanByZoneId(string timeZoneId);
}

public class BlogTimeZoneResolver(IBlogConfig blogConfig) : ITimeZoneResolver
{
    private readonly TimeSpan _utcOffset = blogConfig.GeneralSettings.TimeZoneUtcOffset;

    public DateTime NowInTimeZone => UtcToZoneTime(DateTime.UtcNow, _utcOffset);

    public DateTime ToTimeZone(DateTime utcTime) => UtcToZoneTime(utcTime, _utcOffset);

    public DateTime ToUtc(DateTime userTime) => ZoneTimeToUtc(userTime, _utcOffset);

    public IEnumerable<TimeZoneInfo> ListTimeZones() => TimeZoneInfo.GetSystemTimeZones();

    public TimeSpan GetTimeSpanByZoneId(string timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return TimeSpan.Zero;
        }

        try
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            return tz.BaseUtcOffset;
        }
        catch (TimeZoneNotFoundException)
        {
            // Handle the case where the time zone ID is not found
            return TimeSpan.Zero;
        }
        catch (InvalidTimeZoneException)
        {
            // Handle the case where the time zone data is invalid
            return TimeSpan.Zero;
        }
    }

    #region Private

    private DateTime UtcToZoneTime(DateTime utcTime, TimeSpan timeSpan)
    {
        var span = ParseTimeZone(timeSpan, out var tz);
        return tz is not null ? TimeZoneInfo.ConvertTimeFromUtc(utcTime, tz) : utcTime.Add(span);
    }

    private DateTime ZoneTimeToUtc(DateTime zoneTime, TimeSpan timeSpan)
    {
        var span = ParseTimeZone(timeSpan, out var tz);
        return tz is not null ? TimeZoneInfo.ConvertTimeToUtc(zoneTime, tz) : zoneTime.Add(-span);
    }

    private TimeSpan ParseTimeZone(TimeSpan timeSpan, out TimeZoneInfo tz)
    {
        tz = ListTimeZones().FirstOrDefault(t => t.BaseUtcOffset == timeSpan);
        return timeSpan;
    }

    #endregion
}