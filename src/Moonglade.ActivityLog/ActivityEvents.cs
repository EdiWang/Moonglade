using System.Collections.Frozen;

namespace Moonglade.ActivityLog;

public sealed record ActivityEventDefinition(
    string Key,
    string Group,
    string Label,
    IReadOnlySet<string> DetailKeys,
    IReadOnlySet<string>? SettingKeys = null);

public static class ActivityEvents
{
    public const string CategoryCreated = "category.created";
    public const string CategoryUpdated = "category.updated";
    public const string CategoryDeleted = "category.deleted";
    public const string PostScheduleCancelled = "post.schedule.cancelled";
    public const string PostSchedulePostponed = "post.schedule.postponed";
    public const string PostRestored = "post.restored";
    public const string PostPermanentlyDeleted = "post.permanently-deleted";
    public const string RecycleBinCleared = "post.recycle-bin.cleared";
    public const string PostCreated = "post.created";
    public const string PostUpdated = "post.updated";
    public const string PostDeleted = "post.deleted";
    public const string PostPublished = "post.published";
    public const string PostUnpublished = "post.unpublished";
    public const string CommentApprovalToggled = "comment.approval-toggled";
    public const string CommentDeleted = "comment.deleted";
    public const string CommentReplied = "comment.replied";
    public const string PageCreated = "page.created";
    public const string PageUpdated = "page.updated";
    public const string PageDeleted = "page.deleted";
    public const string PageRestored = "page.restored";
    public const string PagePermanentlyDeleted = "page.permanently-deleted";
    public const string PageRecycleBinCleared = "page.recycle-bin.cleared";
    public const string TagCreated = "tag.created";
    public const string TagUpdated = "tag.updated";
    public const string TagDeleted = "tag.deleted";
    public const string ThemeCreated = "theme.created";
    public const string ThemeDeleted = "theme.deleted";
    public const string SettingsGeneralUpdated = "settings.general.updated";
    public const string SettingsContentUpdated = "settings.content.updated";
    public const string SettingsCommentUpdated = "settings.comments.updated";
    public const string SettingsNotificationUpdated = "settings.notifications.updated";
    public const string SettingsSubscriptionUpdated = "settings.subscription.updated";
    public const string SettingsImageUpdated = "settings.images.updated";
    public const string SettingsAdvancedUpdated = "settings.advanced.updated";
    public const string SettingsAppearanceUpdated = "settings.appearance.updated";
    public const string SettingsCustomMenuUpdated = "settings.custom-menu.updated";
    public const string SettingsPasswordUpdated = "account.password.updated";
    public const string SettingsAuthenticatorReset = "account.authenticator.reset";
    public const string ImageUploaded = "image.uploaded";
    public const string AvatarUpdated = "asset.avatar.updated";
    public const string SiteIconUpdated = "asset.site-icon.updated";
    public const string SiteVerificationFileCreated = "asset.verification-file.created";
    public const string SiteVerificationFileUpdated = "asset.verification-file.updated";
    public const string SiteVerificationFileDeleted = "asset.verification-file.deleted";
    public const string SiteVerificationFileToggled = "asset.verification-file.toggled";
    public const string WidgetCreated = "widget.created";
    public const string WidgetUpdated = "widget.updated";
    public const string WidgetDeleted = "widget.deleted";
    public const string ActivityLogsCleared = "activity-log.cleared";

