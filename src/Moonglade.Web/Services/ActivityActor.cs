#nullable enable

using Moonglade.Data.Entities;
using System.Security.Claims;

namespace Moonglade.Web.Services;

internal readonly record struct ActivityActor(ActivityActorKind Kind, Guid? AccountId, string? DisplayName)
{
    public static ActivityActor FromPrincipal(ClaimsPrincipal principal)
    {
        var kind = principal.IsInRole(BlogAuthSchemas.CollaboratorRole) &&
            !principal.IsInRole(BlogAuthSchemas.AdministratorRole)
            ? ActivityActorKind.Collaborator
            : ActivityActorKind.Administrator;
        var accountId = Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id) && id != Guid.Empty
            ? id
            : (Guid?)null;
        var displayName = principal.FindFirstValue(BlogAuthSchemas.AccountDisplayNameClaim);
        return new(kind, accountId, string.IsNullOrWhiteSpace(displayName) ? principal.Identity?.Name : displayName);
    }
}
