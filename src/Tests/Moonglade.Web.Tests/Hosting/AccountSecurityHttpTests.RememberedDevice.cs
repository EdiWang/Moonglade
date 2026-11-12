using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using Moonglade.Auth;
using Moonglade.Web.Tests.TestSupport;
using System.Net;

namespace Moonglade.Web.Tests.Hosting;

public partial class AccountSecurityHttpTests
{
    private const string RememberedDeviceCookieName = "__Host-Moonglade.LocalAccount.RememberedDevice";

    [Theory]
    [InlineData(false, "")]
    [InlineData(true, "")]
    [InlineData(true, "/blog")]
    public async Task RememberedDevice_OnlyOptInWithValidTotpIssuesProtectedThirtyDayCookie(bool remember, string pathBase)
    {
        await using var fixture = await LocalAccountHttpFixture.CreateAsync(required: true, pathBase: pathBase);
        var account = await InitializeAsync(fixture, bound: true);

        using var verified = await VerifyAndRememberAsync(fixture, account, remember);

        Assert.Equal(HttpStatusCode.Redirect, verified.StatusCode);
        Assert.Equal(pathBase + "/admin/dashboard", verified.Headers.Location!.OriginalString);
        var cookies = SetCookieHeaderValue.ParseList(verified.Headers.GetValues("Set-Cookie").ToList());
        var deviceCookies = cookies.Where(cookie => cookie.Name == RememberedDeviceCookieName).ToArray();
        if (remember)
        {
            var cookie = Assert.Single(deviceCookies);
            Assert.True(cookie.Secure);
            Assert.True(cookie.HttpOnly);
            Assert.Equal(SameSiteMode.Lax, cookie.SameSite);
            Assert.Equal("/", cookie.Path.ToString());
            Assert.False(cookie.Domain.HasValue);
            var ticket = fixture.ReadTicket(BlogAuthSchemas.LocalAccountRememberedDevice);
            Assert.True(LocalAccountPrincipal.Matches(ticket.Principal, account));
            Assert.False(ticket.Principal.IsInRole("Administrator"));
            Assert.True(ticket.Properties.IsPersistent);
            Assert.False(ticket.Properties.AllowRefresh);
            Assert.Equal(TimeSpan.FromDays(30), ticket.Properties.ExpiresUtc - ticket.Properties.IssuedUtc);
            Assert.Equal(ticket.Properties.ExpiresUtc, cookie.Expires);
        }
        else Assert.Empty(deviceCookies);
        using var admin = await fixture.SendAsync(HttpMethod.Get, "/admin/probe");
        Assert.Equal(HttpStatusCode.OK, admin.StatusCode);
    }

    [Fact]
    public async Task RememberedDevice_SurvivesSignOutAndNetworkChangesButStillRequiresPassword()
    {
        await using var fixture = await LocalAccountHttpFixture.CreateAsync(required: true);
        var account = await InitializeAsync(fixture, bound: true);
        using var verified = await VerifyAndRememberAsync(fixture, account);
        Assert.Equal(HttpStatusCode.Redirect, verified.StatusCode);
        var original = fixture.ReadTicket(BlogAuthSchemas.LocalAccountRememberedDevice);
        using var signOut = await fixture.SendAsync(HttpMethod.Get, "/auth/signout");
        Assert.Equal(HttpStatusCode.Redirect, signOut.StatusCode);
        using var anonymous = await fixture.SendAsync(HttpMethod.Get, "/admin/probe");
        Assert.Equal(HttpStatusCode.Redirect, anonymous.StatusCode);
        using var pending = await fixture.SendAsync(HttpMethod.Get, "/auth/verify-authenticator");
        Assert.Equal(HttpStatusCode.Redirect, pending.StatusCode);
        fixture.RemoteIpAddress = IPAddress.Parse("198.51.100.27");
        fixture.UserAgent = "Changed-browser/2.0";

        using var wrongPassword = await PasswordLoginAsync(fixture, account, "WrongPassword456!");
        Assert.Equal(HttpStatusCode.OK, wrongPassword.StatusCode);
        using var stillAnonymous = await fixture.SendAsync(HttpMethod.Get, "/admin/probe");
        Assert.Equal(HttpStatusCode.Redirect, stillAnonymous.StatusCode);
        using var login = await PasswordLoginAsync(fixture, account);

        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        Assert.Equal("/admin/dashboard", login.Headers.Location!.OriginalString);
        Assert.True(LocalAccountPrincipal.Matches(fixture.ReadTicket(BlogAuthSchemas.Local).Principal, account));
        Assert.DoesNotContain(login.Headers.GetValues("Set-Cookie"), header => header.StartsWith(RememberedDeviceCookieName + "="));
        Assert.Equal(original.Properties.ExpiresUtc, fixture.ReadTicket(BlogAuthSchemas.LocalAccountRememberedDevice).Properties.ExpiresUtc);
        using var admin = await fixture.SendAsync(HttpMethod.Get, "/admin/probe");
        Assert.Equal(HttpStatusCode.OK, admin.StatusCode);
    }

