using Moonglade.Configuration.Abstraction;
using Newtonsoft.Json;

namespace Moonglade.Configuration
{
    public class BlogConfig : IBlogConfig
    {
        public BlogOwnerSettings BlogOwnerSettings { get; set; }

        public GeneralSettings GeneralSettings { get; set; }

        public ContentSettings ContentSettings { get; set; }

        public EmailConfiguration EmailConfiguration { get; set; }

        public FeedSettings FeedSettings { get; set; }

        public WatermarkSettings WatermarkSettings { get; set; }

        private bool _hasInitialized;

        public BlogConfig()
        {
            BlogOwnerSettings = new BlogOwnerSettings();
            ContentSettings = new ContentSettings();
            GeneralSettings = new GeneralSettings();
            EmailConfiguration = new EmailConfiguration();
            FeedSettings = new FeedSettings();
            WatermarkSettings = new WatermarkSettings();
        }

        public void Initialize(IBlogConfigurationService blogConfigurationService)
        {
            if (!_hasInitialized)
            {
                var cfgDic = blogConfigurationService.GetAllConfigurations();

                BlogOwnerSettings = JsonConvert.DeserializeObject<BlogOwnerSettings>(cfgDic[nameof(BlogOwnerSettings)]);
                GeneralSettings = JsonConvert.DeserializeObject<GeneralSettings>(cfgDic[nameof(GeneralSettings)]);
                ContentSettings = JsonConvert.DeserializeObject<ContentSettings>(cfgDic[nameof(ContentSettings)]);

                EmailConfiguration = JsonConvert.DeserializeObject<EmailConfiguration>(cfgDic[nameof(EmailConfiguration)]);
                if (!string.IsNullOrWhiteSpace(EmailConfiguration.SmtpPassword))
                {
                    EmailConfiguration.SmtpClearPassword =
                        blogConfigurationService.DecryptPassword(EmailConfiguration.SmtpPassword);
                }

                FeedSettings = JsonConvert.DeserializeObject<FeedSettings>(cfgDic[nameof(FeedSettings)]);
                WatermarkSettings = JsonConvert.DeserializeObject<WatermarkSettings>(cfgDic[nameof(WatermarkSettings)]);

                _hasInitialized = true;
            }
        }

        public void RequireRefresh()
        {
            _hasInitialized = false;
        }
    }
}