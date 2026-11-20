using System.ComponentModel.DataAnnotations;

namespace Moonglade.Auth;

public class ResetLocalAccountTotpRequest
{
    [Required]
    [RegularExpression(UpdateLocalAccountPasswordRequest.PasswordPattern, ErrorMessage = UpdateLocalAccountPasswordRequest.PasswordRequirements)]
    public string CurrentPassword { get; set; }
}
