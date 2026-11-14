#nullable enable

namespace Moonglade.Auth;

public sealed class PendingOwnerSetup
{
    public required string Email { get; init; }
    public required string PasswordHash { get; init; }
    public required string PasswordSalt { get; init; }
    public string? TotpSecret { get; init; }
    public Guid? ExpectedRevision { get; init; }
    public bool RequiresTotp { get; init; }
    public DateTime ExpiresUtc { get; init; }
}
