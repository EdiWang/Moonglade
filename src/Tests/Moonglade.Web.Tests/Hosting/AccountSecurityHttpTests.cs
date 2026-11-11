using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;
using Moonglade.Auth;
using Moonglade.Web.Tests.TestSupport;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;

namespace Moonglade.Web.Tests.Hosting;

public partial class AccountSecurityHttpTests
{
    [Theory]
    [InlineData("Development")]
    [InlineData("Production")]
    [InlineData("Staging")]
    public async Task AuthenticatedSignInGet_ClearsIdentityAndIssuesUsableAnonymousAntiforgery(string environment)
    {
        await using var fixture = await LocalAccountHttpFixture.CreateAsync(environment: environment);
        var account = await InitializeAsync(fixture);
        fixture.SetTicket(LocalAccountPrincipal.Create(account, CookieAuthenticationDefaults.AuthenticationScheme),
            CookieAuthenticationDefaults.AuthenticationScheme);
        var token = await fixture.FormTokenAsync("/auth/signin");

        using var response = await fixture.SendAsync(HttpMethod.Post, "/auth/signin",
            new FormUrlEncodedContent(new Dictionary<string, string>
            { ["Email"] = account.Email, ["Password"] = LocalAccountPageFixture.Password }), token);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/admin/dashboard", response.Headers.Location!.OriginalString);
        var ticket = fixture.ReadTicket(CookieAuthenticationDefaults.AuthenticationScheme);
        Assert.True(LocalAccountPrincipal.Matches(ticket.Principal, account));
        Assert.True(ticket.Principal.IsInRole("Administrator"));
        using var admin = await fixture.SendAsync(HttpMethod.Get, "/admin/probe");
        Assert.Equal(HttpStatusCode.OK, admin.StatusCode);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RequiredTotpPasswordLogin_IssuesOnlyAccountBoundIntermediateCookie(bool bound)
    {
        await using var fixture = await LocalAccountHttpFixture.CreateAsync(required: true);
        var account = await InitializeAsync(fixture, bound);
        var token = await fixture.FormTokenAsync("/auth/signin");
        using var response = await fixture.SendAsync(HttpMethod.Post, "/auth/signin",
            new FormUrlEncodedContent(new Dictionary<string, string>
            { ["Email"] = account.Email, ["Password"] = LocalAccountPageFixture.Password }), token);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(bound ? "/auth/verify-authenticator" : "/auth/setup-authenticator", response.Headers.Location!.OriginalString);
        var ticket = fixture.ReadTicket(bound ? BlogAuthSchemas.LocalAccountTwoFactor : BlogAuthSchemas.LocalAccountSetup);
        Assert.True(LocalAccountPrincipal.Matches(ticket.Principal, account));
        Assert.False(ticket.Principal.IsInRole("Administrator"));
        Assert.Equal(TimeSpan.FromMinutes(10), ticket.Properties.ExpiresUtc - ticket.Properties.IssuedUtc);
        using var admin = await fixture.SendAsync(HttpMethod.Get, "/admin/probe");
        Assert.Equal(HttpStatusCode.Redirect, admin.StatusCode);
        Assert.Contains("/auth/signin", admin.Headers.Location!.OriginalString);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OrdinaryTotpGet_CannotDisablePostRateLimitForEnrollmentOrVerification(bool bound)
    {
        await using var fixture = await LocalAccountHttpFixture.CreateAsync(required: true, permits: 1);
        var account = await InitializeAsync(fixture, bound);
        var loginToken = await fixture.FormTokenAsync("/auth/signin");
        using var login = await fixture.SendAsync(HttpMethod.Post, "/auth/signin",
            new FormUrlEncodedContent(new Dictionary<string, string>
            { ["Email"] = account.Email, ["Password"] = LocalAccountPageFixture.Password }), loginToken);
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        var route = bound ? "/auth/verify-authenticator" : "/auth/setup-authenticator";
        var token = await fixture.FormTokenAsync(route);
        using var invalid = await fixture.SendAsync(HttpMethod.Post, route,
            new FormUrlEncodedContent(new Dictionary<string, string> { ["AuthenticatorCode"] = "invalid-code" }), token);
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);

        using var throttled = await fixture.SendAsync(HttpMethod.Post, route,
            new FormUrlEncodedContent(new Dictionary<string, string> { ["AuthenticatorCode"] = "123456" }), token);

        Assert.Equal(HttpStatusCode.TooManyRequests, throttled.StatusCode);
        Assert.Equal(account, await fixture.Store.Accounts.ReadAsync(TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData("legacy")]
    [InlineData("expired")]
    [InlineData("stale")]
    public async Task LocalApplicationCookie_LegacyExpiredOrChangedRevision_IsRejected(string scenario)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fixture = await LocalAccountHttpFixture.CreateAsync();
        var account = await InitializeAsync(fixture);
        var principal = scenario == "legacy"
            ? new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.Name, "admin"), new Claim(ClaimTypes.Role, "Administrator")], BlogAuthSchemas.Local))
            : LocalAccountPrincipal.Create(account, CookieAuthenticationDefaults.AuthenticationScheme);
        if (scenario == "stale")
            await fixture.Store.Accounts.ChangePasswordAsync(account.Id, account.Revision, LocalAccountPageFixture.Password, "Replacement456!", ct);
        fixture.SetTicket(principal, CookieAuthenticationDefaults.AuthenticationScheme,
            scenario == "expired" ? DateTimeOffset.UtcNow.AddMinutes(-1) : null);

        using var rejected = await fixture.SendAsync(HttpMethod.Get, "/admin/probe");

        Assert.Equal(HttpStatusCode.Redirect, rejected.StatusCode);
        Assert.Contains("/auth/signin", rejected.Headers.Location!.OriginalString);
        Assert.Equal(0, fixture.BusinessExecutions);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TemporaryTotpCookie_ExpiredOrStale_CannotReachVerification(bool stale)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fixture = await LocalAccountHttpFixture.CreateAsync(required: true);
        var account = await InitializeAsync(fixture, bound: true);
        fixture.SetTicket(LocalAccountPrincipal.Create(account, BlogAuthSchemas.LocalAccountTwoFactor),
            BlogAuthSchemas.LocalAccountTwoFactor, stale ? null : DateTimeOffset.UtcNow.AddMinutes(-1));
        if (stale) await fixture.Store.Accounts.ResetTotpAsync(account.Id, account.Revision, LocalAccountPageFixture.Password, ct);

        using var response = await fixture.SendAsync(HttpMethod.Get, "/auth/verify-authenticator");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/auth/signin", response.Headers.Location!.OriginalString);
        var saved = await fixture.Store.Accounts.ReadAsync(ct);
        Assert.True(saved!.IsLocalInitialized);
        Assert.Equal(!stale, saved.IsTotpEnabled);
    }

    [Fact]
    public async Task PasswordEndpoint_RefreshesCurrentCookieAndRejectsOtherStaleSessions()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fixture = await LocalAccountHttpFixture.CreateAsync();
        var before = await InitializeAsync(fixture);
        fixture.SetTicket(LocalAccountPrincipal.Create(before, CookieAuthenticationDefaults.AuthenticationScheme),
            CookieAuthenticationDefaults.AuthenticationScheme);
        var token = await AdminTokenAsync(fixture);
        using var changed = await fixture.SendAsync(HttpMethod.Put, "/api/account/password/local",
            JsonContent.Create(new UpdateLocalAccountPasswordRequest
            { OldPassword = LocalAccountPageFixture.Password, NewPassword = "Replacement456!" }), token);

        Assert.Equal(HttpStatusCode.NoContent, changed.StatusCode);
        Assert.Empty(await changed.Content.ReadAsStringAsync(ct));
        var current = await fixture.Store.Accounts.ReadAsync(ct);
        Assert.NotEqual(before.Revision, current!.Revision);
        Assert.True(LocalAccountPrincipal.Matches(fixture.ReadTicket(CookieAuthenticationDefaults.AuthenticationScheme).Principal, current));
        Assert.Null(await fixture.Store.Accounts.AuthenticateLocalAsync(before.Email, LocalAccountPageFixture.Password, ct));
        Assert.Equal(current, await fixture.Store.Accounts.AuthenticateLocalAsync(before.Email, "Replacement456!", ct));
        using var retained = await fixture.SendAsync(HttpMethod.Get, "/admin/probe");
        Assert.Equal(HttpStatusCode.OK, retained.StatusCode);
        fixture.SetTicket(LocalAccountPrincipal.Create(before, CookieAuthenticationDefaults.AuthenticationScheme),
            CookieAuthenticationDefaults.AuthenticationScheme);
        using var stale = await fixture.SendAsync(HttpMethod.Get, "/admin/probe");
        Assert.Equal(HttpStatusCode.Redirect, stale.StatusCode);
        Assert.Contains("/auth/signin", stale.Headers.Location!.OriginalString);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AccountEndpoints_RequireAntiforgeryBeforePasswordOrResetMutation(bool reset)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fixture = await LocalAccountHttpFixture.CreateAsync(required: true);
        var account = await InitializeAsync(fixture, bound: true);
        fixture.SetTicket(LocalAccountPrincipal.Create(account, CookieAuthenticationDefaults.AuthenticationScheme),
            CookieAuthenticationDefaults.AuthenticationScheme);
        using var response = await fixture.SendAsync(HttpMethod.Put,
            reset ? "/api/account/totp/local/reset" : "/api/account/password/local",
            reset ? JsonContent.Create(new ResetLocalAccountTotpRequest { CurrentPassword = LocalAccountPageFixture.Password })
                : JsonContent.Create(new UpdateLocalAccountPasswordRequest
                { OldPassword = LocalAccountPageFixture.Password, NewPassword = "Replacement456!" }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(account, await fixture.Store.Accounts.ReadAsync(ct));
    }

    [Fact]
    public async Task ResetEndpoint_ClearsAuthenticatorAndSessionsWithoutReopeningSetup()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fixture = await LocalAccountHttpFixture.CreateAsync(required: true);
        var before = await InitializeAsync(fixture, bound: true);
        fixture.SetTicket(LocalAccountPrincipal.Create(before, CookieAuthenticationDefaults.AuthenticationScheme),
            CookieAuthenticationDefaults.AuthenticationScheme);
        var token = await AdminTokenAsync(fixture);
        using var reset = await fixture.SendAsync(HttpMethod.Put, "/api/account/totp/local/reset",
            JsonContent.Create(new ResetLocalAccountTotpRequest { CurrentPassword = LocalAccountPageFixture.Password }), token);

        Assert.Equal(HttpStatusCode.NoContent, reset.StatusCode);
        var account = await fixture.Store.Accounts.ReadAsync(ct);
        Assert.True(account!.IsLocalInitialized);
        Assert.False(account.IsTotpEnabled);
        Assert.Equal(before.Id, account.Id);
        Assert.Equal(account, await fixture.Store.Accounts.AuthenticateLocalAsync(account.Email, LocalAccountPageFixture.Password, ct));
        using var admin = await fixture.SendAsync(HttpMethod.Get, "/admin/probe");
        Assert.Equal(HttpStatusCode.Redirect, admin.StatusCode);
        using var setup = await fixture.SendAsync(HttpMethod.Get, "/auth/setup-account");
        Assert.Equal(HttpStatusCode.Redirect, setup.StatusCode);
        Assert.Equal("/", setup.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task OpenIdConnectAccountCookie_RemainsValidButLocalCredentialEndpointsAreUnavailable()
    {
        await using var fixture = await LocalAccountHttpFixture.CreateAsync(provider: AuthenticationProvider.OpenIdConnect);
        fixture.SetTicket(new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("sub", "exact-subject"), new Claim(ClaimTypes.Name, "OIDC owner")], BlogAuthSchemas.OpenIdConnect)),
            CookieAuthenticationDefaults.AuthenticationScheme);
        using var admin = await fixture.SendAsync(HttpMethod.Get, "/admin/probe");
        Assert.Equal(HttpStatusCode.OK, admin.StatusCode);
        var token = await AdminTokenAsync(fixture);
        using var reset = await fixture.SendAsync(HttpMethod.Put, "/api/account/totp/local/reset",
            JsonContent.Create(new ResetLocalAccountTotpRequest { CurrentPassword = LocalAccountPageFixture.Password }), token);
        Assert.Equal(HttpStatusCode.NotFound, reset.StatusCode);
        Assert.Null(await fixture.Store.Accounts.ReadAsync(TestContext.Current.CancellationToken));
    }

    private static async Task<string> AdminTokenAsync(LocalAccountHttpFixture fixture)
    {
        using var response = await fixture.SendAsync(HttpMethod.Get, "/admin/security-token");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
    }

    private static async Task<OwnerAccountSnapshot> InitializeAsync(LocalAccountHttpFixture fixture, bool bound = false)
    {
        var ct = TestContext.Current.CancellationToken;
        var accounts = new OwnerAccountStore(fixture.Store.Db, new LocalAccountTotpService(),
            Options.Create(new AuthenticationSettings { Totp = new TotpAuthenticationSettings { Required = bound } }), TimeProvider.System,
            Moq.Mock.Of<LiteBus.Events.Abstractions.IEventMediator>());
        var pending = await accounts.PrepareLocalSetupAsync(LocalAccountPageFixture.Email, LocalAccountPageFixture.Password, ct);
        return await accounts.CompleteLocalSetupAsync(pending, bound ? LocalAccountPageFixture.Code(pending.TotpSecret!) : null, ct);
    }
}
