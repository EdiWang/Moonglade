using System.Text.Json.Serialization;

namespace Moonglade.Configuration;

public class SystemManifestSettings : IBlogSettings
{
    public string VersionString { get; set; }
    public DateTime InstallTimeUtc { get; set; }

    [JsonIgnore]
    public static SystemManifestSettings DefaultValue => new()
    {
        VersionString = "14.7.0",
        InstallTimeUtc = DateTime.UtcNow
    };
}