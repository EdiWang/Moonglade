using Edi.AspNetCore.Utils;

namespace Moonglade.Configuration.Tests;

public class BlogConfigTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LoadFromConfig_EmptyStore_ReturnsTheDefaultsAssignedToEverySetting(bool isNew)
    {
        var config = new BlogConfig();
        var before = DateTime.UtcNow;

        var defaults = config.LoadFromConfig(new Dictionary<string, string>(), isNew);

        var after = DateTime.UtcNow;
        var properties = typeof(IBlogConfig).GetProperties();
        Assert.Equal(properties.Length, defaults.Count);
        foreach (var property in properties)
        {
            Assert.Equal(property.GetValue(config).ToJson(), defaults[property.Name]);
        }
        Assert.Equal(isNew ? VersionHelper.AppVersionBasic : "0.0.0.0", config.SystemManifestSettings.VersionString);
        Assert.InRange(config.SystemManifestSettings.InstallTimeUtc, before, after);
    }

    [Fact]
    public void LoadFromConfig_CompleteStore_PreservesSavedSettingsAndManifest()
    {
        var persisted = new BlogConfig().LoadFromConfig(new Dictionary<string, string>()).ToDictionary();
        persisted[nameof(GeneralSettings)] = new GeneralSettings { SiteTitle = "Saved title" }.ToJson();
        persisted[nameof(ContentSettings)] = new ContentSettings { DefaultEditor = EditorChoice.Markdown }.ToJson();
        var manifest = new SystemManifestSettings
        {
            VersionString = "16.3.0",
            InstallTimeUtc = new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc)
        };
        persisted[nameof(SystemManifestSettings)] = manifest.ToJson();
        var config = new BlogConfig();

        var defaults = config.LoadFromConfig(persisted, isNew: true);

        Assert.Empty(defaults);
        Assert.Equal("Saved title", config.GeneralSettings.SiteTitle);
        Assert.Equal(EditorChoice.Markdown, config.ContentSettings.DefaultEditor);
        Assert.Equal(manifest.ToJson(), config.SystemManifestSettings.ToJson());
    }

    [Theory]
    [InlineData("not-json")]
    [InlineData("null")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("[]")]
    [InlineData("42")]
    [InlineData("{\"siteTitle\":[]}")]
    public void LoadFromConfig_MalformedJson_ReturnsFallbackWithoutReplacingValidSettings(string? json)
    {
        var config = new BlogConfig();

        var defaults = config.LoadFromConfig(new Dictionary<string, string>
        {
            [nameof(GeneralSettings)] = json!,
            [nameof(ContentSettings)] = """{"postListPageSize":20}"""
        });

        Assert.Equal(GeneralSettings.DefaultValue.ToJson(), defaults[nameof(GeneralSettings)]);
        Assert.Equal(config.GeneralSettings.ToJson(), defaults[nameof(GeneralSettings)]);
        Assert.DoesNotContain(nameof(ContentSettings), defaults.Keys);
        Assert.Equal(20, config.ContentSettings.PostListPageSize);
    }

    [Theory]
    [InlineData(nameof(LocalAccountSettings), "not-json")]
    [InlineData(nameof(LocalAccountSettings), "null")]
    [InlineData(nameof(LocalAccountSettings), null)]
    [InlineData(nameof(SystemManifestSettings), "not-json")]
    [InlineData(nameof(SystemManifestSettings), "null")]
    public void LoadFromConfig_InvalidAccountOrManifest_DoesNotResetToDefaults(string key, string? json)
    {
        var config = new BlogConfig();
        var persisted = new Dictionary<string, string> { [key] = json! };

        var exception = Assert.Throws<InvalidOperationException>(() => config.LoadFromConfig(persisted));

        Assert.Contains(key, exception.Message);
        Assert.Contains("Restore", exception.Message);
        Assert.Null(exception.InnerException);
        if (key == nameof(LocalAccountSettings)) Assert.Null(config.LocalAccountSettings.PasswordHash);
    }

    [Fact]
    public void LoadFromConfig_RepeatedLoads_KeepEarlierDefaultSnapshots()
    {
        var config = new BlogConfig();
        var firstDefaults = config.LoadFromConfig(new Dictionary<string, string>());
        var originalDefaults = firstDefaults.ToDictionary();

        var secondDefaults = config.LoadFromConfig(originalDefaults);

        Assert.Empty(secondDefaults);
        Assert.NotEmpty(firstDefaults);
        Assert.Equal(originalDefaults, firstDefaults);
    }
}
