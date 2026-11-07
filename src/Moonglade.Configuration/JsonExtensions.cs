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
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static T FromJson<T>(this string json) => JsonSerializer.Deserialize<T>(json, JsonOptions);

    internal static T FromJsonWithDefaults<T>(this string json, T defaults)
    {
        using var saved = JsonDocument.Parse(json);
        if (saved.RootElement.ValueKind != JsonValueKind.Object) return json.FromJson<T>();

        var savedNames = saved.RootElement.EnumerateObject().Select(property => property.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        using var defaultJson = JsonDocument.Parse(defaults.ToJson());
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            foreach (var property in defaultJson.RootElement.EnumerateObject())
            {
                if (!savedNames.Contains(property.Name)) property.WriteTo(writer);
            }
            // Preserve saved duplicates and their order so deserialization keeps its existing behavior.
            foreach (var property in saved.RootElement.EnumerateObject()) property.WriteTo(writer);
            writer.WriteEndObject();
        }

        return JsonSerializer.Deserialize<T>(stream.ToArray(), JsonOptions);
    }

    public static object FromJson(this string json, Type type) => JsonSerializer.Deserialize(json, type, JsonOptions);

    public static string ToJson<T>(this T obj)
    {
        return JsonSerializer.Serialize(obj, JsonOptions);
    }
}
