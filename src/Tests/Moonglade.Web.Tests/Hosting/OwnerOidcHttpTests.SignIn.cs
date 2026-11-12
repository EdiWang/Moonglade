#nullable enable

using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moonglade.Auth;
using Moonglade.Auth.Oidc;
using Moonglade.Web.Tests.TestSupport;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;

namespace Moonglade.Web.Tests.Hosting;

public partial class OwnerOidcHttpTests
{
    [Fact]
    public async Task OidcSignIn_SignsInAsOwnerWithoutTotp_AndSignOutIsLocal()
    {
        using var provider = new SyntheticProvider();
        await using var fixture = await LocalAccountHttpFixture.CreateAsync(required: true, pathBase: "/blog", oidcBackchannel: provider);
        var owner = await ConfigureSignInAsync(fixture);
        Assert.False(owner.IsTotpEnabled);

        using var signedIn = await SignInWithOidcAsync(fixture, provider);
        Assert.Equal(HttpStatusCode.Redirect, signedIn.StatusCode);
        Assert.StartsWith("/blog/admin", signedIn.Headers.Location!.OriginalString);
        var ticket = fixture.ReadTicket(BlogAuthSchemas.Local);
        Assert.True(LocalAccountPrincipal.Matches(ticket.Principal, owner));
        Assert.Equal(owner.Email, ticket.Principal.Identity!.Name);
        Assert.Null(ticket.Principal.FindFirstValue("sub"));
        Assert.Empty(ticket.Properties.GetTokens());
        using var admin = await fixture.SendAsync(HttpMethod.Get, "/admin/probe");
        Assert.Equal(HttpStatusCode.OK, admin.StatusCode);

        using var signOut = await fixture.SendAsync(HttpMethod.Get, "/auth/signout");
        Assert.Equal(HttpStatusCode.Redirect, signOut.StatusCode);
        Assert.StartsWith("/blog", signOut.Headers.Location!.OriginalString);
        using var after = await fixture.SendAsync(HttpMethod.Get, "/admin/probe");
        Assert.Equal(HttpStatusCode.Redirect, after.StatusCode);
        Assert.Equal(1, provider.Exchanges);
    }

    [Theory]
    [InlineData("unbound")]
    [InlineData("disabled")]
    [InlineData("absent")]
    public async Task OidcSignIn_RequiresAnEnabledBindingBeforeChallenge(string scenario)
    {
        using var provider = new SyntheticProvider();
        await using var fixture = await LocalAccountHttpFixture.CreateAsync(oidcBackchannel: provider);
        if (scenario == "absent")
        {
            await SignInLocalAsync(fixture);
        }
        else
        {
            await ConfigureSignInAsync(fixture, bind: scenario != "unbound");
            if (scenario == "disabled")
            {
                var ct = TestContext.Current.CancellationToken;
                await fixture.Store.Db.OwnerOidc.ExecuteUpdateAsync(setters => setters.SetProperty(row => row.Enabled, false), ct);
            }
        }

        using var response = await ChallengeSignInAsync(fixture);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/auth/signin?failed=True", response.Headers.Location!.OriginalString);
        Assert.Equal(0, provider.Exchanges);
    }

    [Theory]
    [InlineData("subject")]
    [InlineData("issuer")]
    [InlineData("changed")]
    [InlineData("disabled")]
    public async Task OidcSignIn_RejectsMismatchedOrStaleExternalIdentity(string scenario)
    {
        var ct = TestContext.Current.CancellationToken;
        using var provider = new SyntheticProvider();
        await using var fixture = await LocalAccountHttpFixture.CreateAsync(oidcBackchannel: provider);
        var owner = await ConfigureSignInAsync(fixture, issuer: scenario == "issuer" ? SyntheticProvider.Authority + "/other" : null);
        if (scenario == "subject") provider.Subject = "External-subject";
        using var challenge = await ChallengeSignInAsync(fixture);
        var callback = provider.CallbackFor(challenge.Headers.Location!);
        if (scenario == "changed")
            await fixture.Store.Db.OwnerOidc.ExecuteUpdateAsync(setters => setters.SetProperty(row => row.Revision, Guid.NewGuid()), ct);
        if (scenario == "disabled")
            await fixture.Store.Db.OwnerOidc.ExecuteUpdateAsync(setters => setters.SetProperty(row => row.Enabled, false), ct);

        using var response = await fixture.SendAsync(HttpMethod.Get, callback);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/auth/signin?failed=true", response.Headers.Location!.OriginalString);
        using var admin = await fixture.SendAsync(HttpMethod.Get, "/admin/probe");
        Assert.Equal(HttpStatusCode.Redirect, admin.StatusCode);
        Assert.Equal(owner, await fixture.Store.Accounts.ReadAsync(ct));
        Assert.Equal(scenario is "subject" or "issuer" ? 1 : 0, provider.Exchanges);
    }

