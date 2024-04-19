using System.ComponentModel.DataAnnotations;

namespace Moonglade.Auth;

public class UpdateLocalAccountPasswordRequest
{
    [Required]
    [RegularExpression("^[A-Za-z0-9]{3,16}$")]
    public string NewUsername { get; set; }

    [Required]
    [RegularExpression("^(?=.*[a-zA-Z])(?=.*[0-9])[A-Za-z0-9._~!@#$^&*]{8,}$")]
    public string OldPassword { get; set; }

    [Required]
    [RegularExpression("^(?=.*[a-zA-Z])(?=.*[0-9])[A-Za-z0-9._~!@#$^&*]{8,}$")]
    public string NewPassword { get; set; }
}