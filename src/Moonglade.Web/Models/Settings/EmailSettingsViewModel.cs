using System.ComponentModel.DataAnnotations;

namespace Moonglade.Web.Models.Settings
{
    public class EmailSettingsViewModel
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
        public string AdminEmail { get; set; }

        [Required]
        [Display(Name = "Display Name")]
        public string EmailDisplayName { get; set; }

        [Display(Name = "Banned Mail Domain")]
        public string BannedMailDomain { get; set; }
    }
}
