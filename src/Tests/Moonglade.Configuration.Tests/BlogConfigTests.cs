namespace Moonglade.Configuration.Tests;

public class BlogConfigTests
{
    [Fact]
    public void OrdinaryConsumerContract_ExcludesProtectedState()
    {
        var properties = typeof(BlogSettingsSnapshot).GetProperties();

        Assert.Equal(9, properties.Length);
        Assert.DoesNotContain(properties, property => property.Name is "LocalAccountSettings" or "SystemManifestSettings");
        Assert.DoesNotContain(typeof(BlogConfig).GetProperties(), property => property.Name is "LocalAccountSettings" or "SystemManifestSettings");
    }

    [Fact]
    public void LoadFromConfig_EmptyStore_ReturnsTheDefaultsAssignedToEverySetting()
    {
        var config = new BlogConfig();

        var defaults = config.LoadFromConfig(new Dictionary<string, string>());

        var properties = typeof(BlogSettingsSnapshot).GetProperties();
        Assert.Equal(properties.Length, defaults.Count);
        foreach (var property in properties)
        {
            Assert.Equal(property.GetValue(config.Capture()).ToJson(), defaults[property.Name]);
        }
    }

    [Fact]
    public void LoadFromConfig_CompleteStore_PreservesSavedOrdinarySettings()
    {
        var persisted = new BlogConfig().LoadFromConfig(new Dictionary<string, string>()).ToDictionary();
        var general = GeneralSettings.DefaultValue;
        general = general with { SiteTitle = "Saved title" };
        persisted[nameof(GeneralSettings)] = general.ToJson();
        persisted[nameof(ContentSettings)] = new ContentSettings { DefaultEditor = EditorChoice.Markdown }.ToJson();
        var config = new BlogConfig();

        var defaults = config.LoadFromConfig(persisted);

        Assert.Empty(defaults);
        Assert.Equal("Saved title", config.Capture().GeneralSettings.SiteTitle);
        Assert.Equal(EditorChoice.Markdown, config.Capture().ContentSettings.DefaultEditor);
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
        Assert.Equal(config.Capture().GeneralSettings.ToJson(), defaults[nameof(GeneralSettings)]);
        Assert.DoesNotContain(nameof(ContentSettings), defaults.Keys);
        Assert.Equal(20, config.Capture().ContentSettings.PostListPageSize);
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
