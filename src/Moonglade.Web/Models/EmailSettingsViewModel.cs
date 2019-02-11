using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

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
        public string SmtpServer { get; set; }

        [Required]
        [Display(Name = "Smtp UserName")]
        public string SmtpUserName { get; set; }

        [Display(Name = "Banned Mail Domain")]
        public string BannedMailDomain { get; set; }

        [Display(Name = "Smtp Password")]
        [DataType(DataType.Password)]
        public string SmtpPassword { get; set; }

        public EmailSettingsViewModel()
        {
            EnableSsl = true;
            SmtpServerPort = 576;
        }
    }
}
