using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace Moonglade.Configuration;

public static class JsonExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        // https://en.wikipedia.org/wiki/CJK_Unified_Ideographs_(Unicode_block)
        Encoder = JavaScriptEncoder.Create(
            UnicodeRanges.BasicLatin,
            UnicodeRanges.CjkCompatibility,
            UnicodeRanges.CjkCompatibilityForms,
            UnicodeRanges.CjkCompatibilityIdeographs,
            UnicodeRanges.CjkRadicalsSupplement,
            UnicodeRanges.CjkStrokes,
            UnicodeRanges.CjkUnifiedIdeographs,
            UnicodeRanges.CjkUnifiedIdeographsExtensionA,
            UnicodeRanges.CjkSymbolsandPunctuation,
            UnicodeRanges.HalfwidthandFullwidthForms),
        PropertyNameCaseInsensitive = true
    };

    // Workaround stupid:
    // System.InvalidOperationException: This JsonSerializerOptions instance is read-only or has already been used in serialization or deserialization.
    private static readonly JsonSerializerOptions JsonOptionsIndented = new()
    {
        Encoder = JavaScriptEncoder.Create(
            UnicodeRanges.BasicLatin,
            UnicodeRanges.CjkCompatibility,
            UnicodeRanges.CjkCompatibilityForms,
            UnicodeRanges.CjkCompatibilityIdeographs,
            UnicodeRanges.CjkRadicalsSupplement,
            UnicodeRanges.CjkStrokes,
            UnicodeRanges.CjkUnifiedIdeographs,
            UnicodeRanges.CjkUnifiedIdeographsExtensionA,
            UnicodeRanges.CjkSymbolsandPunctuation,
            UnicodeRanges.HalfwidthandFullwidthForms),
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public static T FromJson<T>(this string json) => JsonSerializer.Deserialize<T>(json, JsonOptions);

    public static string ToJson<T>(this T obj, bool writeIndented = false)
    {
        return JsonSerializer.Serialize(obj, writeIndented ? JsonOptionsIndented : JsonOptions);
    }
}