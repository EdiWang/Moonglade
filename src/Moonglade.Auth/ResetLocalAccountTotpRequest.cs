using System.ComponentModel.DataAnnotations;

namespace Moonglade.Auth;

public class ResetLocalAccountTotpRequest
{
    [Required]
    [RegularExpression("^(?=.*[a-zA-Z])(?=.*[0-9])[A-Za-z0-9._~!@#$^&*]{8,}$")]
    public string CurrentPassword { get; set; }
}
