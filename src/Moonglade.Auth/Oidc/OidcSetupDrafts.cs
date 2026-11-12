#nullable enable

using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;

namespace Moonglade.Auth.Oidc;

public sealed class OidcSetupDrafts(IMemoryCache cache)
{
    private readonly object _gate = new();

    public Guid Save(ClaimsPrincipal owner, OidcProviderInput input)
    {
        var draft = new Draft(Guid.NewGuid(), input);
        lock (_gate) cache.Set(GetKey(owner), draft, OidcBindingFlow.Lifetime);
        return draft.Id;
    }

    public OidcProviderInput? Read(ClaimsPrincipal owner, Guid id) =>
        cache.TryGetValue<Draft>(GetKey(owner), out var draft) && draft?.Id == id ? draft.Input : null;

    public void Clear(ClaimsPrincipal owner, Guid id)
    {
        lock (_gate)
        {
            if (Read(owner, id) is not null) cache.Remove(GetKey(owner));
        }
    }

    private static DraftKey GetKey(ClaimsPrincipal owner) => new(
        owner.FindFirstValue(ClaimTypes.NameIdentifier),
        owner.FindFirstValue(LocalAccountPrincipal.RevisionClaim),
        owner.FindFirstValue(LocalAccountPrincipal.SessionClaim) ?? throw new InvalidOperationException("The owner session is missing."));

    private sealed record DraftKey(string? OwnerId, string? Revision, string Session);
    private sealed record Draft(Guid Id, OidcProviderInput Input);
}
