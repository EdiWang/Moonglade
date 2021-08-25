using Edi.Captcha;
using System.ComponentModel.DataAnnotations;

namespace Moonglade.Web.Models
{
    public class CommentRequest : ICaptchable
    {
        [Required]
        [MaxLength(64)]
        public string Username { get; set; }

        [Required]
        [DataType(DataType.MultilineText), MaxLength(1024)]
        public string Content { get; set; }

        [MaxLength(128)]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; }

        [Required]
        [StringLength(4)]
        public string CaptchaCode { get; set; }
    }
}