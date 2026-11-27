using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moonglade.ActivityLog;
using Moonglade.Auth;
using Moonglade.Web.Tests.TestSupport;
using System.Net;

namespace Moonglade.Web.Tests.Hosting;

public class CollaboratorManagementHttpTests
{
    private const string Path = "/admin/account/collaborators";
    private const string TemporaryPassword = "Temporary432!";
    private const string PermanentPassword = "Permanent876!";
    private const string FirstEmail = "first@example.test";

    [Fact]
    public async Task ManagementRequiresOwnerAndAntiforgeryWhileCollaboratorCanOpenPersonalPasswordForm()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fixture = await LocalAccountHttpFixture.CreateAsync(pathBase: "/blog", useProductionPageConventions: true);
        using var scope = fixture.App.Services.CreateScope();
        var accounts = scope.ServiceProvider.GetRequiredService<LocalAccountStore>();
        var (owner, _) = await InitializeOwnerAsync(scope.ServiceProvider, false, ct);
        var temporary = await accounts.CreateCollaboratorAsync(FirstEmail, "First editor", TemporaryPassword, ct);
        var editor = (await accounts.CompleteTemporaryPasswordChangeAsync(temporary.Id, temporary.Revision, PermanentPassword, null, ct))!;

        using var anonymous = await fixture.SendAsync(HttpMethod.Get, Path);
        Assert.Contains("/blog/auth/signin", anonymous.Headers.Location!.OriginalString);

        fixture.SetTicket(LocalAccountPrincipal.Create(editor, BlogAuthSchemas.Local), BlogAuthSchemas.Local);
        using var denied = await fixture.SendAsync(HttpMethod.Get, Path + "?operation=Create");
        Assert.Contains("/blog/auth/accessdenied", denied.Headers.Location!.OriginalString);
        var editorToken = await fixture.FormTokenAsync("/admin/account/password");
        using var deniedPost = await PostAsync(fixture, editorToken, "Delete", currentPassword: LocalAccountPageFixture.Password,
            account: editor, confirmDelete: true);
        Assert.Contains("/blog/auth/accessdenied", deniedPost.Headers.Location!.OriginalString);

