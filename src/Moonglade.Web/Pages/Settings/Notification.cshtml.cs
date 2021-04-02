using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Configuration.Abstraction;
using Moonglade.Web.Models.Settings;

namespace Moonglade.Web.Pages.Settings
{
    public class NotificationModel : PageModel
    {
        private readonly IBlogConfig _blogConfig;
        public NotificationSettingsViewModel ViewModel { get; set; }

        public NotificationModel(IBlogConfig blogConfig)
        {
            _blogConfig = blogConfig;
        }

        public void OnGet()
        {
            var settings = _blogConfig.NotificationSettings;
            ViewModel = new()
            {
                EmailDisplayName = settings.EmailDisplayName,
                EnableEmailSending = settings.EnableEmailSending,
                SendEmailOnCommentReply = settings.SendEmailOnCommentReply,
                SendEmailOnNewComment = settings.SendEmailOnNewComment,
                AzureFunctionEndpoint = settings.AzureFunctionEndpoint
            };
        }
    }
}
