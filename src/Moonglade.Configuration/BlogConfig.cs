using Newtonsoft.Json;

namespace Moonglade.Configuration
{
    public class BlogConfig
    {
        public string DisharmonyWords { get; set; }

        public string BloggerAvatarBase64 { get; set; }

        public string BloggerName { get; set; }

        public bool EnableComments { get; set; }

        public GeneralSettings GeneralSettings { get; set; }

        public EmailConfiguration EmailConfiguration { get; set; }

        public FeedSettings FeedSettings { get; set; }

        public WatermarkSettings WatermarkSettings { get; set; }

        private bool _hasInitialized;

        public BlogConfig()
        {
            // Set default values in case of blow up
            DisharmonyWords = string.Empty;
            EnableComments = true;
            GeneralSettings = new GeneralSettings();
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
                BloggerAvatarBase64 = cfgDic[nameof(BloggerAvatarBase64)];
                BloggerName = cfgDic[nameof(BloggerName)];
                EnableComments = bool.Parse(cfgDic[nameof(EnableComments)]);

                GeneralSettings = JsonConvert.DeserializeObject<GeneralSettings>(cfgDic[nameof(GeneralSettings)]);

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