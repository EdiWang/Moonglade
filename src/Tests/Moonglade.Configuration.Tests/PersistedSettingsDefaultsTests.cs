using System.Reflection;

namespace Moonglade.Configuration.Tests;

public class PersistedSettingsDefaultsTests
{
    public static IEnumerable<object[]> OrdinarySettingNames => typeof(IBlogConfig).GetProperties()
        .Where(property => property.Name != nameof(LocalAccountSettings) && property.Name != nameof(SystemManifestSettings))
        .Select(property => new object[] { property.Name });

    [Theory]
    [MemberData(nameof(OrdinarySettingNames))]
    public void Load_AbsentOrdinaryFields_UseWholeGroupDefaults(string name)
    {
        var property = typeof(IBlogConfig).GetProperty(name)!;
        var expected = property.PropertyType.GetProperty("DefaultValue", BindingFlags.Public | BindingFlags.Static)!.GetValue(null);
        var saved = new Dictionary<string, string> { [name] = "{}" };
        var config = new BlogConfig();

        var repairs = config.LoadFromConfig(saved);

        Assert.Equal(expected.ToJson(), property.GetValue(config).ToJson());
        Assert.DoesNotContain(name, repairs.Keys);
        Assert.Equal("{}", saved[name]);
    }

    [Fact]
    public void Load_ExplicitValuesIncludingNullAndEmptyCollections_ArePreserved()
    {
        var saved = new Dictionary<string, string>
        {
            [nameof(GeneralSettings)] = """{"SITEtitle":"Saved title","ownerEmail":null,"hotTagAmount":0,"widgetsProfile":false}""",
            [nameof(ContentSettings)] = """{"calloutSectionHtmlPitch":null,"postListPageSize":20}""",
            [nameof(CommentSettings)] = """{"requireCommentReview":false,"enableGravatar":false}""",
            [nameof(NotificationSettings)] = """{"emailDisplayName":null}""",
            [nameof(FeedSettings)] = """{"feedItemCount":0,"useFullContent":true}""",
            [nameof(ImageSettings)] = """{"isWatermarkEnabled":false,"watermarkFontSize":0,"watermarkText":null}""",
            [nameof(AppearanceSettings)] = """{"cssCode":null}""",
            [nameof(CustomMenuSettings)] = """{"isEnabled":false,"menus":[]}"""
        };
        var original = saved.ToDictionary();
        var config = new BlogConfig();

        var repairs = config.LoadFromConfig(saved);

        Assert.Equal("Saved title", config.GeneralSettings.SiteTitle);
        Assert.Null(config.GeneralSettings.OwnerEmail);
        Assert.Equal(0, config.GeneralSettings.HotTagAmount);
        Assert.False(config.GeneralSettings.WidgetsProfile);
        Assert.Equal(GeneralSettings.DefaultValue.OwnerName, config.GeneralSettings.OwnerName);
        Assert.Null(config.ContentSettings.CalloutSectionHtmlPitch);
        Assert.Equal(20, config.ContentSettings.PostListPageSize);
        Assert.False(config.CommentSettings.RequireCommentReview);
        Assert.False(config.CommentSettings.EnableGravatar);
        Assert.Null(config.NotificationSettings.EmailDisplayName);
        Assert.Equal(0, config.FeedSettings.FeedItemCount);
        Assert.True(config.FeedSettings.UseFullContent);
        Assert.False(config.ImageSettings.IsWatermarkEnabled);
        Assert.Equal(0, config.ImageSettings.WatermarkFontSize);
        Assert.Null(config.ImageSettings.WatermarkText);
        Assert.Null(config.AppearanceSettings.CssCode);
        Assert.False(config.CustomMenuSettings.IsEnabled);
        Assert.Empty(config.CustomMenuSettings.Menus);
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

        Assert.Equal(count, config.FeedSettings.FeedItemCount);
        Assert.Equal(repaired, repairs.ContainsKey(nameof(FeedSettings)));
    }

    [Fact]
    public void Load_ProtectedPartialObjects_DoNotAcquireBootstrapDefaults()
    {
        var config = new BlogConfig();

        var repairs = config.LoadFromConfig(new Dictionary<string, string>
        {
            [nameof(LocalAccountSettings)] = "{}",
            [nameof(SystemManifestSettings)] = "{}"
        });

        Assert.Null(config.LocalAccountSettings.Username);
        Assert.Null(config.LocalAccountSettings.PasswordHash);
        Assert.Null(config.LocalAccountSettings.PasswordSalt);
        Assert.Null(config.SystemManifestSettings.VersionString);
        Assert.Equal(DateTime.MinValue, config.SystemManifestSettings.InstallTimeUtc);
        Assert.DoesNotContain(nameof(LocalAccountSettings), repairs.Keys);
        Assert.DoesNotContain(nameof(SystemManifestSettings), repairs.Keys);
    }

    [Fact]
    public void Load_ExplicitNullMenuArray_IsPreserved()
    {
        var config = new BlogConfig();

        config.LoadFromConfig(new Dictionary<string, string>
        {
            [nameof(CustomMenuSettings)] = """{"menus":null}"""
        });

        Assert.Null(config.CustomMenuSettings.Menus);
        Assert.True(config.CustomMenuSettings.IsEnabled);
    }

    [Fact]
    public void Load_DefaultMenus_AreIndependentAcrossConfigurationInstances()
    {
        var saved = new Dictionary<string, string> { [nameof(CustomMenuSettings)] = "{}" };
        var first = new BlogConfig();
        var second = new BlogConfig();
        first.LoadFromConfig(saved);
        second.LoadFromConfig(saved);

        first.CustomMenuSettings.Menus[0].Title = "Changed";
        first.CustomMenuSettings.Menus[0].SubMenus.Add(new SubMenu { Title = "Added" });

        Assert.Equal("About", second.CustomMenuSettings.Menus[0].Title);
        Assert.Empty(second.CustomMenuSettings.Menus[0].SubMenus);
        Assert.Equal("About", CustomMenuSettings.DefaultValue.Menus[0].Title);
    }
}
