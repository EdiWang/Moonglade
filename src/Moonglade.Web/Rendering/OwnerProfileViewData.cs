using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Moonglade.Web.Rendering;

public static class OwnerProfileViewData
{
    private const string PresentationKey = "Moonglade.OwnerPresentation";

    public static async Task<OwnerPresentation> CaptureOwnerPresentationAsync(
        this ViewDataDictionary viewData, IOwnerProfileReader reader, CancellationToken ct)
    {
        if (viewData.TryGetValue(PresentationKey, out var value) && value is OwnerPresentation presentation)
            return presentation;
        var profile = await reader.ReadPresentationAsync(ct);
        viewData[PresentationKey] = profile;
        return profile;
    }

    private const string Key = "Moonglade.OwnerProfile";

    public static async Task<OwnerProfile> CaptureOwnerAsync(
        this ViewDataDictionary viewData, IOwnerProfileReader reader, CancellationToken ct)
    {
        if (viewData.TryGetValue(Key, out var value)) return value as OwnerProfile;
        var owner = await reader.ReadProfileAsync(ct);
        viewData[Key] = owner;
        return owner;
    }
}
