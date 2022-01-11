using TimeZoneConverter;

namespace Moonglade.Web;

public interface ITimeZoneResolver
{
    DateTime NowOfTimeZone { get; }

    DateTime ToTimeZone(DateTime utcDateTime);
    DateTime ToUtc(DateTime userDateTime);
    IEnumerable<TimeZoneInfo> ListTimeZones();
    TimeSpan GetTimeSpanByZoneId(string timeZoneId);
}

public class BlogTimeZoneResolver : ITimeZoneResolver
{
    public string UtcOffset { get; }

    public BlogTimeZoneResolver(IBlogConfig blogConfig)
    {
        UtcOffset = blogConfig.GeneralSettings.TimeZoneUtcOffset;
    }

    public DateTime NowOfTimeZone => UtcToZoneTime(DateTime.UtcNow, UtcOffset);

    public DateTime ToTimeZone(DateTime utcDateTime)
    {
        return UtcToZoneTime(utcDateTime, UtcOffset);
    }

    public DateTime ToUtc(DateTime userDateTime)
    {
        return ZoneTimeToUtc(userDateTime, UtcOffset);
    }

    public IEnumerable<TimeZoneInfo> ListTimeZones()
    {
        return TimeZoneInfo.GetSystemTimeZones();
    }

    public TimeSpan GetTimeSpanByZoneId(string timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return TimeSpan.Zero;
        }

        // Reference: https://devblogs.microsoft.com/dotnet/cross-platform-time-zones-with-net-core/
        var tz = TZConvert.GetTimeZoneInfo(timeZoneId);
        return tz.BaseUtcOffset;
    }

    #region Private

    private DateTime UtcToZoneTime(DateTime utcTime, string timeSpan)
    {
        var span = ParseTimeZone(timeSpan, out var tz);
        return tz is not null ? TimeZoneInfo.ConvertTimeFromUtc(utcTime, tz) : utcTime.AddTicks(span.Ticks);
    }

    private DateTime ZoneTimeToUtc(DateTime zoneTime, string timeSpan)
    {
        var span = ParseTimeZone(timeSpan, out var tz);
        return tz is not null ? TimeZoneInfo.ConvertTimeToUtc(zoneTime, tz) : zoneTime.AddTicks(-1 * span.Ticks);
    }

    private TimeSpan ParseTimeZone(string timeSpan, out TimeZoneInfo tz)
    {
        if (string.IsNullOrWhiteSpace(timeSpan))
        {
            timeSpan = TimeSpan.FromSeconds(0).ToString();
        }

        // Ugly code for workaround https://github.com/EdiWang/Moonglade/issues/310
        var ok = TimeSpan.TryParse(timeSpan, out var span);
        if (!ok)
        {
            throw new FormatException($"{nameof(timeSpan)} is not a valid TimeSpan format");
        }

        tz = ListTimeZones().FirstOrDefault(t => t.BaseUtcOffset == span);
        return span;
    }

    #endregion
}