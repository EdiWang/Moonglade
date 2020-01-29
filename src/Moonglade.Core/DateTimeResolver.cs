using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Moonglade.Configuration.Abstraction;
using TimeZoneConverter;

namespace Moonglade.Core
{
    public class DateTimeResolver : IDateTimeResolver
    {
        private readonly IBlogConfig _blogConfig;

        public DateTimeResolver(IBlogConfig blogConfig)
        {
            _blogConfig = blogConfig;
        }

        public DateTime GetNowWithUserTZone()
        {
            return UtcToZoneTime(DateTime.UtcNow, _blogConfig.GeneralSettings.TimeZoneUtcOffset);
        }

        public DateTime GetDateTimeWithUserTZone(DateTime utcDateTime)
        {
            return UtcToZoneTime(utcDateTime, _blogConfig.GeneralSettings.TimeZoneUtcOffset);
        }

        public DateTime GetUtcTimeFromUserTZone(DateTime userDateTime)
        {
            return ZoneTimeToUtc(userDateTime, _blogConfig.GeneralSettings.TimeZoneUtcOffset);
        }

        public IEnumerable<TimeZoneInfo> GetTimeZones()
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
            return null != tz ? TimeZoneInfo.ConvertTimeFromUtc(utcTime, tz) : utcTime.AddTicks(span.Ticks);
        }

        private DateTime ZoneTimeToUtc(DateTime zoneTime, string timeSpan)
        {
            var span = ParseTimeZone(timeSpan, out var tz);
            return null != tz ? TimeZoneInfo.ConvertTimeToUtc(zoneTime, tz) : zoneTime.AddTicks(-1 * span.Ticks);
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

            tz = GetTimeZones().FirstOrDefault(t => t.BaseUtcOffset == span);
            return span;
        }

        #endregion
    }
}
