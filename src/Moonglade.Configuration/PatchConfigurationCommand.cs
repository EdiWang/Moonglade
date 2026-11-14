using LiteBus.Commands.Abstractions;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Moonglade.Configuration;

public sealed class PatchConfigurationCommand : ICommand<ConfigurationSaveResult>
{
    private static readonly JsonSerializerOptions FieldJson = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly Func<object, object> apply;

    private PatchConfigurationCommand(Type settingsType, Func<object, object> apply)
    {
        SettingsType = settingsType;
        this.apply = apply;
    }

    internal Type SettingsType { get; }

    internal object Apply(object current) => apply(current);

    public static PatchConfigurationCommand Create<T>(
        Func<T, T> copy,
        JsonElement fields,
        params string[] editableFields) where T : IBlogSettings<T>
    {
        if (fields.ValueKind != JsonValueKind.Object)
            throw new ValidationException("A settings object is required.");

        var allowed = editableFields.ToDictionary(name => name, name => typeof(T).GetProperty(name), StringComparer.OrdinalIgnoreCase);
        var updates = new Dictionary<PropertyInfo, object>();
        foreach (var field in fields.EnumerateObject())
        {
            if (!allowed.TryGetValue(field.Name, out var property))
                throw InvalidField(field.Name, "This setting cannot be edited.");

            try
            {
                if (!updates.TryAdd(property, field.Value.Deserialize(property.PropertyType, FieldJson)))
                    throw InvalidField(property.Name, "This setting can only be supplied once.");
            }
            catch (JsonException)
            {
                throw InvalidField(property.Name, "The setting value is invalid.");
            }
        }

        if (updates.Count == 0)
            throw new ValidationException("At least one setting is required.");

        return new(typeof(T), current =>
        {
            var candidate = copy((T)current);
            foreach (var (property, value) in updates)
                property.SetValue(candidate, value);
            return candidate;
        });
    }

    private static ValidationException InvalidField(string field, string message) =>
        new(new ValidationResult(message, [field]), null, null);
}
