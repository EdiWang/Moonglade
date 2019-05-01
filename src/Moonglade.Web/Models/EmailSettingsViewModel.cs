using System.ComponentModel.DataAnnotations;

namespace Moonglade.Web.Models
{
    public class EmailSettingsViewModel
    {
        [Display(Name = "Enable Email Sending")]
        public bool EnableEmailSending { get; set; }

        [Display(Name = "Enable SSL")]
        public bool EnableSsl { get; set; }

        [Display(Name = "Send Email on Comment Reply")]
        public bool SendEmailOnCommentReply { get; set; }

        [Display(Name = "Send Email on NewComment")]
        public bool SendEmailOnNewComment { get; set; }

        [Display(Name = "Smtp Server Port")]
        [Range(1, 65535, ErrorMessage = "Port can only range from 1-65535")]
        public int SmtpServerPort { get; set; }

        [Required]
        [Display(Name = "Admin Email")]
        [DataType(DataType.EmailAddress)]
        public string AdminEmail { get; set; }

        [Required]
        [Display(Name = "Display Name")]
        public string EmailDisplayName { get; set; }

        [Required]
        [Display(Name = "Smtp Server")]
        [RegularExpression(
            @"^(([a-zA-Z0-9]|[a-zA-Z0-9][a-zA-Z0-9\-]*[a-zA-Z0-9])\.)*([A-Za-z0-9]|[A-Za-z0-9][A-Za-z0-9\-]*[A-Za-z0-9])$",
            ErrorMessage = "Invalid IP or Host Name")]
        public string SmtpServer { get; set; }

        [Required]
        [Display(Name = "Smtp UserName")]
        public string SmtpUserName { get; set; }

        [Display(Name = "Banned Mail Domain")]
        public string BannedMailDomain { get; set; }

        [Display(Name = "Smtp Password")]
        [DataType(DataType.Password)]
        public string SmtpClearPassword { get; set; }

        public EmailSettingsViewModel()
        {
            EnableSsl = true;
            SmtpServerPort = 576;
        }
    }
}
