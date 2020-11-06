using System.ComponentModel.DataAnnotations;

namespace Moonglade.Web.Models
{
    public class NewCommentModel
    {
        [Required]
        [MaxLength(64)]
        public string Username { get; set; }

        [Required]
        [DataType(DataType.MultilineText), MaxLength(1024)]
        public string Content { get; set; }

        [MaxLength(128)]
        [DataType(DataType.EmailAddress)]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [StringLength(4)]
        public string CaptchaCode { get; set; }
    }
}