namespace Moonglade.ActivityLog;

public sealed record ActivityLogSettingChange(string Key, object? OldValue, object? NewValue)
{
    public const string DetailsKey = "Changes";
}
