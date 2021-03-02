using System.ComponentModel.DataAnnotations;

namespace Moonglade.Web.Models
{
    public class ResetPasswordRequest
    {
        [Required]
        public string NewPassword { get; set; }
    }
}
