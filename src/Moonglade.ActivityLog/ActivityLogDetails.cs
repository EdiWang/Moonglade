using System.Text.Json;
using System.Text.Json.Serialization;

namespace Moonglade.ActivityLog;

public static class ActivityLogDetails
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public static IReadOnlyDictionary<string, object?> Create(params (string Name, object? Value)[] properties)
    {
        var metaData = new Dictionary<string, object?>(StringComparer.Ordinal);

        foreach (var (name, value) in properties)
        {
            metaData[name] = value;
        }

        return metaData;
    }

    internal static string? Serialize(ActivityEventDefinition definition, IReadOnlyDictionary<string, object?>? details)
    {
        if (details is null || details.Count == 0) return null;

        var safe = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (var (key, value) in details)
        {
            if (!definition.DetailKeys.Contains(key) || !IsScalarOrArray(value))
            {
                throw new ArgumentException("Activity details contain an unsupported field or value.");
            }
            safe.Add(key, JsonSerializer.SerializeToElement(value, JsonOptions));
        }
        return JsonSerializer.Serialize(safe);
    }

    internal static IReadOnlyDictionary<string, JsonElement> Parse(ActivityEventDefinition definition, string? details)
    {
        if (string.IsNullOrWhiteSpace(details)) return new Dictionary<string, JsonElement>();

        var values = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(details)
            ?? throw new InvalidDataException("Activity details must be a JSON object.");
        foreach (var (key, value) in values)
        {
            if (!definition.DetailKeys.Contains(key) || !IsSafeJson(value))
            {
                throw new InvalidDataException("Stored activity details contain an unsupported field or value.");
            }
        }
        return values;
    }

    private static bool IsScalarOrArray(object? value) => value switch
    {
        null or string or bool or byte or sbyte or short or ushort or int or uint or long or ulong
            or float or double or decimal or Guid or Enum => true,
        Array array => array.Cast<object?>().All(item => item is not Array && IsScalarOrArray(item)),
        _ => false
    };

    private static bool IsSafeJson(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Null or JsonValueKind.String or JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => true,
        JsonValueKind.Array => value.EnumerateArray().All(item => item.ValueKind != JsonValueKind.Array && IsSafeJson(item)),
        _ => false
    };
}
