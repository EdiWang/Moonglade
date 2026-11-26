using Microsoft.Extensions.DependencyInjection;
using Moonglade.Auth;
using Moonglade.Web.Tests.TestSupport;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace Moonglade.Web.Tests.Hosting;

public class CollaboratorSignInHttpTests
{
    private const string Email = "collaborator@example.test";
    private const string TemporaryPassword = "Temporary432!";
    private const string PermanentPassword = "Permanent876!";
    private const string NewPassword = "NewPassword924!";
    private const string ChangeTemporaryPath = "/auth/change-temporary-password";
    private const string VerifyPath = "/auth/verify-authenticator";
    private const string ChangePasswordApi = "/api/account/collaborator/password";

    [Theory]
    [InlineData(false, "")]
    [InlineData(true, "/blog")]
    public async Task TemporaryPasswordRequiresChangeAndApplicableTotpBeforeIssuingContentSession(bool required, string pathBase)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fixture = await LocalAccountHttpFixture.CreateAsync(required: required, pathBase: pathBase);
        var temporary = await ProvisionAsync(fixture, required);
        using var passwordLogin = await SignInAsync(fixture, Email, TemporaryPassword);
        Assert.Equal(pathBase + ChangeTemporaryPath, passwordLogin.Headers.Location!.OriginalString);
        var ticket = fixture.ReadTicket(BlogAuthSchemas.TemporaryPasswordChange);
        Assert.False(ticket.Principal.IsInRole(BlogAuthSchemas.CollaboratorRole));
        Assert.False(ticket.Principal.IsInRole(BlogAuthSchemas.AdministratorRole));
        Assert.Equal(TimeSpan.FromMinutes(10), ticket.Properties.ExpiresUtc - ticket.Properties.IssuedUtc);
        using var blocked = await fixture.SendAsync(HttpMethod.Get, "/admin/content-probe");
        Assert.Equal(HttpStatusCode.Redirect, blocked.StatusCode);
        using var blockedApi = await fixture.SendAsync(HttpMethod.Put, ChangePasswordApi);
        Assert.Equal(HttpStatusCode.Unauthorized, blockedApi.StatusCode);

        using var missingAntiforgery = await fixture.SendAsync(HttpMethod.Post, ChangeTemporaryPath,
            Form(("NewPassword", PermanentPassword), ("ConfirmPassword", PermanentPassword)));
        Assert.Equal(HttpStatusCode.BadRequest, missingAntiforgery.StatusCode);
        using var reused = await ChangeTemporaryAsync(fixture, TemporaryPassword);
        Assert.Equal(HttpStatusCode.OK, reused.StatusCode);

