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
        if (details is null || details.Count == 0)
        {
            if (definition.SettingKeys is not null)
                throw new ArgumentException("Settings activity requires a change envelope.");
            return null;
        }

        var safe = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (var (key, value) in details)
        {
            if (!definition.DetailKeys.Contains(key))
            {
                throw new ArgumentException("Activity details contain an unsupported field or value.");
            }
            if (key == ActivityLogSettingChange.DetailsKey)
            {
                if (value is not IReadOnlyList<ActivityLogSettingChange> changes
                    || changes.Any(change => change is null || !IsScalar(change.OldValue) || !IsScalar(change.NewValue)))
                    throw new ArgumentException("Activity setting changes contain an unsupported value.");
                var json = JsonSerializer.SerializeToElement(changes, JsonOptions);
                ValidateSettingChanges(definition, json);
                safe.Add(key, json);
            }
            else
            {
                if (!IsScalarOrArray(value))
                    throw new ArgumentException("Activity details contain an unsupported field or value.");
                safe.Add(key, JsonSerializer.SerializeToElement(value, JsonOptions));
            }
        }
        return JsonSerializer.Serialize(safe);
    }

    internal static (IReadOnlyDictionary<string, JsonElement> Details, IReadOnlyList<ActivityLogSettingChangeDetail>? SettingChanges)
        Parse(ActivityEventDefinition definition, string? details)
    {
        if (string.IsNullOrWhiteSpace(details))
        {
            if (definition.SettingKeys is not null)
                throw new InvalidDataException("Settings activity requires a change envelope.");
            return (new Dictionary<string, JsonElement>(), null);
        }

        try
        {
            using var document = JsonDocument.Parse(details);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                throw new InvalidDataException("Activity details must be a JSON object.");
            var values = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
            IReadOnlyList<ActivityLogSettingChangeDetail>? settingChanges = null;
            foreach (var field in document.RootElement.EnumerateObject())
            {
                if (!definition.DetailKeys.Contains(field.Name) || !values.TryAdd(field.Name, field.Value.Clone()))
                    throw new InvalidDataException("Stored activity details contain an unsupported or repeated field.");
                if (field.Name == ActivityLogSettingChange.DetailsKey)
                {
                    var changes = new List<ActivityLogSettingChangeDetail>();
                    ValidateSettingChanges(definition, field.Value, changes);
                    settingChanges = changes.AsReadOnly();
                }
                else if (!IsSafeJson(field.Value))
                    throw new InvalidDataException("Stored activity details contain an unsupported value.");
            }
            if (definition.SettingKeys is not null && values.Count == 0)
                throw new InvalidDataException("Settings activity requires a change envelope.");
            return (values, settingChanges);
        }
        catch (JsonException)
        {
            throw new InvalidDataException("Stored activity details contain invalid JSON.");
        }
    }

    private static void ValidateSettingChanges(ActivityEventDefinition definition, JsonElement changes,
        List<ActivityLogSettingChangeDetail>? readDetails = null)
    {
        if (definition.SettingKeys is null || changes.ValueKind != JsonValueKind.Array)
            throw new InvalidDataException("Activity setting changes are not supported for this event.");
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var change in changes.EnumerateArray())
        {
            if (change.ValueKind != JsonValueKind.Object)
                throw new InvalidDataException("Activity setting changes must contain field changes.");
            var properties = change.EnumerateObject().ToArray();
            if (properties.Length != 3 || properties.Select(property => property.Name).Distinct(StringComparer.Ordinal).Count() != 3
                || !change.TryGetProperty("Key", out var key) || key.ValueKind != JsonValueKind.String
                || !change.TryGetProperty("OldValue", out var oldValue) || !IsSafeScalarJson(oldValue)
                || !change.TryGetProperty("NewValue", out var newValue) || !IsSafeScalarJson(newValue)
                || !definition.SettingKeys.Contains(key.GetString()!) || !keys.Add(key.GetString()!)
                || JsonElement.DeepEquals(oldValue, newValue))
                throw new InvalidDataException("Activity setting changes contain an unsupported, repeated, or unchanged field.");
            readDetails?.Add(new(key.GetString()!, oldValue.Clone(), newValue.Clone()));
        }
    }

    private static bool IsScalar(object? value) => value is
        null or string or bool or byte or sbyte or short or ushort or int or uint or long or ulong
        or float or double or decimal or Guid or Enum;

    private static bool IsScalarOrArray(object? value) => IsScalar(value) || value is Array array && array.Cast<object?>().All(IsScalar);

    private static bool IsSafeScalarJson(JsonElement value) => value.ValueKind is
        JsonValueKind.Null or JsonValueKind.String or JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False;

    private static bool IsSafeJson(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Array => value.EnumerateArray().All(IsSafeScalarJson),
        _ => IsSafeScalarJson(value)
    };
}
