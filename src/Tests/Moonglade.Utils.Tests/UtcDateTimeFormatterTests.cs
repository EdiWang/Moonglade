namespace Moonglade.Utils.Tests;

public class UtcDateTimeFormatterTests
{
    [Fact]
    public void ToIsoString_UtcValue_WritesRoundTripValueWithUtcDesignator()
    {
        var value = new DateTime(2026, 8, 15, 1, 2, 3, DateTimeKind.Utc).AddTicks(1_234_567);

        var result = UtcDateTimeFormatter.ToIsoString(value);

        Assert.Equal("2026-08-15T01:02:03.1234567Z", result);
    }

    [Fact]
    public void ToIsoString_UnspecifiedValue_Throws()
    {
        var value = new DateTime(2026, 8, 15, 1, 2, 3, DateTimeKind.Unspecified);

        var exception = Assert.Throws<ArgumentException>(() => UtcDateTimeFormatter.ToIsoString(value));

        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public void ToIsoString_LocalValue_Throws()
    {
        var value = new DateTime(2026, 8, 15, 1, 2, 3, DateTimeKind.Local);

        var exception = Assert.Throws<ArgumentException>(() => UtcDateTimeFormatter.ToIsoString(value));

        Assert.Equal("value", exception.ParamName);
    }
}
