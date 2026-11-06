using System.Reflection;

namespace Moonglade.Configuration.Tests;

public class PersistedSettingsDefaultsTests
{
    public static IEnumerable<object[]> OrdinarySettingNames => typeof(BlogSettingsSnapshot).GetProperties()
        .Select(property => new object[] { property.Name });

    [Theory]
    [MemberData(nameof(OrdinarySettingNames))]
    public void Load_AbsentOrdinaryFields_UseWholeGroupDefaults(string name)
    {
        var property = typeof(BlogSettingsSnapshot).GetProperty(name)!;
        var expected = property.PropertyType.GetProperty("DefaultValue", BindingFlags.Public | BindingFlags.Static)!.GetValue(null);
        var saved = new Dictionary<string, string> { [name] = "{}" };
        var config = new BlogConfig();

        var repairs = config.LoadFromConfig(saved);

        Assert.Equal(expected.ToJson(), property.GetValue(config.Capture()).ToJson());
        Assert.DoesNotContain(name, repairs.Keys);
        Assert.Equal("{}", saved[name]);
    }

    [Fact]
    public void Load_ExplicitValuesIncludingNullAndEmptyCollections_ArePreserved()
    {
        var saved = new Dictionary<string, string>
        {
            [nameof(GeneralSettings)] = """{"SITEtitle":"Saved title","canonicalPrefix":null,"hotTagAmount":0,"widgetsProfile":false}""",
            [nameof(ContentSettings)] = """{"calloutSectionHtmlPitch":null,"postListPageSize":20}""",
            [nameof(CommentSettings)] = """{"requireCommentReview":false,"enableGravatar":false}""",
            [nameof(NotificationSettings)] = """{"emailDisplayName":null}""",
            [nameof(FeedSettings)] = """{"feedItemCount":0,"useFullContent":true}""",
            [nameof(ImageSettings)] = """{"isWatermarkEnabled":false,"watermarkFontSize":8,"watermarkText":"Saved","watermarkColorA":0}""",
            [nameof(AppearanceSettings)] = """{"cssCode":null}""",
            [nameof(CustomMenuSettings)] = """{"isEnabled":false,"menus":[]}"""
        };
        var original = saved.ToDictionary();
        var config = new BlogConfig();

        var repairs = config.LoadFromConfig(saved);

        Assert.Equal("Saved title", config.Capture().GeneralSettings.SiteTitle);
        Assert.Null(config.Capture().GeneralSettings.CanonicalPrefix);
        Assert.Equal(0, config.Capture().GeneralSettings.HotTagAmount);
        Assert.False(config.Capture().GeneralSettings.WidgetsProfile);
        Assert.Equal(GeneralSettings.DefaultValue.OwnerName, config.Capture().GeneralSettings.OwnerName);
        Assert.Null(config.Capture().ContentSettings.CalloutSectionHtmlPitch);
        Assert.Equal(20, config.Capture().ContentSettings.PostListPageSize);
        Assert.False(config.Capture().CommentSettings.RequireCommentReview);
        Assert.False(config.Capture().CommentSettings.EnableGravatar);
        Assert.Null(config.Capture().NotificationSettings.EmailDisplayName);
        Assert.Equal(0, config.Capture().FeedSettings.FeedItemCount);
        Assert.True(config.Capture().FeedSettings.UseFullContent);
        Assert.False(config.Capture().ImageSettings.IsWatermarkEnabled);
        Assert.Equal(8, config.Capture().ImageSettings.WatermarkFontSize);
        Assert.Equal("Saved", config.Capture().ImageSettings.WatermarkText);
        Assert.Equal(0, config.Capture().ImageSettings.WatermarkColorA);
        Assert.Null(config.Capture().AppearanceSettings.CssCode);
        Assert.False(config.Capture().CustomMenuSettings.IsEnabled);
        Assert.Empty(config.Capture().CustomMenuSettings.Menus);
        Assert.Equal(original, saved);
        Assert.DoesNotContain(repairs.Keys, original.ContainsKey);
    }

    [Theory]
    [InlineData("""{"feedItemCount":10,"FeedItemCount":0,"futureField":true}""", 0, false)]
    [InlineData("""{"feedItemCount":"invalid","FeedItemCount":5}""", 20, true)]
    public void Load_DuplicateCaseInsensitiveFields_KeepExistingDeserializationSemantics(string json, int count, bool repaired)
    {
        var config = new BlogConfig();

        var repairs = config.LoadFromConfig(new Dictionary<string, string> { [nameof(FeedSettings)] = json });

        Assert.Equal(count, config.Capture().FeedSettings.FeedItemCount);
        Assert.Equal(repaired, repairs.ContainsKey(nameof(FeedSettings)));
    }

    [Fact]
    public void Load_ExplicitNullMenuArray_IsPreserved()
    {
        var config = new BlogConfig();

        config.LoadFromConfig(new Dictionary<string, string>
        {
            [nameof(CustomMenuSettings)] = """{"isEnabled":false,"menus":null}"""
        });

        Assert.Null(config.Capture().CustomMenuSettings.Menus);
        Assert.False(config.Capture().CustomMenuSettings.IsEnabled);
    }

    [Fact]
    public void Load_DefaultMenus_AreIndependentAcrossConfigurationInstances()
    {
        var saved = new Dictionary<string, string> { [nameof(CustomMenuSettings)] = "{}" };
        var first = new BlogConfig();
        var second = new BlogConfig();
        first.LoadFromConfig(saved);
        second.LoadFromConfig(saved);

        var original = first.Capture().CustomMenuSettings;
        var changedMenu = original.Menus[0] with
        {
            Title = "Changed",
            SubMenus = original.Menus[0].SubMenus.Add(new SubMenu { Title = "Added", Url = "/added" })
        };
        first.Publish(first.Capture() with
        {
            CustomMenuSettings = original with { Menus = original.Menus.SetItem(0, changedMenu) }
        });

        Assert.Equal("About", original.Menus[0].Title);
        Assert.Empty(original.Menus[0].SubMenus);

        Assert.Equal("About", second.Capture().CustomMenuSettings.Menus[0].Title);
        Assert.Empty(second.Capture().CustomMenuSettings.Menus[0].SubMenus);
        Assert.Equal("About", CustomMenuSettings.DefaultValue.Menus[0].Title);
    }
}
