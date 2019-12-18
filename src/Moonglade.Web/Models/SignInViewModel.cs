using System.ComponentModel.DataAnnotations;

namespace Moonglade.Web.Models
{
    public class SignInViewModel
    {
        [Required]
        [Display(Name = "Username")]
        [MaxLength(32)]
        public string Username { get; set; }

        [Required]
        [Display(Name = "Password")]
        [DataType(DataType.Password)]
        [MaxLength(32)]
        public string Password { get; set; }
    }
}