        using var changed = await ChangeTemporaryAsync(fixture, PermanentPassword);
        Assert.Equal(pathBase + (required ? "/auth/setup-authenticator" : "/admin/post"), changed.Headers.Location!.OriginalString);
        if (required)
        {
            using var beforeTotp = await fixture.SendAsync(HttpMethod.Get, "/admin/content-probe");
            Assert.Equal(HttpStatusCode.Redirect, beforeTotp.StatusCode);
            var token = await fixture.FormTokenAsync("/auth/setup-authenticator");
            using var setupPage = await fixture.SendAsync(HttpMethod.Get, "/auth/setup-authenticator");
            var page = await setupPage.Content.ReadAsStringAsync(ct);
            var secret = Regex.Match(page, "id=\"setup-key\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
            Assert.False(string.IsNullOrWhiteSpace(secret));
            using var enrolled = await fixture.SendAsync(HttpMethod.Post, "/auth/setup-authenticator",
                Form(("AuthenticatorCode", LocalAccountPageFixture.Code(WebUtility.HtmlDecode(secret)))), token);
            Assert.Equal(pathBase + "/admin/post", enrolled.Headers.Location!.OriginalString);
        }

        var application = fixture.ReadTicket(BlogAuthSchemas.Local);
        Assert.True(application.Principal.IsInRole(BlogAuthSchemas.CollaboratorRole));
        Assert.False(application.Principal.IsInRole(BlogAuthSchemas.AdministratorRole));
        Assert.Equal(temporary.Id.ToString(), application.Principal.FindFirstValue(ClaimTypes.NameIdentifier));
        Assert.Equal("Editorial account", application.Principal.FindFirstValue(BlogAuthSchemas.AccountDisplayNameClaim));
        using var allowed = await fixture.SendAsync(HttpMethod.Get, "/admin/content-probe");
        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
        using var denied = await fixture.SendAsync(HttpMethod.Get, "/admin/probe");
        Assert.Contains("/auth/accessdenied", denied.Headers.Location!.OriginalString);

        using var tokenResponse = await fixture.SendAsync(HttpMethod.Get, "/admin/content-token");
        var changeToken = await tokenResponse.Content.ReadAsStringAsync(ct);
        var request = new UpdateLocalAccountPasswordRequest { OldPassword = PermanentPassword, NewPassword = NewPassword };
        using var selfChange = await fixture.SendAsync(HttpMethod.Put, ChangePasswordApi, JsonContent.Create(request), changeToken);
        Assert.Equal(HttpStatusCode.NoContent, selfChange.StatusCode);
        var refreshed = fixture.ReadTicket(BlogAuthSchemas.Local);
        Assert.Equal(temporary.Id.ToString(), refreshed.Principal.FindFirstValue(ClaimTypes.NameIdentifier));
        Assert.NotEqual(application.Principal.FindFirstValue(LocalAccountPrincipal.RevisionClaim),
            refreshed.Principal.FindFirstValue(LocalAccountPrincipal.RevisionClaim));
        using var current = await fixture.SendAsync(HttpMethod.Get, "/admin/content-probe");
        Assert.Equal(HttpStatusCode.OK, current.StatusCode);
        fixture.SetTicket(application.Principal, BlogAuthSchemas.Local);
        using var stale = await fixture.SendAsync(HttpMethod.Get, "/admin/content-probe");
        Assert.Equal(HttpStatusCode.Redirect, stale.StatusCode);
        fixture.SetTicket(ticket.Principal, BlogAuthSchemas.TemporaryPasswordChange);
        using var replay = await fixture.SendAsync(HttpMethod.Get, ChangeTemporaryPath);
        Assert.Equal(HttpStatusCode.Redirect, replay.StatusCode);
        using var oldPassword = await SignInAsync(fixture, Email, TemporaryPassword);
        Assert.Equal(HttpStatusCode.OK, oldPassword.StatusCode);
    }

    [Fact]
    public async Task RememberedDeviceTrustCannotCrossAccountsAndPasswordResetRequiresExistingTotp()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fixture = await LocalAccountHttpFixture.CreateAsync(required: true);
        var first = await ProvisionAsync(fixture, required: true);
        using var scope = fixture.App.Services.CreateScope();
        var accounts = scope.ServiceProvider.GetRequiredService<LocalAccountStore>();
        var second = await accounts.CreateCollaboratorAsync("other@example.test", "Other", TemporaryPassword, ct);
        var (firstEnrolled, firstSecret) = await EnrollAsync(accounts, first, ct);
        var (secondEnrolled, _) = await EnrollAsync(accounts, second, ct);

        using var login = await SignInAsync(fixture, first.Email, PermanentPassword);
        Assert.Equal(VerifyPath, login.Headers.Location!.OriginalString);
        var verifyToken = await fixture.FormTokenAsync(VerifyPath);
        using var verified = await fixture.SendAsync(HttpMethod.Post, VerifyPath,
            Form(("AuthenticatorCode", LocalAccountPageFixture.Code(firstSecret)), ("RememberDevice", "true")), verifyToken);
        Assert.Equal("/admin/post", verified.Headers.Location!.OriginalString);
        using var signOut = await fixture.SendAsync(HttpMethod.Get, "/auth/signout");
        var remembered = fixture.ReadTicket(BlogAuthSchemas.LocalAccountRememberedDevice);
        Assert.True(LocalAccountPrincipal.MatchesAccount(remembered.Principal, firstEnrolled));
        Assert.False(remembered.Principal.IsInRole(BlogAuthSchemas.CollaboratorRole));

        using var otherLogin = await SignInAsync(fixture, secondEnrolled.Email, PermanentPassword);
        Assert.Equal(VerifyPath, otherLogin.Headers.Location!.OriginalString);
        using var firstAgain = await SignInAsync(fixture, first.Email, PermanentPassword);
        Assert.Equal("/admin/post", firstAgain.Headers.Location!.OriginalString);

