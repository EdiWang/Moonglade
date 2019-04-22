using Newtonsoft.Json;

namespace Moonglade.Configuration
{
    public class BlogConfig
    {
        public string DisharmonyWords { get; set; }

        public string MetaKeyword { get; set; }

        public string MetaAuthor { get; set; }

        public string SiteTitle { get; set; }

        public string BloggerAvatarBase64 { get; set; }

        public string BloggerName { get; set; }

        public bool EnableComments { get; set; }

        public EmailConfiguration EmailConfiguration { get; set; }

        public FeedSettings FeedSettings { get; set; }

        public WatermarkSettings WatermarkSettings { get; set; }

        private bool _hasInitialized;

        public BlogConfig()
        {
            // Set default values in case of blow up
            DisharmonyWords = string.Empty;
            MetaKeyword = string.Empty;
            MetaAuthor = string.Empty;
            SiteTitle = string.Empty;
            EnableComments = true;
            EmailConfiguration = new EmailConfiguration();
            FeedSettings = new FeedSettings();
            WatermarkSettings = new WatermarkSettings();
        }

        public void GetConfiguration(BlogConfigurationService blogConfigurationService)
        {
            if (!_hasInitialized)
            {
                var cfgDic = blogConfigurationService.GetAllConfigurations();
                DisharmonyWords = cfgDic[nameof(DisharmonyWords)];
                MetaKeyword = cfgDic[nameof(MetaKeyword)];
                MetaAuthor = cfgDic[nameof(MetaAuthor)];
                SiteTitle = cfgDic[nameof(SiteTitle)];
                BloggerAvatarBase64 = cfgDic[nameof(BloggerAvatarBase64)];
                BloggerName = cfgDic[nameof(BloggerName)];
                EnableComments = bool.Parse(cfgDic[nameof(EnableComments)]);

                EmailConfiguration = JsonConvert.DeserializeObject<EmailConfiguration>(cfgDic[nameof(EmailConfiguration)]);
                if (!string.IsNullOrWhiteSpace(EmailConfiguration.SmtpPassword))
                {
                    EmailConfiguration.SmtpPassword =
                        blogConfigurationService.DecryptPassword(EmailConfiguration.SmtpPassword);
                }

                FeedSettings = JsonConvert.DeserializeObject<FeedSettings>(cfgDic[nameof(FeedSettings)]);
                WatermarkSettings = JsonConvert.DeserializeObject<WatermarkSettings>(cfgDic[nameof(WatermarkSettings)]);

                _hasInitialized = true;
            }
        }

        public void DumpOldValuesWhenNextLoad()
        {
            _hasInitialized = false;
        }
    }
}