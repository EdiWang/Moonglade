using System.ComponentModel.DataAnnotations;

namespace Moonglade.Auth;

public class UpdateLocalAccountPasswordRequest
{
    public const string PasswordPattern = @"^(?=.*[a-zA-Z])(?=.*[0-9])[\x21-\x7E]{8,}$";
    public const string PasswordRequirements = "Use at least 8 characters, including an English letter and a number. Symbols are optional. Spaces and non-ASCII characters are not allowed.";

    [Required]
    [RegularExpression(PasswordPattern, ErrorMessage = PasswordRequirements)]
    public string OldPassword { get; set; }

    [Required]
    [RegularExpression(PasswordPattern, ErrorMessage = PasswordRequirements)]
    public string NewPassword { get; set; }
}
