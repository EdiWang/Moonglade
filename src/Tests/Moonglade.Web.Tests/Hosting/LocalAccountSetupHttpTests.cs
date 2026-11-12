using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Moonglade.Auth;
using Moonglade.Web.Tests.TestSupport;
using System.Net;
using System.Security.Claims;
using System.Text.Json;

namespace Moonglade.Web.Tests.Hosting;

public class LocalAccountSetupHttpTests
{
    [Theory]
    [InlineData("", "/")]
    [InlineData("/blog", "/")]
    [InlineData("", "/post/example")]
    [InlineData("/blog", "/page/example")]
    [InlineData("", "/admin/probe")]
    [InlineData("/blog", "/auth/signin")]
    public async Task PendingLocalSetup_PageRoutes_RedirectWithinPathBaseWithoutExecutingBusiness(string pathBase, string path)
    {
        await using var fixture = await LocalAccountHttpFixture.CreateAsync(pathBase: pathBase);
        foreach (var method in new[] { HttpMethod.Get, HttpMethod.Head })
        {
            using var response = await fixture.SendAsync(method, path);
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal(pathBase + "/auth/setup-account", response.Headers.Location!.OriginalString);
            Assert.True(response.Headers.CacheControl!.NoStore);
        }
        Assert.Equal(0, fixture.BusinessExecutions);
    }

    [Theory]
    [InlineData("/api/probe")]
    [InlineData("/feed/rss")]
    [InlineData("/feed/atom")]
    [InlineData("/category/sample/rss")]
    [InlineData("/webmention")]
    [InlineData("/foaf.xml")]
    [InlineData("/robots.txt")]
    [InlineData("/sitemap.xml")]
    [InlineData("/opensearch")]
    [InlineData("/verification.txt")]
    [InlineData("/auth/setup-account-extra")]
    [InlineData("/health/extra")]
    public async Task PendingLocalSetup_BusinessProtocolsAndLookalikes_Return503WithoutExecution(string path)
    {
        await using var fixture = await LocalAccountHttpFixture.CreateAsync(pathBase: "/blog");
        foreach (var method in new[] { HttpMethod.Get, HttpMethod.Head, HttpMethod.Post })
        {
            using var response = await fixture.SendAsync(method, path);
            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            Assert.Null(response.Headers.Location);
            Assert.True(response.Headers.CacheControl!.NoStore);
            Assert.Equal(TimeSpan.FromSeconds(60), response.Headers.RetryAfter!.Delta);
        }
        Assert.Equal(0, fixture.BusinessExecutions);
    }

    [Fact]
    public async Task PendingLocalSetup_CompiledSetupHealthAndFingerprintAssets_RemainReachable()
    {
        await using var fixture = await LocalAccountHttpFixture.CreateAsync(pathBase: "/blog");
        await fixture.SetupTokenAsync();
        foreach (var path in new[] { "/health", "/health/ready" })
        {
            using var response = await fixture.SendAsync(HttpMethod.Get, path);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
        var manifestPath = Path.Combine(AppContext.BaseDirectory, "Moonglade.Web.staticwebassets.endpoints.json");
        using var manifest = JsonDocument.Parse(await File.ReadAllTextAsync(manifestPath, TestContext.Current.CancellationToken));
        var routes = manifest.RootElement.GetProperty("Endpoints").EnumerateArray()
            .Select(endpoint => endpoint.GetProperty("Route").GetString()!).Distinct().ToArray();
        var css = Assert.Single(routes, route => route.StartsWith("css/base.", StringComparison.Ordinal)
            && route.EndsWith(".css", StringComparison.Ordinal) && route != "css/base.css");
        var module = Assert.Single(routes, route => route.StartsWith("js/app/authenticator-code.", StringComparison.Ordinal)
            && route.EndsWith(".mjs", StringComparison.Ordinal) && route != "js/app/authenticator-code.mjs");
        foreach (var route in new[] { css, module })
        {
            using var response = await fixture.SendAsync(HttpMethod.Get, "/" + route);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(response.Content.Headers.ContentLength > 0);
            Assert.Equal(route == css ? "text/css" : "text/javascript", response.Content.Headers.ContentType!.MediaType);
        }
        using var business = await fixture.SendAsync(HttpMethod.Get, "/api/probe");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, business.StatusCode);
        Assert.Equal(0, fixture.BusinessExecutions);
    }

