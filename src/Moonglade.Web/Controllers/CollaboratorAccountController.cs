using LiteBus.Commands.Abstractions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Moonglade.ActivityLog;
using Moonglade.Web.Services;

namespace Moonglade.Web.Controllers;

[Authorize(Roles = BlogAuthSchemas.CollaboratorRole)]
[Route("api/account/collaborator")]
public class CollaboratorAccountController(
    LocalAccountStore accounts,
    ICommandMediator commandMediator,
    ILogger<CollaboratorAccountController> logger) : ContentControllerBase(commandMediator, logger)
{
    [HttpPut("password")]
    [EnableRateLimiting(LocalAccountRateLimitPolicy.PolicyName)]
    public async Task<IActionResult> ChangePassword(UpdateLocalAccountPasswordRequest request, CancellationToken ct)
    {
        var current = await accounts.ReadAsync(User, ct);
        if (current is not { IsOwner: false, MustChangePassword: false }) return Unauthorized();

        LocalAccountSnapshot saved;
        try
        {
            saved = await accounts.ChangeCollaboratorPasswordAsync(current.Id, current.Revision,
                request.OldPassword, request.NewPassword, ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict("The account changed. Sign in again.");
        }
        if (saved is null) return Conflict("Old password is incorrect.");

        await HttpContext.SignInAsync(BlogAuthSchemas.Local, LocalAccountPrincipal.Create(saved, BlogAuthSchemas.Local));
        await LogActivityAsync(ActivityEvents.SettingsPasswordUpdated, saved.DisplayName ?? saved.Email);
        return NoContent();
    }
}
