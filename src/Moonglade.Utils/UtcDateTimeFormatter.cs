using System.Globalization;

namespace Moonglade.Utils;

public static class UtcDateTimeFormatter
{
    public static string ToIsoString(DateTime value)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("A UTC timestamp must have DateTimeKind.Utc.", nameof(value));
        }

        return value.ToString("O", CultureInfo.InvariantCulture);
    }
}
