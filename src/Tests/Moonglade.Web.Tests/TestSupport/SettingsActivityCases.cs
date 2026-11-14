using Moonglade.ActivityLog;
using Moonglade.Configuration;

namespace Moonglade.Web.Tests.TestSupport;

internal static class SettingsActivityCases
{
    internal sealed record GroupCase(string Group, string EventKey, string[] Allowed);

    // The accepted B0 contract, not obtained from the production event definitions.
    internal static readonly GroupCase[] Groups =
    [
        new("general", ActivityEvents.SettingsGeneralUpdated, ["LogoText", "SiteTitle", "SideBarOption", "WidgetsProfile", "HotTagAmount",
            "WidgetsCategoryList", "WidgetsSubscriptionButtons", "DefaultLanguageCode"]),
        new("content", ActivityEvents.SettingsContentUpdated, ["DefaultEditor", "PostListPageSize", "PostTitleAlignment", "ShowCalloutSection",
            "ShowPostFooter", "DocumentOutline", "EnableViewCount", "MaximumPageNumbersToDisplay"]),
        new("comment", ActivityEvents.SettingsCommentUpdated, ["CommentProvider", "CommentOrder", "EnableComments", "EnableGravatar",
            "EnableMarkdownLinks", "RequireCommentReview", "CloseCommentAfterDays", "EnableWordFilter", "WordFilterMode", "EnableCommentSectionOnMobile"]),
        new("notification", ActivityEvents.SettingsNotificationUpdated, ["EnableEmailSending", "SendEmailOnCommentReply", "SendEmailOnNewComment"]),
        new("subscription", ActivityEvents.SettingsSubscriptionUpdated, ["FeedItemCount", "UseFullContent"]),
        new("watermark", ActivityEvents.SettingsImageUpdated, ["IsWatermarkEnabled", "KeepOriginImage", "WatermarkFontSize", "WatermarkColorA",
            "WatermarkSkipPixel", "EnableCDNRedirect"]),
        new("advanced", ActivityEvents.SettingsAdvancedUpdated, ["EnableWebmention", "EnableOpenSearch", "EnableFoaf", "EnableOpml",
            "EnablePostScheduler", "WarnPostExternalLink", "WarnCommentExternalLink"]),
        new("seo", ActivityEvents.SettingsSeoUpdated, ["EnableSiteMap", "EnableJsonLdBlogPosting", "UseDublinCoreMetaData"]),
        new("appearance", ActivityEvents.SettingsAppearanceUpdated, ["ThemeId", "EnableCustomCss"]),
        new("custom-menu", ActivityEvents.SettingsCustomMenuUpdated, ["IsEnabled"])
    ];

    internal static readonly IReadOnlyDictionary<string, string> Labels = new Dictionary<string, string>
    {
        ["LogoText"] = "Logo text",
        ["SiteTitle"] = "Site title",
        ["SideBarOption"] = "Side bar display",
        ["WidgetsProfile"] = "Profile",
        ["HotTagAmount"] = "How many tags to show on sidebar",
        ["WidgetsCategoryList"] = "Categories",
        ["WidgetsSubscriptionButtons"] = "Subscription buttons",
        ["UseGravatarAsProfilePicture"] = "Use my Gravatar as profile picture",
        ["DefaultLanguageCode"] = "Default language",
        ["UseDublinCoreMetaData"] = "Use Dublin Core Metadata",
        ["DefaultEditor"] = "Default editor",
        ["PostListPageSize"] = "Post list page size",
        ["PostTitleAlignment"] = "Post title alignment",
        ["ShowCalloutSection"] = "Show call-out section",
        ["ShowPostFooter"] = "Show customize footer on each post",
        ["DocumentOutline"] = "Show post outline as side navigation",
        ["EnableViewCount"] = "Enable view count",
        ["MaximumPageNumbersToDisplay"] = "Maximum page numbers to display in pagination",
        ["CommentProvider"] = "Comment provider",
        ["CommentOrder"] = "Comments display order",
        ["EnableComments"] = "Enable comments",
        ["EnableGravatar"] = "Enable Gravatar in comment list",
        ["EnableMarkdownLinks"] = "Enable Markdown links in comments and replies",
        ["RequireCommentReview"] = "Require comment review",
        ["CloseCommentAfterDays"] = "Automatically close comments on posts older than x days",
        ["EnableWordFilter"] = "Enable word filter",
        ["WordFilterMode"] = "Word filter mode",
        ["EnableCommentSectionOnMobile"] = "Show comment section on mobile screens",
        ["EnableEmailSending"] = "Enable email sending",
        ["SendEmailOnCommentReply"] = "Send email on comment reply",
        ["SendEmailOnNewComment"] = "Send email on new comment",
        ["FeedItemCount"] = "Feed items",
        ["UseFullContent"] = "Use full blog post content instead of abstract",
        ["IsWatermarkEnabled"] = "Enable watermark",
        ["KeepOriginImage"] = "Keep origin image",
        ["WatermarkFontSize"] = "Font size",
        ["WatermarkColorA"] = "Watermark opacity",
        ["WatermarkSkipPixel"] = "Watermark skip pixel threshold",
        ["EnableCDNRedirect"] = "Enable CDN redirect",
        ["EnableWebmention"] = "Enable Webmention",
        ["EnableOpenSearch"] = "Enable OpenSearch",
        ["EnableFoaf"] = "Enable FOAF",
        ["EnableOpml"] = "Enable OPML",
        ["EnableSiteMap"] = "Enable Site Map",
        ["EnablePostScheduler"] = "Enable Post Scheduler",
        ["WarnPostExternalLink"] = "Show warning when clicking external links in posts",
        ["WarnCommentExternalLink"] = "Show warning when clicking external links in comments",
        ["EnableJsonLdBlogPosting"] = "Enable JSON-LD BlogPosting",
        ["ThemeId"] = "Theme ID",
        ["EnableCustomCss"] = "Enable Custom CSS",
        ["IsEnabled"] = "Enabled"
    };

    internal static object Get(BlogSettingsSnapshot root, string group) => group switch
    {
        "general" => root.GeneralSettings,
        "content" => root.ContentSettings,
        "comment" => root.CommentSettings,
        "notification" => root.NotificationSettings,
        "subscription" => root.FeedSettings,
        "watermark" => root.ImageSettings,
        "advanced" => root.AdvancedSettings,
        "seo" => root.SEOSettings,
        "appearance" => root.AppearanceSettings,
        _ => root.CustomMenuSettings
    };
}
