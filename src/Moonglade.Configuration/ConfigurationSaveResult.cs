#nullable enable

namespace Moonglade.Configuration;

public sealed record ConfigurationSaveResult(
    Type SettingsType,
    bool HasEffectiveChanges,
    IReadOnlyList<SettingValueChange> Changes);

public sealed record SettingValueChange(string Key, object? OldValue, object? NewValue);
