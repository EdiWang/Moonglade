namespace Moonglade.Configuration
{
    // TODO: Research if this can change to IConfigurationSource
    // in order to use built in ASP.NET Core configurations mechanism
    public class BlogConfig
    {
        public string DisharmonyWords { get; set; }

        public string MetaKeyword { get; set; }

        public string MetaAuthor { get; set; }

        public string SiteTitle { get; set; }

        public string BloggerAvatarBase64 { get; set; }

        public EmailConfiguration EmailConfiguration { get; set; }

        public FeedSettings FeedSettings { get; set; }

        public ReaderView ReaderView { get; set; }

        private bool _hasInitialized;

        public BlogConfig()
        {
            // Set default values in case of blow up
            DisharmonyWords = string.Empty;
            MetaKeyword = string.Empty;
            MetaAuthor = string.Empty;
            SiteTitle = string.Empty;
            EmailConfiguration = new EmailConfiguration();
            FeedSettings = new FeedSettings();
            ReaderView = new ReaderView();
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

                EmailConfiguration.EnableSsl =
                    bool.Parse(cfgDic[$"{nameof(EmailConfiguration)}.{nameof(EmailConfiguration.EnableSsl)}"]);
                EmailConfiguration.EnableEmailSending =
                    bool.Parse(cfgDic[$"{nameof(EmailConfiguration)}.{nameof(EmailConfiguration.EnableEmailSending)}"]);
                EmailConfiguration.SendEmailOnCommentReply =
                    bool.Parse(cfgDic[$"{nameof(EmailConfiguration)}.{nameof(EmailConfiguration.SendEmailOnCommentReply)}"]);
                EmailConfiguration.SendEmailOnNewComment =
                    bool.Parse(cfgDic[$"{nameof(EmailConfiguration)}.{nameof(EmailConfiguration.SendEmailOnNewComment)}"]);
                EmailConfiguration.AdminEmail = cfgDic[$"{nameof(EmailConfiguration)}.{nameof(EmailConfiguration.AdminEmail)}"];
                EmailConfiguration.SmtpServer = cfgDic[$"{nameof(EmailConfiguration)}.{nameof(EmailConfiguration.SmtpServer)}"];
                EmailConfiguration.SmtpUserName = cfgDic[$"{nameof(EmailConfiguration)}.{nameof(EmailConfiguration.SmtpUserName)}"];
                EmailConfiguration.SmtpPassword =
                    blogConfigurationService.DecryptPassword(
                        cfgDic[$"{nameof(EmailConfiguration)}.{nameof(EmailConfiguration.SmtpPassword)}"]);
                EmailConfiguration.SmtpServerPort = int.Parse(cfgDic[$"{nameof(EmailConfiguration)}.{nameof(EmailConfiguration.SmtpServerPort)}"]);
                EmailConfiguration.EmailDisplayName = cfgDic[$"{nameof(EmailConfiguration)}.{nameof(EmailConfiguration.EmailDisplayName)}"];
                EmailConfiguration.BannedMailDomain = cfgDic[$"{nameof(EmailConfiguration)}.{nameof(EmailConfiguration.BannedMailDomain)}"];

                FeedSettings.RssTitle = cfgDic[$"{nameof(FeedSettings)}.{nameof(FeedSettings.RssTitle)}"];
                FeedSettings.RssDescription = cfgDic[$"{nameof(FeedSettings)}.{nameof(FeedSettings.RssDescription)}"];
                FeedSettings.RssCopyright = cfgDic[$"{nameof(FeedSettings)}.{nameof(FeedSettings.RssCopyright)}"];
                FeedSettings.RssGeneratorName = cfgDic[$"{nameof(FeedSettings)}.{nameof(FeedSettings.RssGeneratorName)}"];
                FeedSettings.AuthorName = cfgDic[$"{nameof(FeedSettings)}.{nameof(FeedSettings.AuthorName)}"];
                FeedSettings.RssItemCount = int.Parse(cfgDic[$"{nameof(FeedSettings)}.{nameof(FeedSettings.RssItemCount)}"]);

                ReaderView.SiteName = cfgDic[$"{nameof(ReaderView)}.{nameof(ReaderView.SiteName)}"];

                _hasInitialized = true;
            }
        }

        public void DumpOldValuesWhenNextLoad()
        {
            _hasInitialized = false;
        }
    }
}