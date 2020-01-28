using System;
using System.Collections.Generic;
using System.Text;
using Moonglade.Configuration.Abstraction;

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
            return Utils.UtcToZoneTime(DateTime.UtcNow, _blogConfig.GeneralSettings.TimeZoneUtcOffset);
        }

        public DateTime GetDateTimeWithUserTZone(DateTime dt)
        {
            return Utils.UtcToZoneTime(dt, _blogConfig.GeneralSettings.TimeZoneUtcOffset);
        }

        public DateTime GetUtcTimeFromUserTZone(DateTime dt)
        {
            return Utils.ZoneTimeToUtc(dt, _blogConfig.GeneralSettings.TimeZoneUtcOffset);
        }
    }
}
