#nullable enable

namespace Moonglade.Auth;

public sealed record OwnerProfile(Guid Id, string Email);

public interface IOwnerProfileReader
{
    Task<OwnerProfile?> ReadProfileAsync(CancellationToken ct = default);
}

public sealed record OwnerAccountSnapshot(
    Guid Id, string Email, Guid Revision, bool IsLocalInitialized, bool IsTotpEnabled);