    [Fact]
    public async Task RememberedDevice_ExpiresAfterThirtyDaysWithoutRenewingOnPasswordLogin()
    {
        var clock = new LocalAccountPageFixture.TestClock();
        await using var fixture = await LocalAccountHttpFixture.CreateAsync(required: true, timeProvider: clock);
        var account = await InitializeAsync(fixture, bound: true);
        using var verified = await VerifyAndRememberAsync(fixture, account);
        Assert.Equal(HttpStatusCode.Redirect, verified.StatusCode);
        var original = fixture.ReadTicket(BlogAuthSchemas.LocalAccountRememberedDevice);
        clock.Advance(TimeSpan.FromDays(29));

        using var beforeExpiry = await PasswordLoginAsync(fixture, account);

        Assert.Equal("/admin/dashboard", beforeExpiry.Headers.Location!.OriginalString);
        Assert.DoesNotContain(beforeExpiry.Headers.GetValues("Set-Cookie"), header => header.StartsWith(RememberedDeviceCookieName + "="));
        Assert.Equal(original.Properties.ExpiresUtc, fixture.ReadTicket(BlogAuthSchemas.LocalAccountRememberedDevice).Properties.ExpiresUtc);
        clock.Advance(TimeSpan.FromDays(1) + TimeSpan.FromSeconds(1));
        using var afterExpiry = await PasswordLoginAsync(fixture, account);
        Assert.Equal("/auth/verify-authenticator", afterExpiry.Headers.Location!.OriginalString);
        using var admin = await fixture.SendAsync(HttpMethod.Get, "/admin/probe");
        Assert.Equal(HttpStatusCode.Redirect, admin.StatusCode);
    }

    [Theory]
    [InlineData("tampered")]
    [InlineData("other-owner")]
    [InlineData("password-changed")]
    [InlineData("email-changed")]
    [InlineData("authenticator-reset")]
    public async Task RememberedDevice_InvalidOrRevokedTrustRequiresAuthenticatorProof(string scenario)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fixture = await LocalAccountHttpFixture.CreateAsync(required: true);
        var account = await InitializeAsync(fixture, bound: true);
        using var verified = await VerifyAndRememberAsync(fixture, account);
        Assert.Equal(HttpStatusCode.Redirect, verified.StatusCode);
        var password = LocalAccountPageFixture.Password;
        switch (scenario)
        {
            case "tampered":
                fixture.CorruptTicket(BlogAuthSchemas.LocalAccountRememberedDevice);
                break;
            case "other-owner":
                fixture.SetTicket(LocalAccountPrincipal.Create(account with { Id = Guid.NewGuid() },
                    BlogAuthSchemas.LocalAccountRememberedDevice), BlogAuthSchemas.LocalAccountRememberedDevice);
                break;
            case "password-changed":
                password = "Replacement456!";
                account = (await fixture.Store.Accounts.ChangePasswordAsync(account.Id, account.Revision,
                    LocalAccountPageFixture.Password, password, ct))!;
                break;
            case "email-changed":
                account = (await fixture.Store.Accounts.ChangeLocalEmailAsync(account.Id, account.Revision,
                    password, "changed@example.test", ct))!;
                break;
            case "authenticator-reset":
                account = (await fixture.Store.Accounts.ResetTotpAsync(account.Id, account.Revision, password, ct))!;
                var secret = new LocalAccountTotpService().GenerateSecret();
                account = (await fixture.Store.Accounts.EnrollTotpAsync(account.Id, account.Revision,
                    secret, LocalAccountPageFixture.Code(secret), ct))!;
                break;
        }

        using var login = await PasswordLoginAsync(fixture, account, password);

        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        Assert.Equal(fixture.PathBase + "/auth/verify-authenticator", login.Headers.Location!.OriginalString);
        Assert.False(fixture.ReadTicket(BlogAuthSchemas.LocalAccountTwoFactor).Principal.IsInRole("Administrator"));
        using var admin = await fixture.SendAsync(HttpMethod.Get, "/admin/probe");
        Assert.Equal(HttpStatusCode.Redirect, admin.StatusCode);
    }

    [Fact]
    public async Task RememberedDevice_CannotSkipAuthenticatorEnrollment()
    {
        await using var fixture = await LocalAccountHttpFixture.CreateAsync(required: true);
        var account = await InitializeAsync(fixture);
        fixture.SetTicket(LocalAccountPrincipal.Create(account, BlogAuthSchemas.LocalAccountRememberedDevice),
            BlogAuthSchemas.LocalAccountRememberedDevice);

        using var login = await PasswordLoginAsync(fixture, account);

        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        Assert.Equal("/auth/setup-authenticator", login.Headers.Location!.OriginalString);
        Assert.False(fixture.ReadTicket(BlogAuthSchemas.LocalAccountSetup).Principal.IsInRole("Administrator"));
        using var admin = await fixture.SendAsync(HttpMethod.Get, "/admin/probe");
        Assert.Equal(HttpStatusCode.Redirect, admin.StatusCode);
    }

    private static async Task<HttpResponseMessage> PasswordLoginAsync(LocalAccountHttpFixture fixture,
        OwnerAccountSnapshot account, string password = LocalAccountPageFixture.Password)
    {
        var token = await fixture.FormTokenAsync("/auth/signin");
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        { ["Email"] = account.Email, ["Password"] = password });
        return await fixture.SendAsync(HttpMethod.Post, "/auth/signin", content, token);
    }

    private static async Task<HttpResponseMessage> VerifyAndRememberAsync(LocalAccountHttpFixture fixture,
        OwnerAccountSnapshot account, bool remember = true)
    {
        using var login = await PasswordLoginAsync(fixture, account);
        Assert.Equal(fixture.PathBase + "/auth/verify-authenticator", login.Headers.Location!.OriginalString);
        var token = await fixture.FormTokenAsync("/auth/verify-authenticator");
        var row = await fixture.Store.Db.OwnerAccount.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["AuthenticatorCode"] = LocalAccountPageFixture.Code(row.TotpSecret!),
            ["RememberDevice"] = remember.ToString()
        });
        return await fixture.SendAsync(HttpMethod.Post, "/auth/verify-authenticator", content, token);
    }
}