    private static readonly FrozenDictionary<string, ActivityEventDefinition> Definitions = new[]
    {
        Define(CategoryCreated, "Categories", "Category created", "CategoryId", "Slug"),
        Define(CategoryUpdated, "Categories", "Category updated", "Id", "CategoryId", "Slug"),
        Define(CategoryDeleted, "Categories", "Category deleted", "CategoryId"),
        Define(PostScheduleCancelled, "Posts", "Post schedule cancelled", "PostId"),
        Define(PostSchedulePostponed, "Posts", "Post schedule postponed", "PostId", "Hours"),
        Define(PostRestored, "Posts", "Post restored", "PostId"),
        Define(PostPermanentlyDeleted, "Posts", "Post permanently deleted", "PostId"),
        Define(RecycleBinCleared, "Posts", "Post recycle bin cleared", "Count"),
        Define(PostCreated, "Posts", "Post created", "PostId", "Slug", "PostStatus"),
        Define(PostUpdated, "Posts", "Post updated", "PostId", "Slug", "PostStatus"),
        Define(PostDeleted, "Posts", "Post moved to recycle bin", "PostId"),
        Define(PostPublished, "Posts", "Post published", "PostId"),
        Define(PostUnpublished, "Posts", "Post unpublished", "PostId"),
        Define(CommentApprovalToggled, "Comments", "Comment approval toggled", "CommentId"),
        Define(CommentDeleted, "Comments", "Comments deleted", "CommentIds"),
        Define(CommentReplied, "Comments", "Comment replied to", "CommentId"),
        Define(PageCreated, "Pages", "Page created", "PageId", "Slug", "IsPublished"),
        Define(PageUpdated, "Pages", "Page updated", "PageId", "Slug", "IsPublished"),
        Define(PageDeleted, "Pages", "Page moved to recycle bin", "PageId", "Slug"),
        Define(PageRestored, "Pages", "Page restored", "PageId", "Slug"),
        Define(PagePermanentlyDeleted, "Pages", "Page permanently deleted", "PageId", "Slug"),
        Define(PageRecycleBinCleared, "Pages", "Page recycle bin cleared", "Count"),
        Define(TagCreated, "Tags", "Tag created", "TagId"),
        Define(TagUpdated, "Tags", "Tag updated", "TagId"),
        Define(TagDeleted, "Tags", "Tag deleted", "TagId"),
        Define(ThemeCreated, "Themes", "Theme created", "ThemeId", "AccentColor"),
        Define(ThemeDeleted, "Themes", "Theme deleted", "ThemeId"),
        DefineSettings(SettingsGeneralUpdated, "General settings updated", ["SiteTitle", "LogoText"],
            "LogoText", "SiteTitle", "SideBarOption", "WidgetsProfile", "HotTagAmount", "WidgetsCategoryList",
            "WidgetsSubscriptionButtons", "UseGravatarAsProfilePicture", "DefaultLanguageCode", "UseDublinCoreMetaData"),
        DefineSettings(SettingsContentUpdated, "Content settings updated", [],
            "DefaultEditor", "PostListPageSize", "PostTitleAlignment", "ShowCalloutSection", "ShowPostFooter",
            "DocumentOutline", "EnableViewCount", "MaximumPageNumbersToDisplay"),
        DefineSettings(SettingsCommentUpdated, "Comment settings updated", ["EnableComments", "RequireCommentReview"],
            "CommentProvider", "CommentOrder", "EnableComments", "EnableGravatar", "EnableMarkdownLinks",
            "RequireCommentReview", "CloseCommentAfterDays", "EnableWordFilter", "WordFilterMode", "EnableCommentSectionOnMobile"),
        DefineSettings(SettingsNotificationUpdated, "Notification settings updated", [],
            "EnableEmailSending", "SendEmailOnCommentReply", "SendEmailOnNewComment"),
        DefineSettings(SettingsSubscriptionUpdated, "Subscription settings updated", [], "FeedItemCount", "UseFullContent"),
        DefineSettings(SettingsImageUpdated, "Image settings updated", ["EnableCDNRedirect", "IsWatermarkEnabled"],
            "IsWatermarkEnabled", "KeepOriginImage", "WatermarkFontSize", "WatermarkColorA", "WatermarkSkipPixel", "EnableCDNRedirect"),
        DefineSettings(SettingsAdvancedUpdated, "Advanced settings updated", [],
            "EnableWebmention", "EnableOpenSearch", "EnableFoaf", "EnableOpml", "EnableSiteMap", "EnablePostScheduler",
            "WarnPostExternalLink", "WarnCommentExternalLink", "EnableJsonLdBlogPosting"),
        DefineSettings(SettingsAppearanceUpdated, "Appearance settings updated", ["ThemeId", "EnableCustomCss"],
            "ThemeId", "EnableCustomCss"),
        DefineSettings(SettingsCustomMenuUpdated, "Custom menu settings updated", ["IsEnabled"], "IsEnabled"),
        Define(SettingsPasswordUpdated, "Account", "Account password updated", "Username"),
        Define(SettingsAuthenticatorReset, "Account", "Account authenticator reset", "Username"),
        Define(ImageUploaded, "Images", "Image uploaded", "FileName", "FileSize", "SkipWatermark"),
        Define(AvatarUpdated, "Site assets", "Avatar updated", "FileName", "FileSize", "ContentType"),
        Define(SiteIconUpdated, "Site assets", "Site icon updated", "ImageSize"),
        Define(SiteVerificationFileCreated, "Site assets", "Verification file created", "FileId", "FileName", "ContentType", "IsEnabled"),
        Define(SiteVerificationFileUpdated, "Site assets", "Verification file updated", "FileId", "OldFileName", "FileName", "ContentType", "IsEnabled"),
        Define(SiteVerificationFileDeleted, "Site assets", "Verification file deleted", "FileId", "FileName", "ContentType"),
        Define(SiteVerificationFileToggled, "Site assets", "Verification file toggled", "FileId", "FileName", "IsEnabled"),
        Define(WidgetCreated, "Widgets", "Widget created", "WidgetId", "WidgetType"),
        Define(WidgetUpdated, "Widgets", "Widget updated", "WidgetId", "WidgetType"),
        Define(WidgetDeleted, "Widgets", "Widget deleted", "WidgetId"),
        Define(ActivityLogsCleared, "Activity log", "Activity log cleared", "DeletedCount")
    }.ToFrozenDictionary(definition => definition.Key, StringComparer.Ordinal);

    public static IReadOnlyCollection<ActivityEventDefinition> All => Definitions.Values;

    public static bool IsSupported(string? key) => key is not null && Definitions.ContainsKey(key);

    public static ActivityEventDefinition Get(string key) => Definitions.TryGetValue(key, out var definition)
        ? definition
        : throw new ArgumentException("Unsupported activity event key.", nameof(key));

    private static ActivityEventDefinition Define(string key, string group, string label, params string[] detailKeys) =>
        new(key, group, label, detailKeys.ToFrozenSet(StringComparer.Ordinal));

    private static ActivityEventDefinition DefineSettings(string key, string label, string[] legacyDetailKeys, params string[] settingKeys) =>
        new(key, "Settings", label,
            legacyDetailKeys.Append(ActivityLogSettingChange.DetailsKey).ToFrozenSet(StringComparer.Ordinal),
            settingKeys.ToFrozenSet(StringComparer.Ordinal));
}
