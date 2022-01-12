using System.ComponentModel.DataAnnotations;

namespace Moonglade.Auth;

public class EditAccountRequest
{
    [Required]
    [Display(Name = "Username")]
    [MinLength(2), MaxLength(32)]
    [RegularExpression("[a-z0-9]+")]
    public string Username { get; set; }

    [Required]
    [Display(Name = "Password")]
    [MinLength(8), MaxLength(32)]
    [DataType(DataType.Password)]
    [RegularExpression(@"^(?=.*[a-zA-Z])(?=.*[0-9])[A-Za-z0-9._~!@#$^&*]{8,}$")]
    public string Password { get; set; }
}