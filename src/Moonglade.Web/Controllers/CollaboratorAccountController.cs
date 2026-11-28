using LiteBus.Commands.Abstractions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Moonglade.ActivityLog;
using Moonglade.Web.Services;

namespace Moonglade.Web.Controllers;

[Authorize(Policy = BlogAuthSchemas.CollaboratorSelfServicePolicy)]
[Route("api/account/collaborator")]
public class CollaboratorAccountController(
    LocalAccountStore accounts,
    ICommandMediator commandMediator,
    IStringLocalizer<Program> localizer,
    ILogger<CollaboratorAccountController> logger) : ActivityControllerBase(commandMediator, logger)
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
            return Conflict(localizer["The account changed. Sign in again."].Value);
        }
        if (saved is null) return Conflict(localizer["Old password is incorrect."].Value);

        await HttpContext.SignInAsync(BlogAuthSchemas.Local, LocalAccountPrincipal.Create(saved, BlogAuthSchemas.Local));
        await LogActivityAsync(ActivityEvents.SettingsPasswordUpdated, saved.DisplayName ?? saved.Email);
        return NoContent();
    }
}
