using System.ComponentModel.DataAnnotations;

namespace Moonglade.Web.Models.Settings
{
    public class EditAccountRequest
    {
        [Required(ErrorMessage = "Please enter a username.")]
        [Display(Name = "Username")]
        [MinLength(2, ErrorMessage = "Username must be at least 2 characters"), MaxLength(32)]
        [RegularExpression("[a-z0-9]+", ErrorMessage = "Username must be lower case letters or numbers.")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Please enter a password.")]
        [Display(Name = "Password")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters"), MaxLength(32)]
        [DataType(DataType.Password)]
        [RegularExpression(@"^(?=.*[a-zA-Z])(?=.*[0-9])[A-Za-z0-9._~!@#$^&*]{8,}$", ErrorMessage = "Password must be minimum eight characters, at least one letter and one number")]
        public string Password { get; set; }
    }
}
