namespace Moonglade.ActivityLog;

public enum EventType
{
    Default = 0,

    // Category operations (100-199)
    CategoryCreated = 100,
    CategoryUpdated = 101,
    CategoryDeleted = 102,

    // Post operations (200-299)
    // Reserved for future use

    // Comment operations (300-399)
    CommentCreated = 300,
    CommentApprovalToggled = 301,
    CommentDeleted = 302,
    CommentReplied = 303,

    // Page operations (400-499)
    // Reserved for future use

    // User operations (500-599)
    // Reserved for future use

    // Tag operations (600-699)
    TagCreated = 600,
    TagUpdated = 601,
    TagDeleted = 602,

    // Theme operations (700-799)
    ThemeCreated = 700,
    ThemeDeleted = 701,

    // Settings operations (800-899)
    SettingsGeneralUpdated = 800,
    SettingsContentUpdated = 801,
    SettingsCommentUpdated = 802,
    SettingsNotificationUpdated = 803,
    SettingsSubscriptionUpdated = 804,
    SettingsImageUpdated = 805,
    SettingsAdvancedUpdated = 806,
    SettingsAppearanceUpdated = 807,
    SettingsCustomMenuUpdated = 808,
    SettingsPasswordUpdated = 809,

    // System operations (900-999)
    // Reserved for future use
}
