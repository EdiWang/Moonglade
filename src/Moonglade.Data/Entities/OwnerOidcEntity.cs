#nullable enable

namespace Moonglade.Data.Entities;

public sealed class OwnerOidcEntity
{
    public const int Slot = 1;

    public int Id { get; init; } = Slot;
    public Guid OwnerId { get; init; }
    public Guid Revision { get; set; }
    public bool Enabled { get; set; }
    public required string DisplayName { get; set; }
    public required string Authority { get; set; }
    public required string ClientId { get; set; }
    public required string ProtectedSecret { get; set; }
    public string? Issuer { get; set; }
    public string? Subject { get; set; }
    public string? AccountLabel { get; set; }
    public Guid? SessionRevision { get; set; }
}
