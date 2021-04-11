namespace Moonglade.Configuration
{
    public class NotificationSettings : IBlogSettings
    {
        public bool EnableEmailSending { get; set; }
        public bool SendEmailOnCommentReply { get; set; }
        public bool SendEmailOnNewComment { get; set; }
        public string EmailDisplayName { get; set; }
        public string AzureFunctionEndpoint { get; set; }
    }
}