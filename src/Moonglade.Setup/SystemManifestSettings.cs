using Edi.AspNetCore.Utils;
using Moonglade.Configuration;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Moonglade.Setup;

public class SystemManifestSettings : IValidatableObject
{
    public string VersionString { get; set; }
    public DateTime InstallTimeUtc { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (InstallTimeUtc == default || InstallTimeUtc.Kind != DateTimeKind.Utc)
            yield return new("Installation time must be a non-minimum UTC timestamp.", [nameof(InstallTimeUtc)]);
    }

    public static SystemManifestSettings ParseStored(string json)
    {
        try
        {
            var value = json?.FromJson<SystemManifestSettings>();
            if (value is not null && Validator.TryValidateObject(value, new ValidationContext(value), null, true)) return value;
        }
        catch (JsonException)
        {
        }
        throw new InvalidOperationException($"Stored configuration '{nameof(SystemManifestSettings)}' is invalid. Restore a valid value before restarting.");
    }

    [JsonIgnore]
    public static SystemManifestSettings DefaultValue => new()
    {
        VersionString = "0.0.0.0",
        InstallTimeUtc = DateTime.UtcNow
    };

    [JsonIgnore]
    public static SystemManifestSettings DefaultValueNew => new()
    {
        VersionString = VersionHelper.AppVersionBasic,
        InstallTimeUtc = DateTime.UtcNow
    };
}
