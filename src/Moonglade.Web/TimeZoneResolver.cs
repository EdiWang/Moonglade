namespace Moonglade.Web;

public interface ITimeZoneResolver
{
    DateTime NowOfTimeZone { get; }

    DateTime ToTimeZone(DateTime utcTime);
    DateTime ToUtc(DateTime userTime);
    IEnumerable<TimeZoneInfo> ListTimeZones();
    TimeSpan GetTimeSpanByZoneId(string timeZoneId);
}

public class BlogTimeZoneResolver : ITimeZoneResolver
{
    public TimeSpan UtcOffset { get; }

    public BlogTimeZoneResolver(IBlogConfig blogConfig) => UtcOffset = blogConfig.GeneralSettings.TimeZoneUtcOffset;

    public DateTime NowOfTimeZone => UtcToZoneTime(DateTime.UtcNow, UtcOffset);

    public DateTime ToTimeZone(DateTime utcTime) => UtcToZoneTime(utcTime, UtcOffset);

    public DateTime ToUtc(DateTime userTime) => ZoneTimeToUtc(userTime, UtcOffset);

    public IEnumerable<TimeZoneInfo> ListTimeZones() => TimeZoneInfo.GetSystemTimeZones();

    public TimeSpan GetTimeSpanByZoneId(string timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return TimeSpan.Zero;
        }

        var tz = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        return tz.BaseUtcOffset;
    }

    #region Private

    private DateTime UtcToZoneTime(DateTime utcTime, TimeSpan timeSpan)
    {
        var span = ParseTimeZone(timeSpan, out var tz);
        return tz is not null ? TimeZoneInfo.ConvertTimeFromUtc(utcTime, tz) : utcTime.AddTicks(span.Ticks);
    }

    private DateTime ZoneTimeToUtc(DateTime zoneTime, TimeSpan timeSpan)
    {
        var span = ParseTimeZone(timeSpan, out var tz);
        return tz is not null ? TimeZoneInfo.ConvertTimeToUtc(zoneTime, tz) : zoneTime.AddTicks(-1 * span.Ticks);
    }

    private TimeSpan ParseTimeZone(TimeSpan timeSpan, out TimeZoneInfo tz)
    {
        tz = ListTimeZones().FirstOrDefault(t => t.BaseUtcOffset == timeSpan);
        return timeSpan;
    }

    #endregion
}