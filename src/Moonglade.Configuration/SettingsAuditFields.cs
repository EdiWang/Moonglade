#nullable enable

using System.Collections.Immutable;

namespace Moonglade.Configuration;

public static class SettingsAuditFields
{
    public static ImmutableArray<string> Get(Type type) => type.Name switch
    {
        nameof(GeneralSettings) =>
        [
            nameof(GeneralSettings.LogoText), nameof(GeneralSettings.SiteTitle),
            nameof(GeneralSettings.SideBarOption), nameof(GeneralSettings.WidgetsProfile),
            nameof(GeneralSettings.HotTagAmount), nameof(GeneralSettings.WidgetsCategoryList),
            nameof(GeneralSettings.WidgetsSubscriptionButtons),
            nameof(GeneralSettings.DefaultLanguageCode)
        ],
        nameof(ContentSettings) =>
        [
            nameof(ContentSettings.DefaultEditor), nameof(ContentSettings.PostListPageSize),
            nameof(ContentSettings.PostTitleAlignment), nameof(ContentSettings.ShowCalloutSection),
            nameof(ContentSettings.ShowPostFooter), nameof(ContentSettings.DocumentOutline),
            nameof(ContentSettings.EnableViewCount), nameof(ContentSettings.MaximumPageNumbersToDisplay)
        ],
        nameof(CommentSettings) =>
        [
            nameof(CommentSettings.CommentProvider), nameof(CommentSettings.CommentOrder),
            nameof(CommentSettings.EnableComments), nameof(CommentSettings.EnableGravatar),
            nameof(CommentSettings.EnableMarkdownLinks), nameof(CommentSettings.RequireCommentReview),
            nameof(CommentSettings.CloseCommentAfterDays), nameof(CommentSettings.EnableWordFilter),
            nameof(CommentSettings.WordFilterMode), nameof(CommentSettings.EnableCommentSectionOnMobile)
        ],
        nameof(NotificationSettings) =>
        [
            nameof(NotificationSettings.EnableEmailSending), nameof(NotificationSettings.SendEmailOnCommentReply),
            nameof(NotificationSettings.SendEmailOnNewComment)
        ],
        nameof(SEOSettings) =>
        [
            nameof(SEOSettings.EnableSiteMap), nameof(SEOSettings.EnableJsonLdBlogPosting),
            nameof(SEOSettings.UseDublinCoreMetaData)
        ],
        nameof(FeedSettings) => [nameof(FeedSettings.FeedItemCount), nameof(FeedSettings.UseFullContent)],
        nameof(ImageSettings) =>
        [
            nameof(ImageSettings.IsWatermarkEnabled), nameof(ImageSettings.KeepOriginImage),
            nameof(ImageSettings.WatermarkFontSize), nameof(ImageSettings.WatermarkColorA),
            nameof(ImageSettings.WatermarkSkipPixel), nameof(ImageSettings.EnableCDNRedirect)
        ],
        nameof(AdvancedSettings) =>
        [
            nameof(AdvancedSettings.EnableWebmention), nameof(AdvancedSettings.EnableOpenSearch),
            nameof(AdvancedSettings.EnableFoaf), nameof(AdvancedSettings.EnableOpml),
            nameof(AdvancedSettings.EnablePostScheduler), nameof(AdvancedSettings.WarnPostExternalLink),
            nameof(AdvancedSettings.WarnCommentExternalLink)
        ],
        nameof(AppearanceSettings) => [nameof(AppearanceSettings.ThemeId), nameof(AppearanceSettings.EnableCustomCss)],
        nameof(CustomMenuSettings) => [nameof(CustomMenuSettings.IsEnabled)],
        _ => throw new InvalidOperationException("Unsupported ordinary settings type.")
    };
}
