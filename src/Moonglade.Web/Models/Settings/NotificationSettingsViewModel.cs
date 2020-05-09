using System.ComponentModel.DataAnnotations;

namespace Moonglade.Web.Models.Settings
{
    public class NotificationSettingsViewModel
    {
        [Display(Name = "Enable Email Sending")]
        public bool EnableEmailSending { get; set; }

        [Display(Name = "Send Email on Comment Reply")]
        public bool SendEmailOnCommentReply { get; set; }

        [Display(Name = "Send Email on NewComment")]
        public bool SendEmailOnNewComment { get; set; }

        [Required]
        [Display(Name = "Admin Email")]
        [DataType(DataType.EmailAddress)]
        [MaxLength(64)]
        public string AdminEmail { get; set; }

        [Required]
        [Display(Name = "Display Name")]
        [MaxLength(64)]
        public string EmailDisplayName { get; set; }
    }
}
