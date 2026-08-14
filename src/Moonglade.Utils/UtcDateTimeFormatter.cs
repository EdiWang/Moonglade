using System.Globalization;

namespace Moonglade.Utils;

public static class UtcDateTimeFormatter
{
    public static DateTime Normalize(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        _ => throw new ArgumentException("A UTC timestamp cannot have DateTimeKind.Local.", nameof(value))
    };

    public static string ToIsoString(DateTime value) =>
        Normalize(value).ToString("O", CultureInfo.InvariantCulture);
}
