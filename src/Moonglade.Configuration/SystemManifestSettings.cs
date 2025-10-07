using Edi.AspNetCore.Utils;
using System.Text.Json.Serialization;

namespace Moonglade.Configuration;

public class SystemManifestSettings : IBlogSettings
{
    public string VersionString { get; set; }
    public DateTime InstallTimeUtc { get; set; }

    [JsonIgnore]
    public static SystemManifestSettings DefaultValue => new()
    {
        VersionString = "0.0.0.0", // to trigger a database update
        InstallTimeUtc = DateTime.UtcNow
    };

    [JsonIgnore]
    public static SystemManifestSettings DefaultValueNew => new()
    {
        VersionString = VersionHelper.AppVersionBasic,
        InstallTimeUtc = DateTime.UtcNow
    };
}