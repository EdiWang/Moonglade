#nullable enable

namespace Moonglade.Data.Entities;

public class OwnerAccountEntity
{
    public Guid Id { get; set; }
    public int Slot { get; set; } = 1;
    public string Email { get; set; } = null!;
    public string NormalizedEmail { get; set; } = null!;
    public Guid Revision { get; set; }
    public DateTime? LocalInitializedUtc { get; set; }
    public string? PasswordHash { get; set; }
    public string? PasswordSalt { get; set; }
    public string? TotpSecret { get; set; }
}
