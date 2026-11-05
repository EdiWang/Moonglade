using Edi.AspNetCore.Utils;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

namespace Moonglade.Configuration;

public class SystemManifestSettings : IBlogSettings<SystemManifestSettings>, IValidatableObject
{
    public string VersionString { get; set; }
    public DateTime InstallTimeUtc { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (InstallTimeUtc == default || InstallTimeUtc.Kind != DateTimeKind.Utc)
            yield return new("Installation time must be a non-minimum UTC timestamp.", [nameof(InstallTimeUtc)]);
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