        fixture.SetTicket(LocalAccountPrincipal.Create(owner, BlogAuthSchemas.Local), BlogAuthSchemas.Local);
        using var ownerPasswordPage = await fixture.SendAsync(HttpMethod.Get, "/admin/account/password");
        Assert.Contains("/blog/auth/accessdenied", ownerPasswordPage.Headers.Location!.OriginalString);
        using var noToken = await PostAsync(fixture, null, "Delete", currentPassword: LocalAccountPageFixture.Password,
            account: editor, confirmDelete: true);
        Assert.Equal(HttpStatusCode.BadRequest, noToken.StatusCode);
        Assert.Equal(editor, Assert.Single(await accounts.ListCollaboratorsAsync(ct)));
        Assert.Empty(fixture.Store.Commands);
    }

    [Fact]
    public async Task LifecycleUsesFreshOwnerProofAndRetainsAuditIdentityAfterSlotReuse()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fixture = await LocalAccountHttpFixture.CreateAsync(useProductionPageConventions: true);
        fixture.Store.PersistActivityLogs = true;
        using var scope = fixture.App.Services.CreateScope();
        var accounts = scope.ServiceProvider.GetRequiredService<LocalAccountStore>();
        var (owner, _) = await InitializeOwnerAsync(scope.ServiceProvider, false, ct);
        var ownerPrincipal = LocalAccountPrincipal.Create(owner, BlogAuthSchemas.Local);
        fixture.SetTicket(ownerPrincipal, BlogAuthSchemas.Local);
        var token = await fixture.FormTokenAsync(Path + "?operation=Create");

        using var wrongProof = await PostAsync(fixture, token, "Create", "WrongOwner123!", FirstEmail);
        var failure = await wrongProof.Content.ReadAsStringAsync(ct);
        Assert.Contains("Verify your local password", failure);
        Assert.DoesNotContain("WrongOwner123!", failure);
        Assert.DoesNotContain(TemporaryPassword, failure);
        Assert.Empty(await accounts.ListCollaboratorsAsync(ct));

        using var firstCreated = await PostAsync(fixture, token, "Create", LocalAccountPageFixture.Password, FirstEmail);
        Assert.Equal(Path, firstCreated.Headers.Location!.OriginalString);
        var temporary = Assert.Single(await accounts.ListCollaboratorsAsync(ct));
        var first = (await accounts.CompleteTemporaryPasswordChangeAsync(temporary.Id, temporary.Revision, PermanentPassword, null, ct))!;
        using var secondCreated = await PostAsync(fixture, token, "Create", LocalAccountPageFixture.Password, "second@example.test");
        Assert.Equal(HttpStatusCode.Redirect, secondCreated.StatusCode);
        using var full = await PostAsync(fixture, token, "Create", LocalAccountPageFixture.Password, "third@example.test");
        Assert.Contains("Both collaborator slots are in use.", await full.Content.ReadAsStringAsync(ct));

        using var disable = await PostAsync(fixture, token, "Disable", LocalAccountPageFixture.Password, account: first);
        Assert.Equal(HttpStatusCode.Redirect, disable.StatusCode);
        var editorPrincipal = LocalAccountPrincipal.Create(first, BlogAuthSchemas.Local);
        fixture.SetTicket(editorPrincipal, BlogAuthSchemas.Local);
        using var revoked = await fixture.SendAsync(HttpMethod.Get, "/admin/content-probe");
        Assert.Contains("/auth/signin", revoked.Headers.Location!.OriginalString);

        fixture.SetTicket(ownerPrincipal, BlogAuthSchemas.Local);
        var disabled = (await accounts.ListCollaboratorsAsync(ct)).Single(account => account.Id == first.Id);
        Assert.True(disabled.IsDisabled);
        using var stillFull = await PostAsync(fixture, token, "Create", LocalAccountPageFixture.Password, "third@example.test");
        Assert.Contains("Both collaborator slots are in use.", await stillFull.Content.ReadAsStringAsync(ct));
        using var stale = await PostAsync(fixture, token, "Enable", LocalAccountPageFixture.Password, account: first);
        Assert.Contains("The collaborator account changed.", await stale.Content.ReadAsStringAsync(ct));
        using var enable = await PostAsync(fixture, token, "Enable", LocalAccountPageFixture.Password, account: disabled);
        Assert.Equal(HttpStatusCode.Redirect, enable.StatusCode);
        var active = await accounts.AuthenticateAsync(FirstEmail, PermanentPassword, ct);
        Assert.Equal(first.Id, active!.Id);

        using var unconfirmed = await PostAsync(fixture, token, "Delete", LocalAccountPageFixture.Password, account: active);
        Assert.Contains("Confirm deletion", await unconfirmed.Content.ReadAsStringAsync(ct));
        using var delete = await PostAsync(fixture, token, "Delete", LocalAccountPageFixture.Password, account: active, confirmDelete: true);
        Assert.Equal(HttpStatusCode.Redirect, delete.StatusCode);
        using var replacement = await PostAsync(fixture, token, "Create", LocalAccountPageFixture.Password, FirstEmail);
        Assert.Equal(HttpStatusCode.Redirect, replacement.StatusCode);
        var current = (await accounts.ListCollaboratorsAsync(ct)).Single(account => account.Email == FirstEmail);
        Assert.NotEqual(first.Id, current.Id);
        Assert.True(current.MustChangePassword);

        var history = await fixture.Store.Db.ActivityLog.AsNoTracking().ToListAsync(ct);
        Assert.Equal(6, history.Count);
        Assert.All(history, entry => Assert.Equal(owner.Id, entry.ActorAccountId));
        Assert.Contains(history, entry => entry.EventKey == ActivityEvents.CollaboratorCreated && entry.TargetId == first.Id.ToString());
        Assert.Contains(history, entry => entry.EventKey == ActivityEvents.CollaboratorDeleted && entry.TargetId == first.Id.ToString());
        Assert.Contains(history, entry => entry.EventKey == ActivityEvents.CollaboratorCreated && entry.TargetId == current.Id.ToString());
        Assert.All(fixture.Store.Commands.OfType<CreateActivityLogCommand>(), entry =>
        {
            Assert.Equal("Editor", entry.TargetDisplayName);
            Assert.Null(entry.Details);
        });
    }

    [Fact]
    public async Task OwnerAuthenticatorProofProtectsEmailPasswordAndAuthenticatorChanges()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fixture = await LocalAccountHttpFixture.CreateAsync(required: true, useProductionPageConventions: true);
        using var scope = fixture.App.Services.CreateScope();
        var accounts = scope.ServiceProvider.GetRequiredService<LocalAccountStore>();
        var (owner, secret) = await InitializeOwnerAsync(scope.ServiceProvider, true, ct);
        fixture.SetTicket(LocalAccountPrincipal.Create(owner, BlogAuthSchemas.Local), BlogAuthSchemas.Local);
        var token = await fixture.FormTokenAsync(Path + "?operation=Create");
        var temporary = await accounts.CreateCollaboratorAsync(FirstEmail, "Editor", TemporaryPassword, ct);
        var active = (await accounts.CompleteTemporaryPasswordChangeAsync(temporary.Id, temporary.Revision, PermanentPassword, null, ct))!;
        var editorSecret = new LocalAccountTotpService().GenerateSecret();
        var enrolled = (await accounts.EnrollTotpAsync(active, editorSecret, LocalAccountPageFixture.Code(editorSecret), ct))!;

        using var withoutCode = await PostAsync(fixture, token, "ChangeEmail", LocalAccountPageFixture.Password,
            "changed@example.test", enrolled);
        Assert.Contains("Verify your local password", await withoutCode.Content.ReadAsStringAsync(ct));
        Assert.Equal(enrolled, Assert.Single(await accounts.ListCollaboratorsAsync(ct)));

        using var changed = await PostAsync(fixture, token, "ChangeEmail", LocalAccountPageFixture.Password,
            "changed@example.test", enrolled, LocalAccountPageFixture.Code(secret!));
        Assert.Equal(HttpStatusCode.Redirect, changed.StatusCode);
        var changedEmail = Assert.Single(await accounts.ListCollaboratorsAsync(ct));
        Assert.Equal("changed@example.test", changedEmail.Email);
        using var resetPassword = await PostAsync(fixture, token, "ResetPassword", LocalAccountPageFixture.Password,
            account: changedEmail, code: LocalAccountPageFixture.Code(secret!));
        Assert.Equal(HttpStatusCode.Redirect, resetPassword.StatusCode);
        var reset = Assert.Single(await accounts.ListCollaboratorsAsync(ct));
        Assert.True(reset.MustChangePassword);
        Assert.True(reset.IsTotpEnabled);
        using var resetAuthenticator = await PostAsync(fixture, token, "ResetAuthenticator", LocalAccountPageFixture.Password,
            account: reset, code: LocalAccountPageFixture.Code(secret!));
        Assert.Equal(HttpStatusCode.Redirect, resetAuthenticator.StatusCode);
        Assert.False(Assert.Single(await accounts.ListCollaboratorsAsync(ct)).IsTotpEnabled);
        Assert.Equal(new[] { ActivityEvents.CollaboratorEmailChanged, ActivityEvents.CollaboratorPasswordReset, ActivityEvents.CollaboratorAuthenticatorReset },
            fixture.Store.Commands.OfType<CreateActivityLogCommand>().Select(command => command.EventKey));
    }

    private static async Task<(OwnerAccountSnapshot Owner, string? Secret)> InitializeOwnerAsync(
        IServiceProvider services, bool required, CancellationToken ct)
    {
        var store = services.GetRequiredService<OwnerAccountStore>();
        var pending = await store.PrepareLocalSetupAsync(LocalAccountPageFixture.Email, LocalAccountPageFixture.Password, ct);
        var owner = await store.CompleteLocalSetupAsync(pending, required ? LocalAccountPageFixture.Code(pending.TotpSecret!) : null, ct);
        return (owner, pending.TotpSecret);
    }

    private static Task<HttpResponseMessage> PostAsync(LocalAccountHttpFixture fixture, string? token, string operation,
        string currentPassword, string email = "", LocalAccountSnapshot? account = null, string code = "", bool confirmDelete = false)
    {
        var fields = new Dictionary<string, string>
        {
            ["operation"] = operation,
            ["displayName"] = "Editor",
            ["email"] = email,
            ["temporaryPassword"] = TemporaryPassword,
            ["currentPassword"] = currentPassword,
            ["authenticatorCode"] = code,
            ["confirmDelete"] = confirmDelete.ToString()
        };
        if (account is not null)
        {
            fields["accountId"] = account.Id.ToString();
            fields["revision"] = account.Revision.ToString();
        }
        return fixture.SendAsync(HttpMethod.Post, Path, new FormUrlEncodedContent(fields), token);
    }
}
