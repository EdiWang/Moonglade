using Moonglade.Configuration;

namespace Moonglade.Setup.Tests;

public class SystemManifestSettingsTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("not-json-private-marker")]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("[]")]
    [InlineData("{\"installTimeUtc\":\"0001-01-01T00:00:00Z\"}")]
    [InlineData("{\"installTimeUtc\":\"2024-01-01T00:00:00\"}")]
    public void ParseStored_InvalidManifest_ThrowsKeyOnlyError(string json)
    {
        var exception = Assert.Throws<InvalidOperationException>(() => SystemManifestSettings.ParseStored(json));

        Assert.Contains(nameof(SystemManifestSettings), exception.Message);
        Assert.DoesNotContain("private-marker", exception.ToString());
        Assert.Null(exception.InnerException);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("historical-version")]
    public void ParseStored_HistoricalVersionAndUtcTime_ArePreserved(string version)
    {
        var manifest = new SystemManifestSettings
        {
            VersionString = version,
            InstallTimeUtc = new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc)
        };

        var restored = SystemManifestSettings.ParseStored(manifest.ToJson());

        Assert.Equal(manifest.ToJson(), restored.ToJson());
        Assert.Equal(DateTimeKind.Utc, restored.InstallTimeUtc.Kind);
    }
}
