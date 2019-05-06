namespace Moonglade.Configuration
{
    public interface IBlogConfig
    {
        BlogOwnerSettings BlogOwnerSettings { get; set; }
        GeneralSettings GeneralSettings { get; set; }
        ContentSettings ContentSettings { get; set; }
        EmailConfiguration EmailConfiguration { get; set; }
        FeedSettings FeedSettings { get; set; }
        WatermarkSettings WatermarkSettings { get; set; }
        void Initialize(IBlogConfigurationService blogConfigurationService);
        void RequireRefresh();
    }
}