    [Theory]
    [InlineData("unbind")]
    [InlineData("disable")]
    [InlineData("delete")]
    public async Task OidcSession_RevocationRejectsOldTicketAndKeepsLocalAccess(string operation)
    {
        using var provider = new SyntheticProvider();
        await using var fixture = await LocalAccountHttpFixture.CreateAsync(oidcBackchannel: provider);
        var owner = await ConfigureSignInAsync(fixture);
        using var signedIn = await SignInWithOidcAsync(fixture, provider);
        var oldTicket = fixture.ReadTicket(BlogAuthSchemas.Local).Principal;
        var local = LocalAccountPrincipal.Create(owner, BlogAuthSchemas.Local);
        fixture.SetTicket(local, BlogAuthSchemas.Local);
        var ct = TestContext.Current.CancellationToken;
        var row = await fixture.Store.Db.OwnerOidc.AsNoTracking().SingleAsync(ct);

        using var revoked = await ManageAsync(fixture, row.Revision, operation, confirmDelete: operation == "delete");
        Assert.Equal(HttpStatusCode.Redirect, revoked.StatusCode);
        if (operation == "disable")
        {
            fixture.SetTicket(oldTicket, BlogAuthSchemas.Local);
            using var disabledSession = await fixture.SendAsync(HttpMethod.Get, "/admin/probe");
            Assert.Equal(HttpStatusCode.Redirect, disabledSession.StatusCode);
            fixture.SetTicket(local, BlogAuthSchemas.Local);
            var disabled = await fixture.Store.Db.OwnerOidc.AsNoTracking().SingleAsync(ct);
            using var enable = await ManageAsync(fixture, disabled.Revision, "enable");
            Assert.Equal(HttpStatusCode.Redirect, enable.StatusCode);
        }
        using var localStillWorks = await fixture.SendAsync(HttpMethod.Get, "/admin/probe");
        Assert.Equal(HttpStatusCode.OK, localStillWorks.StatusCode);
        fixture.SetTicket(oldTicket, BlogAuthSchemas.Local);
        using var rejected = await fixture.SendAsync(HttpMethod.Get, "/admin/probe");
        Assert.Equal(HttpStatusCode.Redirect, rejected.StatusCode);
    }

    [Fact]
    public async Task OidcSession_PasswordChangeAndSecretRotationPreserveItsOrigin()
    {
        var ct = TestContext.Current.CancellationToken;
        const string newPassword = "NewSynthetic123!";
        using var provider = new SyntheticProvider();
        await using var fixture = await LocalAccountHttpFixture.CreateAsync(oidcBackchannel: provider);
        await ConfigureSignInAsync(fixture);
        using var signedIn = await SignInWithOidcAsync(fixture, provider);
        using var tokenResponse = await fixture.SendAsync(HttpMethod.Get, "/admin/security-token");
        var token = await tokenResponse.Content.ReadAsStringAsync(ct);

        using var changed = await fixture.SendAsync(HttpMethod.Put, "/api/account/password/local",
            JsonContent.Create(new UpdateLocalAccountPasswordRequest
            {
                OldPassword = LocalAccountPageFixture.Password, NewPassword = newPassword
            }), token);
        Assert.Equal(HttpStatusCode.NoContent, changed.StatusCode);
        var refreshed = fixture.ReadTicket(BlogAuthSchemas.Local).Principal;
        var row = await fixture.Store.Db.OwnerOidc.AsNoTracking().SingleAsync(ct);
        using var rotate = await PostAsync(fixture, "Save", row.Revision,
            ("DisplayName", row.DisplayName), ("Authority", row.Authority), ("ClientId", row.ClientId),
            ("ClientSecret", "rotated-disposable-secret"), ("CurrentPassword", newPassword));
        Assert.Equal(HttpStatusCode.Redirect, rotate.StatusCode);
        using var stillSignedIn = await fixture.SendAsync(HttpMethod.Get, "/admin/probe");
        Assert.Equal(HttpStatusCode.OK, stillSignedIn.StatusCode);

        var current = await fixture.Store.Accounts.ReadAsync(ct);
        fixture.SetTicket(LocalAccountPrincipal.Create(current!, BlogAuthSchemas.Local), BlogAuthSchemas.Local);
        var rotated = await fixture.Store.Db.OwnerOidc.AsNoTracking().SingleAsync(ct);
        using var disable = await PostAsync(fixture, "Manage", rotated.Revision,
            ("operation", "disable"), ("CurrentPassword", newPassword));
        Assert.Equal(HttpStatusCode.Redirect, disable.StatusCode);
        fixture.SetTicket(refreshed, BlogAuthSchemas.Local);
        using var rejected = await fixture.SendAsync(HttpMethod.Get, "/admin/probe");
        Assert.Equal(HttpStatusCode.Redirect, rejected.StatusCode);
    }

    private static async Task<OwnerAccountSnapshot> ConfigureSignInAsync(LocalAccountHttpFixture fixture, bool bind = true, string? issuer = null)
    {
        var owner = await SignInLocalAsync(fixture);
        var ct = TestContext.Current.CancellationToken;
        await using var scope = fixture.App.Services.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<OwnerOidcStore>();
        var revision = await store.SaveAsync(owner.Id, null, "Synthetic provider", SyntheticProvider.Authority, "site", Secret, ct);
        if (bind) await store.BindAsync(owner.Id, revision, issuer ?? SyntheticProvider.Authority, "external-subject", "External account", ct);
        return owner;
    }

    private static async Task<HttpResponseMessage> ChallengeSignInAsync(LocalAccountHttpFixture fixture)
    {
        var token = await fixture.FormTokenAsync("/auth/signin");
        using var content = new FormUrlEncodedContent([]);
        return await fixture.SendAsync(HttpMethod.Post, "/auth/signin?handler=Oidc", content, token);
    }

    private static async Task<HttpResponseMessage> SignInWithOidcAsync(LocalAccountHttpFixture fixture, SyntheticProvider provider)
    {
        using var challenge = await ChallengeSignInAsync(fixture);
        Assert.Equal(HttpStatusCode.Redirect, challenge.StatusCode);
        return await fixture.SendAsync(HttpMethod.Get, provider.CallbackFor(challenge.Headers.Location!));
    }
}
