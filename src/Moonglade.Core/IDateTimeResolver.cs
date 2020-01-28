using System;

namespace Moonglade.Core
{
    public interface IDateTimeResolver
    {
        DateTime GetNowWithUserTZone();
        DateTime GetDateTimeWithUserTZone(DateTime dt);
    }
}