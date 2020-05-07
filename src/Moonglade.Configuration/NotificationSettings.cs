using Moonglade.Configuration.Abstraction;

namespace Moonglade.Configuration
{
    public class NotificationSettings : MoongladeSettings
    {
        public bool EnableEmailSending { get; set; }
        public bool SendEmailOnCommentReply { get; set; }
        public bool SendEmailOnNewComment { get; set; }
        public string AdminEmail { get; set; }
        public string EmailDisplayName { get; set; }
    }
}