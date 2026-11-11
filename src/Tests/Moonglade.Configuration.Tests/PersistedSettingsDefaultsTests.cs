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
        var (snapshot, repairs) = BlogConfig.Parse(saved);

        Assert.Equal(expected.ToJson(), property.GetValue(snapshot).ToJson());
        Assert.DoesNotContain(name, repairs.Keys);
        Assert.Equal("{}", saved[name]);
    }

    [Fact]
    public void Load_ExplicitValuesIncludingNullAndEmptyCollections_ArePreserved()
    {
        var saved = new Dictionary<string, string>
        {
            [nameof(GeneralSettings)] = """{"SITEtitle":"Saved title","hotTagAmount":0,"widgetsProfile":false}""",
            [nameof(SEOSettings)] = """{"CANONICALprefix":null,"enableSiteMap":false,"dcLicenseUrl":""}""",
            [nameof(ContentSettings)] = """{"calloutSectionHtmlPitch":null,"postListPageSize":20}""",
            [nameof(CommentSettings)] = """{"requireCommentReview":false,"enableGravatar":false}""",
            [nameof(NotificationSettings)] = """{"emailDisplayName":null}""",
            [nameof(FeedSettings)] = """{"feedItemCount":0,"useFullContent":true}""",
            [nameof(ImageSettings)] = """{"isWatermarkEnabled":false,"watermarkFontSize":8,"watermarkText":"Saved","watermarkColorA":0}""",
            [nameof(AppearanceSettings)] = """{"cssCode":null}""",
            [nameof(CustomMenuSettings)] = """{"isEnabled":false,"menus":[]}"""
        };
        var original = saved.ToDictionary();
        var (snapshot, repairs) = BlogConfig.Parse(saved);

        Assert.Equal("Saved title", snapshot.GeneralSettings.SiteTitle);
        Assert.Null(snapshot.SEOSettings.CanonicalPrefix);
        Assert.False(snapshot.SEOSettings.EnableSiteMap);
        Assert.Equal("", snapshot.SEOSettings.DcLicenseUrl);
        Assert.Equal(SEOSettings.DefaultValue.MetaKeyword, snapshot.SEOSettings.MetaKeyword);
        Assert.Equal(0, snapshot.GeneralSettings.HotTagAmount);
        Assert.False(snapshot.GeneralSettings.WidgetsProfile);
        Assert.Equal(GeneralSettings.DefaultValue.Copyright, snapshot.GeneralSettings.Copyright);
        Assert.Null(snapshot.ContentSettings.CalloutSectionHtmlPitch);
        Assert.Equal(20, snapshot.ContentSettings.PostListPageSize);
        Assert.False(snapshot.CommentSettings.RequireCommentReview);
        Assert.False(snapshot.CommentSettings.EnableGravatar);
        Assert.Null(snapshot.NotificationSettings.EmailDisplayName);
        Assert.Equal(0, snapshot.FeedSettings.FeedItemCount);
        Assert.True(snapshot.FeedSettings.UseFullContent);
        Assert.False(snapshot.ImageSettings.IsWatermarkEnabled);
        Assert.Equal(8, snapshot.ImageSettings.WatermarkFontSize);
        Assert.Equal("Saved", snapshot.ImageSettings.WatermarkText);
        Assert.Equal(0, snapshot.ImageSettings.WatermarkColorA);
        Assert.Null(snapshot.AppearanceSettings.CssCode);
        Assert.False(snapshot.CustomMenuSettings.IsEnabled);
        Assert.Empty(snapshot.CustomMenuSettings.Menus);
        Assert.Equal(original, saved);
        Assert.DoesNotContain(repairs.Keys, original.ContainsKey);
    }

    [Theory]
    [InlineData("""{"feedItemCount":10,"FeedItemCount":0,"futureField":true}""", 0, false)]
    [InlineData("""{"feedItemCount":"invalid","FeedItemCount":5}""", 20, true)]
    public void Load_DuplicateCaseInsensitiveFields_KeepExistingDeserializationSemantics(string json, int count, bool repaired)
    {
        var (snapshot, repairs) = BlogConfig.Parse(new Dictionary<string, string> { [nameof(FeedSettings)] = json });

        Assert.Equal(count, snapshot.FeedSettings.FeedItemCount);
        Assert.Equal(repaired, repairs.ContainsKey(nameof(FeedSettings)));
    }

    [Fact]
    public void Load_ExplicitNullMenuArray_IsPreserved()
    {
        var (snapshot, _) = BlogConfig.Parse(new Dictionary<string, string>
        {
            [nameof(CustomMenuSettings)] = """{"isEnabled":false,"menus":null}"""
        });

        Assert.Null(snapshot.CustomMenuSettings.Menus);
        Assert.False(snapshot.CustomMenuSettings.IsEnabled);
    }

    [Fact]
    public void Load_DefaultMenus_AreIndependentAcrossConfigurationInstances()
    {
        var saved = new Dictionary<string, string> { [nameof(CustomMenuSettings)] = "{}" };
        var first = new BlogConfig(BlogConfig.Parse(saved).Snapshot);
        var second = new BlogConfig(BlogConfig.Parse(saved).Snapshot);

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
