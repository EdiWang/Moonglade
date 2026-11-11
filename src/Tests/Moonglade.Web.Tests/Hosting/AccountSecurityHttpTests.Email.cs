using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moonglade.Auth;
using Moonglade.Web.Tests.TestSupport;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;

namespace Moonglade.Web.Tests.Hosting;

public partial class AccountSecurityHttpTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EmailEndpoint_LocalChangeSignsOutAllSchemes_AndPreservesAuthenticator(bool bound)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fixture = await LocalAccountHttpFixture.CreateAsync(required: bound);
        var before = await InitializeAsync(fixture, bound);
        var credentials = await fixture.Store.Db.OwnerAccount.AsNoTracking().SingleAsync(ct);
        string[] schemes = [CookieAuthenticationDefaults.AuthenticationScheme, BlogAuthSchemas.LocalAccountSetup,
            BlogAuthSchemas.LocalAccountTwoFactor, BlogAuthSchemas.InitialAccountSetup];
        foreach (var scheme in schemes) fixture.SetTicket(LocalAccountPrincipal.Create(before, scheme), scheme);
        var token = await AdminTokenAsync(fixture);

        using var changed = await fixture.SendAsync(HttpMethod.Put, "/api/account/email",
            JsonContent.Create(new UpdateOwnerEmailRequest
            { Email = " NewOwner@example.test ", Revision = before.Revision, CurrentPassword = LocalAccountPageFixture.Password }), token);

        Assert.Equal(HttpStatusCode.NoContent, changed.StatusCode);
        Assert.Empty(await changed.Content.ReadAsStringAsync(ct));
        var current = await fixture.Store.Accounts.ReadAsync(ct);
        Assert.Equal(before.Id, current!.Id);
        Assert.Equal("NewOwner@example.test", current.Email);
        Assert.NotEqual(before.Revision, current.Revision);
        var saved = await fixture.Store.Db.OwnerAccount.AsNoTracking().SingleAsync(ct);
        Assert.Equal((credentials.PasswordHash, credentials.PasswordSalt, credentials.TotpSecret, credentials.LocalInitializedUtc),
            (saved.PasswordHash, saved.PasswordSalt, saved.TotpSecret, saved.LocalInitializedUtc));
        Assert.Equal(bound, current.IsTotpEnabled);
        var cleared = changed.Headers.GetValues("Set-Cookie").ToArray();
        foreach (var scheme in schemes)
        {
            var name = fixture.App.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>().Get(scheme).Cookie.Name!;
            Assert.Contains(cleared, cookie => cookie.StartsWith(name + "=;", StringComparison.Ordinal));
        }
        using var admin = await fixture.SendAsync(HttpMethod.Get, "/admin/probe");
        Assert.Equal(HttpStatusCode.Redirect, admin.StatusCode);
        Assert.Contains("/auth/signin", admin.Headers.Location!.OriginalString);
        fixture.SetTicket(LocalAccountPrincipal.Create(before, CookieAuthenticationDefaults.AuthenticationScheme),
            CookieAuthenticationDefaults.AuthenticationScheme);
        using var stale = await fixture.SendAsync(HttpMethod.Get, "/admin/probe");
        Assert.Equal(HttpStatusCode.Redirect, stale.StatusCode);
        Assert.Null(await fixture.Store.Accounts.AuthenticateLocalAsync(before.Email, LocalAccountPageFixture.Password, ct));
        Assert.Equal(current, await fixture.Store.Accounts.AuthenticateLocalAsync(current.Email, LocalAccountPageFixture.Password, ct));
        if (bound) Assert.Equal(current, await fixture.Store.Accounts.VerifyTotpAsync(
            current.Id, current.Revision, LocalAccountPageFixture.Code(saved.TotpSecret!), ct));
    }

    [Fact]
    public async Task EmailEndpoint_WrongPasswordNoOpAndRevisionConflict_RetainSessionAndAccount()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fixture = await LocalAccountHttpFixture.CreateAsync();
        var account = await InitializeAsync(fixture);
        fixture.SetTicket(LocalAccountPrincipal.Create(account, CookieAuthenticationDefaults.AuthenticationScheme),
            CookieAuthenticationDefaults.AuthenticationScheme);
        var token = await AdminTokenAsync(fixture);
        foreach (var scenario in new[] { "wrong-password", "missing-password", "stale", "no-op" })
        {
            using var response = await fixture.SendAsync(HttpMethod.Put, "/api/account/email",
                JsonContent.Create(new UpdateOwnerEmailRequest
                {
                    Email = scenario == "no-op" ? $" {account.Email} " : "new@example.test",
                    Revision = scenario == "stale" ? Guid.NewGuid() : account.Revision,
                    CurrentPassword = scenario == "missing-password" ? null : scenario == "wrong-password" ? "Wrong456!" : LocalAccountPageFixture.Password
                }), token);
            Assert.Equal(scenario == "no-op" ? HttpStatusCode.NoContent : HttpStatusCode.Conflict, response.StatusCode);
            Assert.Equal(account, await fixture.Store.Accounts.ReadAsync(ct));
            using var admin = await fixture.SendAsync(HttpMethod.Get, "/admin/probe");
            Assert.Equal(HttpStatusCode.OK, admin.StatusCode);
        }
    }

    [Fact]
    public async Task EmailEndpoint_RequiresAuthenticationAntiforgeryAndValidAddressBeforeMutation()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fixture = await LocalAccountHttpFixture.CreateAsync();
        var account = await InitializeAsync(fixture);
        using var anonymous = await fixture.SendAsync(HttpMethod.Get, "/api/account/email");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        using var anonymousWrite = await fixture.SendAsync(HttpMethod.Put, "/api/account/email",
            JsonContent.Create(new UpdateOwnerEmailRequest
            { Email = "anonymous@example.test", Revision = account.Revision, CurrentPassword = LocalAccountPageFixture.Password }));
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousWrite.StatusCode);
        fixture.SetTicket(LocalAccountPrincipal.Create(account, CookieAuthenticationDefaults.AuthenticationScheme),
            CookieAuthenticationDefaults.AuthenticationScheme);
        using var noToken = await fixture.SendAsync(HttpMethod.Put, "/api/account/email",
            JsonContent.Create(new UpdateOwnerEmailRequest
            { Email = "new@example.test", Revision = account.Revision, CurrentPassword = LocalAccountPageFixture.Password }));
        Assert.Equal(HttpStatusCode.BadRequest, noToken.StatusCode);
        var token = await AdminTokenAsync(fixture);
        using var invalid = await fixture.SendAsync(HttpMethod.Put, "/api/account/email",
            JsonContent.Create(new UpdateOwnerEmailRequest
            { Email = "invalid", Revision = account.Revision, CurrentPassword = LocalAccountPageFixture.Password }), token);
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal(account, await fixture.Store.Accounts.ReadAsync(ct));
        using var response = await fixture.SendAsync(HttpMethod.Get, "/api/account/email");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        Assert.Equal(["email", "revision"], json.RootElement.EnumerateObject().Select(property => property.Name));
        Assert.Equal(account.Email, json.RootElement.GetProperty("email").GetString());
        Assert.Equal(account.Revision, json.RootElement.GetProperty("revision").GetGuid());
    }

    [Fact]
    public async Task EmailEndpoint_OpenIdConnectCreatesAndUpdatesProfileWithoutPassword_AndRetainsSession()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fixture = await LocalAccountHttpFixture.CreateAsync(provider: AuthenticationProvider.OpenIdConnect);
        fixture.SetTicket(new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("sub", "exact-subject"), new Claim("email", "unrelated-claim@example.test")], BlogAuthSchemas.OpenIdConnect)),
            CookieAuthenticationDefaults.AuthenticationScheme);
        var token = await AdminTokenAsync(fixture);
        using var missing = await fixture.SendAsync(HttpMethod.Get, "/api/account/email");
        using var empty = JsonDocument.Parse(await missing.Content.ReadAsStringAsync(ct));
        Assert.Equal(JsonValueKind.Null, empty.RootElement.GetProperty("email").ValueKind);
        Assert.Equal(JsonValueKind.Null, empty.RootElement.GetProperty("revision").ValueKind);
        using var created = await fixture.SendAsync(HttpMethod.Put, "/api/account/email",
            JsonContent.Create(new UpdateOwnerEmailRequest { Email = "public@example.test" }), token);
        Assert.Equal(HttpStatusCode.NoContent, created.StatusCode);
        var before = await fixture.Store.Accounts.ReadAsync(ct);
        using var changed = await fixture.SendAsync(HttpMethod.Put, "/api/account/email",
            JsonContent.Create(new UpdateOwnerEmailRequest { Email = "changed@example.test", Revision = before!.Revision }), token);
        Assert.Equal(HttpStatusCode.NoContent, changed.StatusCode);
        var current = await fixture.Store.Accounts.ReadAsync(ct);
        Assert.Equal(before.Id, current!.Id);
        Assert.Equal("changed@example.test", current.Email);
        Assert.NotEqual(before.Revision, current.Revision);
        Assert.False(current.IsLocalInitialized);
        var row = await fixture.Store.Db.OwnerAccount.AsNoTracking().SingleAsync(ct);
        Assert.Null(row.PasswordHash);
        Assert.Null(row.PasswordSalt);
        Assert.Null(row.TotpSecret);
        using var admin = await fixture.SendAsync(HttpMethod.Get, "/admin/probe");
        Assert.Equal(HttpStatusCode.OK, admin.StatusCode);
        using var stale = await fixture.SendAsync(HttpMethod.Put, "/api/account/email",
            JsonContent.Create(new UpdateOwnerEmailRequest { Email = "stale@example.test", Revision = before.Revision }), token);
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        Assert.Equal(current, await fixture.Store.Accounts.ReadAsync(ct));
    }

    [Fact]
    public async Task EmailEndpoint_OpenIdConnectMatchingEmailClaimCannotGrantAdministration()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fixture = await LocalAccountHttpFixture.CreateAsync(provider: AuthenticationProvider.OpenIdConnect);
        var profile = await fixture.Store.Accounts.CreateProfileAsync("public@example.test", ct);
        fixture.SetTicket(new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("sub", "not-allowed"), new Claim("email", profile.Email), new Claim(ClaimTypes.Role, "Administrator")],
            BlogAuthSchemas.OpenIdConnect)), CookieAuthenticationDefaults.AuthenticationScheme);

        using var response = await fixture.SendAsync(HttpMethod.Get, "/api/account/email");
        using var write = await fixture.SendAsync(HttpMethod.Put, "/api/account/email",
            JsonContent.Create(new UpdateOwnerEmailRequest { Email = "attempt@example.test", Revision = profile.Revision }));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, write.StatusCode);
        Assert.Equal(profile, await fixture.Store.Accounts.ReadAsync(ct));
    }
}
