using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Moonglade.Web.Rendering;

public static class BlogSettingsViewData
{
    private const string Key = "Moonglade.SettingsSnapshot";

    public static BlogSettingsSnapshot CaptureSettings(this ViewDataDictionary viewData, IBlogConfig reader)
    {
        if (viewData.TryGetValue(Key, out var value) && value is BlogSettingsSnapshot snapshot) return snapshot;
        snapshot = reader.Capture();
        viewData[Key] = snapshot;
        return snapshot;
    }
}
