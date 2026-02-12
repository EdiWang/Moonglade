using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace Moonglade.Data.Exporting;

public static class MoongladeJsonSerializerOptions
{
    public static JsonSerializerOptions Default => new()
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };
}