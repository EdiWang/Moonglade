using System.Text.Json;

namespace Moonglade.ActivityLog;

public sealed record ActivityLogSettingChange(string Key, object? OldValue, object? NewValue)
{
    public const string DetailsKey = "Changes";
}

public sealed record ActivityLogSettingChangeDetail(string Key, JsonElement OldValue, JsonElement NewValue);
