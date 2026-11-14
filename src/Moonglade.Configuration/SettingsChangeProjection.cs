#nullable enable

using System.Collections.Immutable;

namespace Moonglade.Configuration;

internal static class SettingsChangeProjection
{
    public static ConfigurationSaveResult Compare(object previous, object saved)
    {
        var type = saved.GetType();
        var fields = SettingsAuditFields.Get(type);
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
