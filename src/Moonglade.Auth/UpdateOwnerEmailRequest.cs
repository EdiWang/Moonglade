using System.ComponentModel.DataAnnotations;

namespace Moonglade.Auth;

public sealed class UpdateOwnerEmailRequest
{
    [Required, EmailAddress, StringLength(254)]
    public string Email { get; set; }

    public Guid? Revision { get; set; }

    [DataType(DataType.Password)]
    public string CurrentPassword { get; set; }
}
