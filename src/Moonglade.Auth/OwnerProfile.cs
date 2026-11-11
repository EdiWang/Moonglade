#nullable enable

namespace Moonglade.Auth;

public sealed record OwnerProfile(Guid Id, string Email)
{
    public bool UseGravatar { get; init; }
}

public interface IOwnerProfileReader
{
    Task<OwnerPresentation> ReadPresentationAsync(CancellationToken ct = default);
    Task<OwnerProfile?> ReadProfileAsync(CancellationToken ct = default);
}

public sealed record OwnerAccountSnapshot(
    Guid Id, string Email, Guid Revision, bool IsLocalInitialized, bool IsTotpEnabled);
