using System;
using System.Threading.Tasks;

namespace Moonglade.Configuration
{
    public interface IBlogSettings
    {
    }

    public interface IBlogConfig
    {
        GeneralSettings GeneralSettings { get; set; }
        ContentSettings ContentSettings { get; set; }
        NotificationSettings NotificationSettings { get; set; }
        FeedSettings FeedSettings { get; set; }
        WatermarkSettings WatermarkSettings { get; set; }
        AdvancedSettings AdvancedSettings { get; set; }
        CustomStyleSheetSettings CustomStyleSheetSettings { get; set; }

        Task SaveAsync<T>(T blogSettings) where T : IBlogSettings;

        Task SaveAssetAsync(Guid assetId, string assetBase64);

        string GetAssetData(Guid assetId);
        Task<string> GetAssetDataAsync(Guid assetId);
    }
}