        var reset = await accounts.ResetCollaboratorPasswordAsync(first.Id, firstEnrolled.Revision, TemporaryPassword, ct);
        using var revoked = await fixture.SendAsync(HttpMethod.Get, "/admin/content-probe");
        Assert.Equal(HttpStatusCode.Redirect, revoked.StatusCode);
        using var resetLogin = await SignInAsync(fixture, first.Email, TemporaryPassword);
        Assert.Equal(ChangeTemporaryPath, resetLogin.Headers.Location!.OriginalString);
        using var withoutCode = await ChangeTemporaryAsync(fixture, NewPassword);
        Assert.Equal(HttpStatusCode.OK, withoutCode.StatusCode);
        Assert.Equal(reset, await accounts.AuthenticateAsync(first.Email, TemporaryPassword, ct));
        using var withCode = await ChangeTemporaryAsync(fixture, NewPassword, LocalAccountPageFixture.Code(firstSecret));
        Assert.Equal("/admin/post", withCode.Headers.Location!.OriginalString);
        using var logout = await fixture.SendAsync(HttpMethod.Get, "/auth/signout");
        using var nextLogin = await SignInAsync(fixture, first.Email, NewPassword);
        Assert.Equal(VerifyPath, nextLogin.Headers.Location!.OriginalString);
    }

    [Theory]
    [InlineData("disable")]
    [InlineData("delete")]
    [InlineData("email")]
    [InlineData("authenticator")]
    public async Task ManagementMutationsInvalidateOldContentSession(string operation)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fixture = await LocalAccountHttpFixture.CreateAsync();
        var temporary = await ProvisionAsync(fixture, required: false);
        using var scope = fixture.App.Services.CreateScope();
        var accounts = scope.ServiceProvider.GetRequiredService<LocalAccountStore>();
        var current = (await accounts.CompleteTemporaryPasswordChangeAsync(temporary.Id, temporary.Revision, PermanentPassword, null, ct))!;
        using var login = await SignInAsync(fixture, Email, PermanentPassword);
        Assert.Equal("/admin/post", login.Headers.Location!.OriginalString);
        var ticket = fixture.ReadTicket(BlogAuthSchemas.Local);
        switch (operation)
        {
            case "disable":
                var disabled = await accounts.SetCollaboratorEnabledAsync(current.Id, current.Revision, false, ct);
                await accounts.SetCollaboratorEnabledAsync(disabled.Id, disabled.Revision, true, ct);
                break;
            case "delete":
                await accounts.DeleteCollaboratorAsync(current.Id, current.Revision, ct);
                await accounts.CreateCollaboratorAsync(Email, "Replacement", TemporaryPassword, ct);
                break;
            case "email":
                await accounts.ChangeCollaboratorEmailAsync(current.Id, current.Revision, "new@example.test", ct);
                break;
            default:
                await accounts.ResetCollaboratorTotpAsync(current.Id, current.Revision, ct);
                break;
        }

        fixture.SetTicket(ticket.Principal, BlogAuthSchemas.Local);
        using var denied = await fixture.SendAsync(HttpMethod.Get, "/admin/content-probe");
        Assert.Equal(HttpStatusCode.Redirect, denied.StatusCode);
        Assert.Contains("/auth/signin", denied.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task ExpiredAndRevokedPasswordChangeTicketsCannotAdvanceSignIn()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fixture = await LocalAccountHttpFixture.CreateAsync();
        var temporary = await ProvisionAsync(fixture, required: false);
        using var login = await SignInAsync(fixture, Email, TemporaryPassword);
        Assert.Equal(ChangeTemporaryPath, login.Headers.Location!.OriginalString);
        var ticket = fixture.ReadTicket(BlogAuthSchemas.TemporaryPasswordChange);
        fixture.SetTicket(ticket.Principal, BlogAuthSchemas.TemporaryPasswordChange, DateTimeOffset.UtcNow.AddSeconds(-1));
        using var expired = await fixture.SendAsync(HttpMethod.Get, ChangeTemporaryPath);
        Assert.Equal(HttpStatusCode.Redirect, expired.StatusCode);
        using var retry = await SignInAsync(fixture, Email, TemporaryPassword);
        Assert.Equal(ChangeTemporaryPath, retry.Headers.Location!.OriginalString);

        using var scope = fixture.App.Services.CreateScope();
        var accounts = scope.ServiceProvider.GetRequiredService<LocalAccountStore>();
        var disabled = await accounts.SetCollaboratorEnabledAsync(temporary.Id, temporary.Revision, false, ct);
        var enabled = await accounts.SetCollaboratorEnabledAsync(disabled.Id, disabled.Revision, true, ct);
        Assert.True(enabled.MustChangePassword);
        using var revoked = await fixture.SendAsync(HttpMethod.Get, ChangeTemporaryPath);
        Assert.Equal(HttpStatusCode.Redirect, revoked.StatusCode);
    }

    [Fact]
    public async Task CookieRoleMustMatchStorageAndOwnerCannotUseCollaboratorSelfService()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fixture = await LocalAccountHttpFixture.CreateAsync();
        var temporary = await ProvisionAsync(fixture, required: false);
        using var owner = await SignInAsync(fixture, LocalAccountPageFixture.Email, LocalAccountPageFixture.Password);
        Assert.Equal("/admin/dashboard", owner.Headers.Location!.OriginalString);
        using var token = await fixture.SendAsync(HttpMethod.Get, "/admin/security-token");
        var proof = await token.Content.ReadAsStringAsync(ct);
        using var denied = await fixture.SendAsync(HttpMethod.Put, ChangePasswordApi, JsonContent.Create(
            new UpdateLocalAccountPasswordRequest { OldPassword = LocalAccountPageFixture.Password, NewPassword = NewPassword }), proof);
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);

        using var scope = fixture.App.Services.CreateScope();
        var accounts = scope.ServiceProvider.GetRequiredService<LocalAccountStore>();
        var current = (await accounts.CompleteTemporaryPasswordChangeAsync(temporary.Id, temporary.Revision, PermanentPassword, null, ct))!;
        var principal = LocalAccountPrincipal.Create(current, BlogAuthSchemas.Local);
        var identity = Assert.IsType<ClaimsIdentity>(principal.Identity);
        identity.RemoveClaim(identity.FindFirst(ClaimTypes.Role)!);
        identity.AddClaim(new Claim(ClaimTypes.Role, BlogAuthSchemas.AdministratorRole));
        fixture.SetTicket(principal, BlogAuthSchemas.Local);
        using var forgedRole = await fixture.SendAsync(HttpMethod.Get, "/admin/probe");
        Assert.Equal(HttpStatusCode.Redirect, forgedRole.StatusCode);
        Assert.Contains("/auth/signin", forgedRole.Headers.Location!.OriginalString);
    }

    private static async Task<LocalAccountSnapshot> ProvisionAsync(LocalAccountHttpFixture fixture, bool required)
    {
        var ct = TestContext.Current.CancellationToken;
        using var scope = fixture.App.Services.CreateScope();
        var owner = scope.ServiceProvider.GetRequiredService<OwnerAccountStore>();
        var pending = await owner.PrepareLocalSetupAsync(LocalAccountPageFixture.Email, LocalAccountPageFixture.Password, ct);
        await owner.CompleteLocalSetupAsync(pending, required ? LocalAccountPageFixture.Code(pending.TotpSecret!) : null, ct);
        return await scope.ServiceProvider.GetRequiredService<LocalAccountStore>()
            .CreateCollaboratorAsync(Email, "Editorial account", TemporaryPassword, ct);
    }

    private static async Task<(LocalAccountSnapshot Account, string Secret)> EnrollAsync(
        LocalAccountStore accounts, LocalAccountSnapshot temporary, CancellationToken ct)
    {
        var account = (await accounts.CompleteTemporaryPasswordChangeAsync(
            temporary.Id, temporary.Revision, PermanentPassword, null, ct))!;
        var secret = new LocalAccountTotpService().GenerateSecret();
        var enrolled = await accounts.EnrollTotpAsync(account, secret, LocalAccountPageFixture.Code(secret), ct);
        return (enrolled!, secret);
    }

    private static async Task<HttpResponseMessage> SignInAsync(LocalAccountHttpFixture fixture, string email, string password)
    {
        var token = await fixture.FormTokenAsync("/auth/signin");
        return await fixture.SendAsync(HttpMethod.Post, "/auth/signin", Form(("Email", email), ("Password", password)), token);
    }

    private static async Task<HttpResponseMessage> ChangeTemporaryAsync(LocalAccountHttpFixture fixture, string password, string code = "")
    {
        var token = await fixture.FormTokenAsync(ChangeTemporaryPath);
        return await fixture.SendAsync(HttpMethod.Post, ChangeTemporaryPath,
            Form(("NewPassword", password), ("ConfirmPassword", password), ("AuthenticatorCode", code)), token);
    }

    private static FormUrlEncodedContent Form(params (string Name, string Value)[] fields) =>
        new(fields.Select(field => new KeyValuePair<string, string>(field.Name, field.Value)));
}
