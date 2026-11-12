using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using System.Security.Claims;

namespace Moonglade.Auth.Tests;

public class LocalAccountCookieEventsTests
{
    [Theory]
    [InlineData(CookieAuthenticationDefaults.AuthenticationScheme)]
    [InlineData(BlogAuthSchemas.LocalAccountSetup)]
    [InlineData(BlogAuthSchemas.LocalAccountTwoFactor)]
    public async Task CurrentAccountCookie_RetainsExactBoundPrincipal(string scheme)
    {
        await using var store = new OwnerAccountTestStore();
        var account = await store.InitializeAsync(TestContext.Current.CancellationToken);
        var principal = LocalAccountPrincipal.Create(account, scheme);
        var auth = new Mock<IAuthenticationService>();
        var context = CreateContext(principal, scheme, auth);

        await Events(store).ValidatePrincipal(context);

        Assert.Same(principal, context.Principal);
        Assert.Equal(account.Email, principal.Identity!.Name);
        Assert.Equal(account.Id.ToString(), principal.FindFirstValue(ClaimTypes.NameIdentifier));
        Assert.Equal(account.Revision.ToString(), principal.FindFirstValue(LocalAccountPrincipal.RevisionClaim));
        Assert.Equal(scheme == CookieAuthenticationDefaults.AuthenticationScheme, principal.IsInRole("Administrator"));
        Assert.Empty(auth.Invocations);
    }

    [Theory]
    [InlineData("legacy")]
    [InlineData("stale")]
    [InlineData("wrong-id")]
    [InlineData("profile")]
    [InlineData("missing")]
    public async Task UnboundOrChangedAccountCookie_IsRejectedAndSignedOut(string scenario)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var store = new OwnerAccountTestStore();
        var account = await store.InitializeAsync(ct);
        var principal = LocalAccountPrincipal.Create(account, CookieAuthenticationDefaults.AuthenticationScheme);
        if (scenario == "legacy")
            principal = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.Name, "admin"), new Claim(ClaimTypes.Role, "Administrator")], BlogAuthSchemas.Local));
        if (scenario == "stale")
            await store.Accounts.ChangePasswordAsync(account.Id, account.Revision, OwnerAccountTestStore.Password, "Replacement456!", ct);
        if (scenario == "wrong-id")
        {
            var identity = (ClaimsIdentity)principal.Identity!;
            identity.RemoveClaim(identity.FindFirst(ClaimTypes.NameIdentifier)!);
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()));
        }
        if (scenario is "profile" or "missing")
        {
            await store.Db.OwnerAccount.ExecuteDeleteAsync(ct);
            if (scenario == "profile") await store.Accounts.CreateProfileAsync(account.Email, ct);
        }
        var auth = new Mock<IAuthenticationService>();
        var context = CreateContext(principal, CookieAuthenticationDefaults.AuthenticationScheme, auth);

        await Events(store).ValidatePrincipal(context);

        Assert.Null(context.Principal);
        auth.Verify(value => value.SignOutAsync(context.HttpContext, CookieAuthenticationDefaults.AuthenticationScheme, null), Times.Once);
    }

    [Fact]
    public async Task RequiredTotp_ApplicationCookieWithoutBoundAuthenticator_IsRejected()
    {
        await using var store = new OwnerAccountTestStore();
        var account = await store.InitializeAsync(TestContext.Current.CancellationToken);
        var auth = new Mock<IAuthenticationService>();
        var context = CreateContext(LocalAccountPrincipal.Create(account, CookieAuthenticationDefaults.AuthenticationScheme),
            CookieAuthenticationDefaults.AuthenticationScheme, auth);
        var events = new LocalAccountCookieEvents(store.Accounts, Options.Create(new AuthenticationSettings()), store.Db);

        await events.ValidatePrincipal(context);

        Assert.Null(context.Principal);
        auth.Verify(value => value.SignOutAsync(context.HttpContext, CookieAuthenticationDefaults.AuthenticationScheme, null), Times.Once);
    }

    private static LocalAccountCookieEvents Events(OwnerAccountTestStore store) =>
        new(store.Accounts, Options.Create(new AuthenticationSettings
        { Totp = new TotpAuthenticationSettings { Required = false } }), store.Db);

    [Theory]
    [InlineData(CookieAuthenticationDefaults.AuthenticationScheme)]
    [InlineData(BlogAuthSchemas.LocalAccountSetup)]
    [InlineData(BlogAuthSchemas.LocalAccountTwoFactor)]
    public async Task ChangedEmail_RejectsEveryOldLocalCookie_WhileNoOpRetainsIt(string scheme)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var store = new OwnerAccountTestStore();
        var before = await store.InitializeAsync(ct);
        var principal = LocalAccountPrincipal.Create(before, scheme);
        var auth = new Mock<IAuthenticationService>();
        await store.Accounts.ChangeLocalEmailAsync(before.Id, before.Revision,
            OwnerAccountTestStore.Password, before.Email, ct);
        var retained = CreateContext(principal, scheme, auth);
        await Events(store).ValidatePrincipal(retained);
        Assert.Same(principal, retained.Principal);
        Assert.Empty(auth.Invocations);

        await store.Accounts.ChangeLocalEmailAsync(before.Id, before.Revision,
            OwnerAccountTestStore.Password, "changed@example.test", ct);
        var stale = CreateContext(principal, scheme, auth);
        await Events(store).ValidatePrincipal(stale);

        Assert.Null(stale.Principal);
        auth.Verify(value => value.SignOutAsync(stale.HttpContext, scheme, null), Times.Once);
    }

    private static CookieValidatePrincipalContext CreateContext(ClaimsPrincipal principal, string scheme, Mock<IAuthenticationService> auth)
    {
        var services = new ServiceCollection().AddSingleton(auth.Object).BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = services };
        return new CookieValidatePrincipalContext(context,
            new AuthenticationScheme(scheme, scheme, typeof(CookieAuthenticationHandler)),
            new CookieAuthenticationOptions(), new AuthenticationTicket(principal, scheme));
    }
}
