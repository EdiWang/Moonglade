using Newtonsoft.Json;

namespace Moonglade.Configuration
{
    public class EmailConfiguration : MoongladeSettings
    {
        public bool EnableEmailSending { get; set; }
        public bool EnableSsl { get; set; }
        public bool SendEmailOnCommentReply { get; set; }
        public bool SendEmailOnNewComment { get; set; }
        public int SmtpServerPort { get; set; }
        public string AdminEmail { get; set; }
        public string EmailDisplayName { get; set; }
        public string SmtpPassword { get; set; }
        public string SmtpServer { get; set; }
        public string SmtpUserName { get; set; }
        public string BannedMailDomain { get; set; }

        [JsonIgnore]
        public string SmtpClearPassword { get; set; }
    }
}