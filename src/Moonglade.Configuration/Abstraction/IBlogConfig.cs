using System.Threading.Tasks;

namespace Moonglade.Configuration.Abstraction
{
    public interface IBlogConfig
    {
        GeneralSettings GeneralSettings { get; set; }
        ContentSettings ContentSettings { get; set; }
        NotificationSettings NotificationSettings { get; set; }
        FeedSettings FeedSettings { get; set; }
        WatermarkSettings WatermarkSettings { get; set; }
        FriendLinksSettings FriendLinksSettings { get; set; }
        AdvancedSettings AdvancedSettings { get; set; }
        SecuritySettings SecuritySettings { get; set; }
        CustomStyleSheetSettings CustomStyleSheetSettings { get; set; }

        Task SaveConfigurationAsync<T>(T blogSettings) where T : BlogSettings;

        void RequireRefresh();
    }
}