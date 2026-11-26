#nullable enable

namespace Moonglade.Data.Entities;

public class BlogAccountEntity
{
    public const int OwnerSlot = 1;
    public const int LastCollaboratorSlot = 3;

    public Guid Id { get; set; }
    public int Slot { get; set; } = OwnerSlot;
    public string Email { get; set; } = null!;
    public string NormalizedEmail { get; set; } = null!;
    public string? DisplayName { get; set; }
    public bool IsDisabled { get; set; }
    public bool MustChangePassword { get; set; }
    public Guid Revision { get; set; }
    public DateTime? LocalInitializedUtc { get; set; }
    public string? PasswordHash { get; set; }
    public string? PasswordSalt { get; set; }
    public string? TotpSecret { get; set; }
}
