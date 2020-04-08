using System.Threading.Tasks;
using Edi.Practice.RequestResponseModel;

namespace Moonglade.Configuration.Abstraction
{
    public interface IBlogConfig
    {
        GeneralSettings GeneralSettings { get; set; }
        ContentSettings ContentSettings { get; set; }
        EmailSettings EmailSettings { get; set; }
        FeedSettings FeedSettings { get; set; }
        WatermarkSettings WatermarkSettings { get; set; }
        FriendLinksSettings FriendLinksSettings { get; set; }
        AdvancedSettings AdvancedSettings { get; set; }

        Task<Response> SaveConfigurationAsync<T>(T moongladeSettings) where T : MoongladeSettings;

        void RequireRefresh();
    }
}