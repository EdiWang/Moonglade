#nullable enable

using System.Collections.Immutable;

namespace Moonglade.Configuration;

internal static class SettingsChangeProjection
{
    public static ConfigurationSaveResult Compare(object previous, object saved)
    {
        var type = saved.GetType();
        var fields = Fields(type);
        var changed = previous is CustomMenuSettings oldMenu && saved is CustomMenuSettings newMenu
            ? oldMenu.IsEnabled != newMenu.IsEnabled || !MenusEqual(oldMenu.Menus, newMenu.Menus)
            : !previous.Equals(saved);
        if (!changed) return new(type, false, []);

        var changes = new List<SettingValueChange>();
        foreach (var field in fields)
        {
            var property = type.GetProperty(field)
                ?? throw new InvalidOperationException("An approved setting field is not defined.");
            var oldValue = property.GetValue(previous);
            var newValue = property.GetValue(saved);
            if (!Equals(oldValue, newValue)) changes.Add(new(field, oldValue, newValue));
        }
        return new(type, true, changes.AsReadOnly());
    }

    private static string[] Fields(Type type) => type.Name switch
    {
        nameof(GeneralSettings) =>
        [
            nameof(GeneralSettings.LogoText), nameof(GeneralSettings.SiteTitle),
            nameof(GeneralSettings.SideBarOption), nameof(GeneralSettings.WidgetsProfile),
            nameof(GeneralSettings.HotTagAmount), nameof(GeneralSettings.WidgetsCategoryList),
            nameof(GeneralSettings.WidgetsSubscriptionButtons), nameof(GeneralSettings.UseGravatarAsProfilePicture),
            nameof(GeneralSettings.DefaultLanguageCode), nameof(GeneralSettings.UseDublinCoreMetaData)
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
            nameof(AdvancedSettings.EnableSiteMap), nameof(AdvancedSettings.EnablePostScheduler),
            nameof(AdvancedSettings.WarnPostExternalLink), nameof(AdvancedSettings.WarnCommentExternalLink),
            nameof(AdvancedSettings.EnableJsonLdBlogPosting)
        ],
        nameof(AppearanceSettings) => [nameof(AppearanceSettings.ThemeId), nameof(AppearanceSettings.EnableCustomCss)],
        nameof(CustomMenuSettings) => [nameof(CustomMenuSettings.IsEnabled)],
        _ => throw new InvalidOperationException("Unsupported ordinary settings type.")
    };

    private static bool MenusEqual(ImmutableList<Menu>? previous, ImmutableList<Menu>? saved)
    {
        if (ReferenceEquals(previous, saved)) return true;
        if (previous is null || saved is null || previous.Count != saved.Count) return false;
        for (var index = 0; index < previous.Count; index++)
        {
            var oldMenu = previous[index];
            var newMenu = saved[index];
            // Records compare immutable lists by reference; compare menu scalars and submenu values separately.
            if (oldMenu with { SubMenus = newMenu.SubMenus } != newMenu) return false;
            if (ReferenceEquals(oldMenu.SubMenus, newMenu.SubMenus)) continue;
            if (oldMenu.SubMenus is null || newMenu.SubMenus is null
                || !oldMenu.SubMenus.SequenceEqual(newMenu.SubMenus)) return false;
        }
        return true;
    }
}
