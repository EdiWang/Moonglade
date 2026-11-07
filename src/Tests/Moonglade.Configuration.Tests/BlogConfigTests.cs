namespace Moonglade.Configuration.Tests;

public class BlogConfigTests
{
    [Fact]
    public void Parse_EmptyStore_ReturnsTheDefaultsAssignedToEverySetting()
    {
        var (snapshot, defaults) = BlogConfig.Parse(new Dictionary<string, string>());

        var properties = typeof(BlogSettingsSnapshot).GetProperties();
        Assert.Equal(properties.Length, defaults.Count);
        foreach (var property in properties)
        {
            Assert.Equal(property.GetValue(snapshot).ToJson(), defaults[property.Name]);
        }
    }

    [Fact]
    public void Parse_CompleteStore_PreservesSavedOrdinarySettings()
    {
        var persisted = BlogConfig.Parse(new Dictionary<string, string>()).Defaults;
        var general = GeneralSettings.DefaultValue;
        general = general with { SiteTitle = "Saved title" };
        persisted[nameof(GeneralSettings)] = general.ToJson();
        persisted[nameof(ContentSettings)] = new ContentSettings { DefaultEditor = EditorChoice.Markdown }.ToJson();
        var (snapshot, defaults) = BlogConfig.Parse(persisted);

        Assert.Empty(defaults);
        Assert.Equal("Saved title", snapshot.GeneralSettings.SiteTitle);
        Assert.Equal(EditorChoice.Markdown, snapshot.ContentSettings.DefaultEditor);
    }

    [Theory]
    [InlineData("not-json")]
    [InlineData("null")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("[]")]
    [InlineData("42")]
    [InlineData("{\"siteTitle\":[]}")]
    public void Parse_MalformedJson_ReturnsFallbackWithoutReplacingValidSettings(string? json)
    {
        var (snapshot, defaults) = BlogConfig.Parse(new Dictionary<string, string>
        {
            [nameof(GeneralSettings)] = json!,
            [nameof(ContentSettings)] = """{"postListPageSize":20}"""
        });

        Assert.Equal(GeneralSettings.DefaultValue.ToJson(), defaults[nameof(GeneralSettings)]);
        Assert.Equal(snapshot.GeneralSettings.ToJson(), defaults[nameof(GeneralSettings)]);
        Assert.DoesNotContain(nameof(ContentSettings), defaults.Keys);
        Assert.Equal(20, snapshot.ContentSettings.PostListPageSize);
    }

    [Fact]
    public void Parse_RepeatedLoads_KeepEarlierDefaultSnapshots()
    {
        var firstDefaults = BlogConfig.Parse(new Dictionary<string, string>()).Defaults;
        var originalDefaults = firstDefaults.ToDictionary();

        var secondDefaults = BlogConfig.Parse(originalDefaults).Defaults;

        Assert.Empty(secondDefaults);
        Assert.NotEmpty(firstDefaults);
        Assert.Equal(originalDefaults, firstDefaults);
    }
}