    [Fact]
    public async Task PendingLocalSetup_PreservesHttpsRedirectAndCultureNegotiation()
    {
        await using var fixture = await LocalAccountHttpFixture.CreateAsync(pathBase: "/blog");
        using var redirect = await fixture.SendAsync(HttpMethod.Get, "/post/example", https: false);
        Assert.Equal(HttpStatusCode.TemporaryRedirect, redirect.StatusCode);
        Assert.Equal("https://localhost/blog/post/example", redirect.Headers.Location!.OriginalString);
        using var setup = await fixture.SendAsync(HttpMethod.Get, "/auth/setup-account", language: "de-DE");
        Assert.Equal(HttpStatusCode.OK, setup.StatusCode);
        Assert.Equal("de-DE", Assert.Single(setup.Content.Headers.ContentLanguage));
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Production")]
    [InlineData("Staging")]
    public async Task TotpDisabled_RealHttpSetupCreatesAdminCookieUnlocksHomeAndRejectsReplay(string environment)
    {
        await using var fixture = await LocalAccountHttpFixture.CreateAsync(pathBase: "/blog", environment: environment);
        var token = await fixture.SetupTokenAsync();
        using var response = await fixture.SendAsync(HttpMethod.Post, "/auth/setup-account", SetupForm(), token);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/blog", response.Headers.Location!.OriginalString.TrimEnd('/'));
        var account = await fixture.Store.Accounts.ReadAsync(TestContext.Current.CancellationToken);
        Assert.True(account!.IsLocalInitialized);
        Assert.False(account.IsTotpEnabled);
        var ticket = fixture.ReadTicket(CookieAuthenticationDefaults.AuthenticationScheme);
        Assert.True(LocalAccountPrincipal.Matches(ticket.Principal, account));
        Assert.True(ticket.Principal.IsInRole("Administrator"));
        using var home = await fixture.SendAsync(HttpMethod.Get, "/");
        Assert.Equal(HttpStatusCode.OK, home.StatusCode);
        using var admin = await fixture.SendAsync(HttpMethod.Get, "/admin/probe");
        Assert.Equal(HttpStatusCode.OK, admin.StatusCode);
        using var refreshedToken = await fixture.SendAsync(HttpMethod.Get, "/admin/security-token");
        token = await refreshedToken.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var replay = await fixture.SendAsync(HttpMethod.Post, "/auth/setup-account", SetupForm(), token);
        Assert.Equal(HttpStatusCode.Conflict, replay.StatusCode);
        Assert.Equal(account, await fixture.Store.Accounts.ReadAsync(TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Production")]
    public async Task TotpRequired_ActualCodePrecedesCredentialCommitAndCookieCreation(string environment)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fixture = await LocalAccountHttpFixture.CreateAsync(required: true, environment: environment);
        var token = await fixture.SetupTokenAsync();
        using var staged = await fixture.SendAsync(HttpMethod.Post, "/auth/setup-account", SetupForm(), token);
        Assert.Equal(HttpStatusCode.Redirect, staged.StatusCode);
        Assert.False(await fixture.Store.Db.OwnerAccount.AnyAsync(ct));
        var pending = JsonSerializer.Deserialize<PendingOwnerSetup>(fixture.ReadTicket(BlogAuthSchemas.InitialAccountSetup)
            .Principal.FindFirstValue("moonglade:pending-setup")!)!;
        token = await fixture.SetupTokenAsync();
        using var rejected = await fixture.SendAsync(HttpMethod.Post, "/auth/setup-account?handler=Verify",
            ProofForm("invalid-code"), token);
        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
        Assert.False(await fixture.Store.Db.OwnerAccount.AnyAsync(ct));
        using var accepted = await fixture.SendAsync(HttpMethod.Post, "/auth/setup-account?handler=Verify",
            ProofForm(LocalAccountPageFixture.Code(pending.TotpSecret!)), token);
        Assert.Equal(HttpStatusCode.Redirect, accepted.StatusCode);
        Assert.Equal("/", accepted.Headers.Location!.OriginalString);
        var account = await fixture.Store.Accounts.ReadAsync(ct);
        Assert.True(account!.IsTotpEnabled);
        Assert.Equal(pending.TotpSecret, (await fixture.Store.Db.OwnerAccount.AsNoTracking().SingleAsync(ct)).TotpSecret);
        using var admin = await fixture.SendAsync(HttpMethod.Get, "/admin/probe");
        Assert.Equal(HttpStatusCode.OK, admin.StatusCode);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SetupPost_MissingOrInvalidAntiforgery_LeavesNoCredentials(bool invalid)
    {
        await using var fixture = await LocalAccountHttpFixture.CreateAsync();
        if (invalid) await fixture.SetupTokenAsync();
        using var response = await fixture.SendAsync(HttpMethod.Post, "/auth/setup-account", SetupForm(), invalid ? "invalid-token" : null);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False(await fixture.Store.Db.OwnerAccount.AnyAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SetupPost_ConfiguredRateLimit_Returns429WithoutCommitting()
    {
        await using var fixture = await LocalAccountHttpFixture.CreateAsync(permits: 1);
        var token = await fixture.SetupTokenAsync();
        using var first = await fixture.SendAsync(HttpMethod.Post, "/auth/setup-account", SetupForm("short"), token);
        Assert.Equal(HttpStatusCode.BadRequest, first.StatusCode);
        using var rejected = await fixture.SendAsync(HttpMethod.Post, "/auth/setup-account", SetupForm(), token);
        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        Assert.False(await fixture.Store.Db.OwnerAccount.AnyAsync(TestContext.Current.CancellationToken));
        Assert.Equal("application/problem+json", rejected.Content.Headers.ContentType!.MediaType);
    }

    [Fact]
    public async Task InitialSetup_TamperedEncryptedTicketCannotCommit()
    {
        await using var fixture = await LocalAccountHttpFixture.CreateAsync(required: true);
        var token = await fixture.SetupTokenAsync();
        using var staged = await fixture.SendAsync(HttpMethod.Post, "/auth/setup-account", SetupForm(), token);
        Assert.Equal(HttpStatusCode.Redirect, staged.StatusCode);
        fixture.CorruptTicket(BlogAuthSchemas.InitialAccountSetup);
        using var proof = await fixture.SendAsync(HttpMethod.Post, "/auth/setup-account?handler=Verify", ProofForm("123456"), token);
        Assert.Equal(HttpStatusCode.BadRequest, proof.StatusCode);
        Assert.False(await fixture.Store.Db.OwnerAccount.AnyAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task InitialSetup_ExpiredEncryptedTicketCannotCommitValidProof()
    {
        await using var fixture = await LocalAccountHttpFixture.CreateAsync(required: true);
        var token = await fixture.SetupTokenAsync();
        using var staged = await fixture.SendAsync(HttpMethod.Post, "/auth/setup-account", SetupForm(), token);
        Assert.Equal(HttpStatusCode.Redirect, staged.StatusCode);
        var ticket = fixture.ReadTicket(BlogAuthSchemas.InitialAccountSetup);
        var pending = JsonSerializer.Deserialize<PendingOwnerSetup>(ticket.Principal.FindFirstValue("moonglade:pending-setup")!)!;
        fixture.SetTicket(ticket.Principal, BlogAuthSchemas.InitialAccountSetup, DateTimeOffset.UtcNow.AddMinutes(-1));

        using var proof = await fixture.SendAsync(HttpMethod.Post, "/auth/setup-account?handler=Verify",
            ProofForm(LocalAccountPageFixture.Code(pending.TotpSecret!)), token);

        Assert.Equal(HttpStatusCode.BadRequest, proof.StatusCode);
        Assert.False(await fixture.Store.Db.OwnerAccount.AnyAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task InitialSetupProof_ConfiguredRateLimitRejectsActualValidCodeWithoutCommit()
    {
        await using var fixture = await LocalAccountHttpFixture.CreateAsync(required: true, permits: 2);
        var token = await fixture.SetupTokenAsync();
        using var staged = await fixture.SendAsync(HttpMethod.Post, "/auth/setup-account", SetupForm(), token);
        Assert.Equal(HttpStatusCode.Redirect, staged.StatusCode);
        var pending = JsonSerializer.Deserialize<PendingOwnerSetup>(fixture.ReadTicket(BlogAuthSchemas.InitialAccountSetup)
            .Principal.FindFirstValue("moonglade:pending-setup")!)!;
        using var rejected = await fixture.SendAsync(HttpMethod.Post, "/auth/setup-account?handler=Verify",
            ProofForm("invalid-code"), token);
        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);

        using var throttled = await fixture.SendAsync(HttpMethod.Post, "/auth/setup-account?handler=Verify",
            ProofForm(LocalAccountPageFixture.Code(pending.TotpSecret!)), token);

        Assert.Equal(HttpStatusCode.TooManyRequests, throttled.StatusCode);
        Assert.False(await fixture.Store.Db.OwnerAccount.AnyAsync(TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData("changed", "different@example.test")]
    [InlineData("empty", "")]
    [InlineData("omitted", null)]
    public async Task InitialSetupProof_ChangedOrMissingEmailCannotMintAnotherQuota(string scenario, string? email)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fixture = await LocalAccountHttpFixture.CreateAsync(required: true, permits: 2);
        var token = await fixture.SetupTokenAsync();
        using var staged = await fixture.SendAsync(HttpMethod.Post, "/auth/setup-account", SetupForm(), token);
        Assert.Equal(HttpStatusCode.Redirect, staged.StatusCode);
        Assert.False(await fixture.Store.Db.OwnerAccount.AnyAsync(ct));
        var pending = JsonSerializer.Deserialize<PendingOwnerSetup>(fixture.ReadTicket(BlogAuthSchemas.InitialAccountSetup)
            .Principal.FindFirstValue("moonglade:pending-setup")!)!;
        using var invalid = await fixture.SendAsync(HttpMethod.Post, "/auth/setup-account?handler=Verify",
            new FormUrlEncodedContent(new Dictionary<string, string>
            { ["Email"] = LocalAccountPageFixture.Email, ["AuthenticatorCode"] = "invalid-code" }), token);
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.False(await fixture.Store.Db.OwnerAccount.AnyAsync(ct));
        var fields = new Dictionary<string, string>
        {
            ["AuthenticatorCode"] = LocalAccountPageFixture.Code(pending.TotpSecret!)
        };
        if (scenario != "omitted") fields["Email"] = email!;

        using var throttled = await fixture.SendAsync(HttpMethod.Post, "/auth/setup-account?handler=Verify",
            new FormUrlEncodedContent(fields), token);

        Assert.Equal(HttpStatusCode.TooManyRequests, throttled.StatusCode);
        Assert.Equal("application/problem+json", throttled.Content.Headers.ContentType!.MediaType);
        Assert.False(await fixture.Store.Db.OwnerAccount.AnyAsync(ct));
    }

    [Fact]
    public async Task ProfileOnlyOwner_RemainsPendingUntilSetupRetainsItsStableIdentity()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fixture = await LocalAccountHttpFixture.CreateAsync(legacyOidcConfiguration: true);
        var profile = await fixture.Store.Accounts.CreateProfileAsync("profile@example.test", ct);
        using var home = await fixture.SendAsync(HttpMethod.Get, "/");
        using var api = await fixture.SendAsync(HttpMethod.Get, "/api/probe");
        using var retiredIdentity = await fixture.SendAsync(HttpMethod.Get, "/auth/identity");
        Assert.Equal(HttpStatusCode.NotFound, retiredIdentity.StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, home.StatusCode);
        Assert.Equal("/auth/setup-account", home.Headers.Location!.OriginalString);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, api.StatusCode);
        Assert.Equal(0, fixture.BusinessExecutions);
        var token = await fixture.SetupTokenAsync();

        using var completed = await fixture.SendAsync(HttpMethod.Post, "/auth/setup-account", SetupForm(), token);

        Assert.Equal(HttpStatusCode.Redirect, completed.StatusCode);
        var initialized = await fixture.Store.Accounts.ReadAsync(ct);
        Assert.Equal(profile.Id, initialized!.Id);
        Assert.NotEqual(profile.Revision, initialized.Revision);
        Assert.True(initialized.IsLocalInitialized);
        Assert.Equal(LocalAccountPageFixture.Email, initialized.Email);
    }

    private static FormUrlEncodedContent SetupForm(string password = LocalAccountPageFixture.Password) =>
        new(new Dictionary<string, string> { ["Email"] = LocalAccountPageFixture.Email, ["Password"] = password });

    private static FormUrlEncodedContent ProofForm(string code) =>
        new(new Dictionary<string, string> { ["AuthenticatorCode"] = code });
}
