namespace Moonglade.Model
{
    public class Constants
    {
        public static string DbConnectionName = "MoongladeDatabase";

        public static string DataDirectory = "DataDirectory";

        public static string AppBaseDirectory = "AppBaseDirectory";

        public static int SmallImagePixelsThreshold = 200 * 200;

        public static string OpenSearchFileName = "opensearch.xml";

        public static string OpmlFileName = "opml.xml";

        public static string BlogOwnerSettingsDefaultValue = @"{""Name"":""Admin"",""Description"":""Moonglade Admin"",""ShortDescription"":""Moonglade Admin"",""AvatarBase64"":""""}";

        public static string GeneralSettingsDefaultValue = @"{""SiteTitle"":""Moonglade"",""LogoText"":""moonglade"",""MetaKeyword"":""moonglade"",""Copyright"":""&copy; 2019""}";

        public static string ContentSettingsDefaultValue = @"{""EnableComments"":true,""DisharmonyWords"":""fuck|shit""}";

        public static string EmailConfigurationDefaultValue =
            @"{""EnableEmailSending"":true,""EnableSsl"":true,""SendEmailOnCommentReply"":true,""SendEmailOnNewComment"":true,""SmtpServerPort"":587,""AdminEmail"":"""",""EmailDisplayName"":""Moonglade"",""SmtpPassword"":"""",""SmtpServer"":"""",""SmtpUserName"":"""",""BannedMailDomain"":""""}";

        public static string WatermarkSettingsDefaultValue =
            @"{""IsEnabled"":true,""KeepOriginImage"":false,""FontSize"":20,""WatermarkText"":""Moonglade""}";

        public static string FeedSettingsDefaultValue =
            @"{""RssItemCount"":20,""RssCopyright"":""(c) {year} Moonglade"",""RssDescription"":""Latest posts from Moonglade"",""RssGeneratorName"":""Moonglade"",""RssTitle"":""Moonglade"",""AuthorName"":""Admin""}";

        public static string PostContentInitValue = "Moonglade is the successor of project Nordrassil, which was the .NET Framework version of the blog system. Moonglade is a complete rewrite of the old system using .NET Core, optimized for cloud-based hosting.";
    }
}
