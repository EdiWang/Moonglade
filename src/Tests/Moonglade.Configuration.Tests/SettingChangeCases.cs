using System.Reflection;

namespace Moonglade.Configuration.Tests;

internal static class SettingChangeCases
{
    // This is the accepted B0 list, intentionally independent of the production projector.
    internal static readonly IReadOnlyDictionary<string, string[]> Allowed = new Dictionary<string, string[]>
    {
        [nameof(GeneralSettings)] = ["LogoText", "SiteTitle", "SideBarOption", "WidgetsProfile", "HotTagAmount", "WidgetsCategoryList",
            "WidgetsSubscriptionButtons", "DefaultLanguageCode"],
        [nameof(ContentSettings)] = ["DefaultEditor", "PostListPageSize", "PostTitleAlignment", "ShowCalloutSection", "ShowPostFooter",
            "DocumentOutline", "EnableViewCount", "MaximumPageNumbersToDisplay"],
        [nameof(CommentSettings)] = ["CommentProvider", "CommentOrder", "EnableComments", "EnableGravatar", "EnableMarkdownLinks",
            "RequireCommentReview", "CloseCommentAfterDays", "EnableWordFilter", "WordFilterMode", "EnableCommentSectionOnMobile"],
        [nameof(NotificationSettings)] = ["EnableEmailSending", "SendEmailOnCommentReply", "SendEmailOnNewComment"],
        [nameof(FeedSettings)] = ["FeedItemCount", "UseFullContent"],
        [nameof(ImageSettings)] = ["IsWatermarkEnabled", "KeepOriginImage", "WatermarkFontSize", "WatermarkColorA", "WatermarkSkipPixel", "EnableCDNRedirect"],
        [nameof(AdvancedSettings)] = ["EnableWebmention", "EnableOpenSearch", "EnableFoaf", "EnableOpml", "EnablePostScheduler",
            "WarnPostExternalLink", "WarnCommentExternalLink"],
        [nameof(SEOSettings)] = ["EnableSiteMap", "EnableJsonLdBlogPosting", "UseDublinCoreMetaData"],
        [nameof(AppearanceSettings)] = ["ThemeId", "EnableCustomCss"],
        [nameof(CustomMenuSettings)] = ["IsEnabled"]
    };

    internal static BlogSettingsSnapshot Baseline() => new()
    {
        GeneralSettings = GeneralSettings.DefaultValue with
        {
            Copyright = "[c] 2026"
        },
        SEOSettings = SEOSettings.DefaultValue with
        {
            CanonicalPrefix = "https://before.example.invalid",
            DcLicenseUrl = "https://before.example.invalid/license",
            RobotsTxtContent = "User-agent: Before"
        },
        ContentSettings = ContentSettings.DefaultValue with { PostFooterHtmlPitch = "<p>Before footer</p>" },
        CommentSettings = CommentSettings.DefaultValue with { ThirdPartyCommentHtmlPitch = "<p>Before comments</p>" },
        NotificationSettings = NotificationSettings.DefaultValue,
        FeedSettings = FeedSettings.DefaultValue,
        ImageSettings = ImageSettings.DefaultValue with { CDNEndpoint = "https://before.example.invalid/images" },
        AdvancedSettings = AdvancedSettings.DefaultValue with
        {
            HeadScripts = "<script>beforeHead()</script>",
            FootScripts = "<script>beforeFoot()</script>"
        },
        AppearanceSettings = AppearanceSettings.DefaultValue with { CssCode = "body { color: blue; }" },
        CustomMenuSettings = new CustomMenuSettings
        {
            IsEnabled = true,
            Menus = [new Menu { Title = "Before parent", Url = "/before", Icon = "bi-star", DisplayOrder = 1,
                SubMenus = [new SubMenu { Title = "Before child", Url = "/before-child" }] }]
        }
    };

    internal static object Group(BlogSettingsSnapshot snapshot, string group) =>
        typeof(BlogSettingsSnapshot).GetProperty(group)!.GetValue(snapshot)!;

    internal static object Copy(object value) =>
        value.GetType().GetMethod("<Clone>$", BindingFlags.Public | BindingFlags.Instance)!.Invoke(value, null)!;

    internal static object Change(object before, string key)
    {
        var after = Copy(before);
        var property = before.GetType().GetProperty(key)!;
        property.SetValue(after, Alternate(property, property.GetValue(before)));
        return after;
    }

    private static object? Alternate(PropertyInfo property, object? old)
    {
        if (property.PropertyType == typeof(bool)) return !(bool)old!;
        if (property.PropertyType.IsEnum)
            return Enum.GetValues(property.PropertyType).Cast<object>().First(value => !Equals(value, old));
        if (property.PropertyType == typeof(int))
            return property.Name switch
            {
                nameof(GeneralSettings.HotTagAmount) => 20,
                nameof(ContentSettings.PostListPageSize) => 30,
                nameof(ContentSettings.MaximumPageNumbersToDisplay) => 10,
                nameof(CommentSettings.CloseCommentAfterDays) => 65536,
                nameof(ImageSettings.WatermarkFontSize) => 32,
                nameof(ImageSettings.WatermarkColorA) => 255,
                nameof(ImageSettings.WatermarkSkipPixel) => int.MaxValue,
                nameof(FeedSettings.FeedItemCount) => int.MaxValue,
                _ => 777
            };
        if (property.PropertyType == typeof(string))
            return property.Name switch
            {
                nameof(GeneralSettings.DefaultLanguageCode) => "de-DE",
                nameof(GeneralSettings.Copyright) => "[c] 2025",
                nameof(SEOSettings.CanonicalPrefix) or nameof(SEOSettings.DcLicenseUrl) or nameof(ImageSettings.CDNEndpoint) =>
                    "https://after.example.invalid",
                nameof(AdvancedSettings.HeadScripts) or nameof(AdvancedSettings.FootScripts) => "<script>changed()</script>",
                nameof(AppearanceSettings.CssCode) => "body { color: red; }",
                nameof(GeneralSettings.SiteTitle) => "After title",
                nameof(GeneralSettings.LogoText) => "After logo",
                _ => "B3-private-field"
            };
        if (property.Name == nameof(CustomMenuSettings.Menus))
            return ((CustomMenuSettings)ChangeMenu()).Menus;
        throw new InvalidOperationException($"No fixture value defined for {property.Name}.");
    }

    private static object ChangeMenu() => new CustomMenuSettings
    {
        IsEnabled = true,
        Menus = [new Menu { Title = "B3-private-menu", Url = "/after", SubMenus = [new SubMenu { Title = "Private child", Url = "/after-child" }] }]
    };
}
