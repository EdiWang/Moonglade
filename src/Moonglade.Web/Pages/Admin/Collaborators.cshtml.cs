#nullable enable

using LiteBus.Commands.Abstractions;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Moonglade.ActivityLog;
using Moonglade.Data.Entities;
using Moonglade.Web.Services;
using System.ComponentModel.DataAnnotations;

namespace Moonglade.Web.Pages.Admin;

[EnableRateLimiting(LocalAccountRateLimitPolicy.PolicyName)]
[ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
public sealed class CollaboratorsModel(
    LocalAccountStore accounts,
    OwnerAccountStore owner,
    IOptions<AuthenticationSettings> authentication,
    IStringLocalizer<Program> localizer,
    ICommandMediator commandMediator,
    ILogger<CollaboratorsModel> logger) : PageModel
{
    public const int Capacity = BlogAccountEntity.LastCollaboratorSlot - BlogAccountEntity.OwnerSlot;

    [TempData] public string? SuccessMessage { get; set; }

    public IReadOnlyList<LocalAccountSnapshot> Accounts { get; private set; } = [];
    public LocalAccountSnapshot? SelectedAccount { get; private set; }
    public Operation SelectedOperation { get; private set; }
    public bool RequireTotp { get; private set; }
    public string? DisplayName { get; private set; }
    public string? Email { get; private set; }

    public async Task<IActionResult> OnGetAsync(Operation operation, Guid? accountId, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest();
        await LoadAsync(operation, accountId, ct);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Operation operation, Guid? accountId, Guid? revision,
        string? displayName, string? email, string? temporaryPassword, string? currentPassword, string? authenticatorCode,
        bool confirmDelete, CancellationToken ct)
    {
        await LoadAsync(operation, accountId, ct);
        DisplayName = displayName;
        Email = email;

        if (!ModelState.IsValid || operation is Operation.None || !Enum.IsDefined(operation))
            return Reject("Check the account details and try again.");
        if (operation != Operation.Create && (SelectedAccount is null || SelectedAccount.Revision != revision))
            return Reject("The collaborator account changed. Reload and try again.");
        if (operation == Operation.Delete && !confirmDelete)
            return Reject("Confirm deletion before deleting the account.");
        if (!await owner.VerifyManagementProofAsync(User, currentPassword, authenticatorCode, ct))
            return Reject("Verify your local password and applicable authenticator code.");

        var id = accountId.GetValueOrDefault();
        var expectedRevision = revision.GetValueOrDefault();
        try
        {
            var (saved, activityEvent) = operation switch
            {
                Operation.Create => (await accounts.CreateCollaboratorAsync(email!, displayName!, temporaryPassword!, ct), ActivityEvents.CollaboratorCreated),
                Operation.ChangeEmail => (await accounts.ChangeCollaboratorEmailAsync(id, expectedRevision, email!, ct), ActivityEvents.CollaboratorEmailChanged),
                Operation.ResetPassword => (await accounts.ResetCollaboratorPasswordAsync(id, expectedRevision, temporaryPassword!, ct), ActivityEvents.CollaboratorPasswordReset),
                Operation.ResetAuthenticator => (await accounts.ResetCollaboratorTotpAsync(id, expectedRevision, ct), ActivityEvents.CollaboratorAuthenticatorReset),
                Operation.Disable => (await accounts.SetCollaboratorEnabledAsync(id, expectedRevision, false, ct), ActivityEvents.CollaboratorDisabled),
                Operation.Enable => (await accounts.SetCollaboratorEnabledAsync(id, expectedRevision, true, ct), ActivityEvents.CollaboratorEnabled),
                Operation.Delete => (await accounts.DeleteCollaboratorAsync(id, expectedRevision, ct), ActivityEvents.CollaboratorDeleted),
                _ => throw new InvalidOperationException("Unsupported collaborator operation.")
            };
            if (operation == Operation.Delete || saved.Revision != expectedRevision)
                await ActivityLogDispatcher.SendForRequestAsync(commandMediator, logger, HttpContext, activityEvent,
                    saved.DisplayName, targetId: saved.Id.ToString());

            SuccessMessage = ActivityEvents.Get(activityEvent).Label;
            return RedirectToPage();
        }
        catch (CollaboratorSlotsFullException)
        {
            return Reject("Both collaborator slots are in use.");
        }
        catch (DbUpdateConcurrencyException)
        {
            await LoadAsync(operation, accountId, ct);
            return Reject("The collaborator account changed. Reload and try again.");
        }
        catch (ValidationException ex)
        {
            return Reject(ex.Message switch
            {
                "This email address is already in use." => ex.Message,
                UpdateLocalAccountPasswordRequest.PasswordRequirements => ex.Message,
                _ => "Check the account details and try again."
            });
        }
    }

    private async Task LoadAsync(Operation operation, Guid? accountId, CancellationToken ct)
    {
        Accounts = await accounts.ListCollaboratorsAsync(ct);
        SelectedAccount = Accounts.SingleOrDefault(account => account.Id == accountId);
        SelectedOperation = operation == Operation.Create || (SelectedAccount is not null && Enum.IsDefined(operation))
            ? operation : Operation.None;
        Email = SelectedAccount?.Email;
        RequireTotp = SelectedOperation != Operation.None &&
            (authentication.Value.Totp.Required || (await owner.ReadAsync(ct))?.IsTotpEnabled == true);
    }

    private PageResult Reject(string key)
    {
        ModelState.Clear();
        ModelState.AddModelError(string.Empty, localizer[key]);
        return Page();
    }

    public enum Operation
    {
        None,
        Create,
        ChangeEmail,
        ResetPassword,
        ResetAuthenticator,
        Disable,
        Enable,
        Delete
    }
}
