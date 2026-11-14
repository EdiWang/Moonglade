using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Moonglade.Auth;
using Moonglade.Web.Pages;
using Moonglade.Web.Tests.TestSupport;
using System.Security.Claims;
using System.Text.Json;

namespace Moonglade.Web.Tests.Pages.Auth;

public class SetupAccountModelTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OnPostAsync_TotpDisabled_InitializesOwnerSignsInAndRedirectsHome(bool profileOnly)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fixture = new LocalAccountPageFixture(required: false);
        var profile = profileOnly ? await fixture.Accounts.CreateProfileAsync("profile@example.test", ct) : null;
        var model = CreateModel(fixture);

        Assert.Equal("/Index", Assert.IsType<RedirectToPageResult>(await model.OnPostAsync()).PageName);

        var account = await fixture.Accounts.ReadAsync(ct);
        Assert.True(account!.IsLocalInitialized);
        Assert.False(account.IsTotpEnabled);
        Assert.Equal(LocalAccountPageFixture.Email, account.Email);
        if (profileOnly) Assert.Equal(profile!.Id, account.Id);
        AssertAdminTicket(fixture, account);
        Assert.Empty(model.Password);
        Assert.Equal(account, await fixture.Accounts.AuthenticateLocalAsync(account.Email, LocalAccountPageFixture.Password, ct));
    }

    [Fact]
    public async Task OnPostAsync_RequiredTotp_StagesOnlyProtectedHashAndRequiresActualCodeBeforeCommit()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fixture = new LocalAccountPageFixture();
        var model = CreateModel(fixture);

        Assert.Equal("/SetupAccount", Assert.IsType<RedirectToPageResult>(await model.OnPostAsync()).PageName);

        var ticket = Assert.Single(fixture.SignIns);
        Assert.Equal(BlogAuthSchemas.InitialAccountSetup, ticket.AuthenticationScheme);
        Assert.False(ticket.Principal.IsInRole("Administrator"));
        var json = ticket.Principal.FindFirstValue("moonglade:pending-setup")!;
        Assert.DoesNotContain(LocalAccountPageFixture.Password, json);
        var pending = JsonSerializer.Deserialize<PendingOwnerSetup>(json)!;
        Assert.True(pending.RequiresTotp);
        Assert.Empty(model.Password);
        Assert.False(await fixture.Db.OwnerAccount.AnyAsync(ct));
        Assert.Null(await fixture.Accounts.AuthenticateLocalAsync(model.Email, LocalAccountPageFixture.Password, ct));
        var proof = CreateModel(fixture);
        Assert.IsType<PageResult>(await proof.OnGetAsync());
        Assert.True(proof.IsVerifying);
        Assert.Equal(pending.TotpSecret, proof.SetupKey);
        proof.AuthenticatorCode = "invalid-code";
        Assert.IsType<PageResult>(await proof.OnPostVerifyAsync());
        Assert.Equal(400, proof.Response.StatusCode);
        Assert.False(await fixture.Db.OwnerAccount.AnyAsync(ct));
        Assert.Single(fixture.SignIns);

        proof = CreateModel(fixture);
        proof.AuthenticatorCode = LocalAccountPageFixture.Code(pending.TotpSecret!);
        Assert.Equal("/Index", Assert.IsType<RedirectToPageResult>(await proof.OnPostVerifyAsync()).PageName);

        var account = await fixture.Accounts.ReadAsync(ct);
        Assert.True(account!.IsLocalInitialized);
        Assert.True(account.IsTotpEnabled);
        Assert.Equal(pending.TotpSecret, (await fixture.Db.OwnerAccount.AsNoTracking().SingleAsync(ct)).TotpSecret);
        AssertAdminTicket(fixture, account);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("expired")]
    [InlineData("malformed")]
    [InlineData("competing-owner")]
    public async Task OnPostVerifyAsync_InvalidOrStaleAttempt_NeverCommitsCredentials(string scenario)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fixture = new LocalAccountPageFixture();
        if (scenario != "missing") await CreateModel(fixture).OnPostAsync();
        if (scenario == "expired") fixture.Clock.Advance(TimeSpan.FromMinutes(10));
        if (scenario == "malformed")
        {
            var principal = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("moonglade:pending-setup", "not-json")], BlogAuthSchemas.InitialAccountSetup));
            fixture.Tickets[BlogAuthSchemas.InitialAccountSetup] =
                new Microsoft.AspNetCore.Authentication.AuthenticationTicket(principal, BlogAuthSchemas.InitialAccountSetup);
        }
        OwnerAccountSnapshot? winner = null;
        if (scenario == "competing-owner") winner = await fixture.InitializeAsync(withTotp: true);
        var model = CreateModel(fixture);
        model.AuthenticatorCode = "invalid-code";

        Assert.IsType<PageResult>(await model.OnPostVerifyAsync());

        Assert.Equal(400, model.Response.StatusCode);
        Assert.Equal(winner, await fixture.Accounts.ReadAsync(ct));
        Assert.DoesNotContain(fixture.SignIns, ticket => ticket.AuthenticationScheme == CookieAuthenticationDefaults.AuthenticationScheme);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OnPostAsync_InvalidModelOrPassword_DoesNotStageOrCommit(bool invalidModel)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fixture = new LocalAccountPageFixture();
        var model = CreateModel(fixture);
        if (invalidModel) model.ModelState.AddModelError(nameof(model.Email), "Invalid");
        else model.Password = "short";

        Assert.IsType<PageResult>(await model.OnPostAsync());

        Assert.Equal(400, model.Response.StatusCode);
        Assert.Empty(fixture.SignIns);
        Assert.False(await fixture.Db.OwnerAccount.AnyAsync(ct));
    }

    [Fact]
    public async Task CompletedAccount_GetRedirectsHomeAndPostCannotClaimOwnerAgain()
    {
        await using var fixture = new LocalAccountPageFixture(required: false);
        var account = await fixture.InitializeAsync();
        var model = CreateModel(fixture);

        Assert.Equal("/Index", Assert.IsType<RedirectToPageResult>(await model.OnGetAsync()).PageName);
        Assert.Equal(409, Assert.IsType<StatusCodeResult>(await model.OnPostAsync()).StatusCode);
        Assert.Equal(account, await fixture.Accounts.ReadAsync(TestContext.Current.CancellationToken));
        Assert.Empty(fixture.SignIns);
    }

    private static SetupAccountModel CreateModel(LocalAccountPageFixture fixture)
    {
        var model = fixture.Attach(new SetupAccountModel(fixture.Options, fixture.Accounts, fixture.Totp));
        model.Email = LocalAccountPageFixture.Email;
        model.Password = LocalAccountPageFixture.Password;
        return model;
    }

    private static void AssertAdminTicket(LocalAccountPageFixture fixture, OwnerAccountSnapshot account)
    {
        var ticket = Assert.Single(fixture.SignIns, item => item.AuthenticationScheme == CookieAuthenticationDefaults.AuthenticationScheme);
        Assert.True(LocalAccountPrincipal.Matches(ticket.Principal, account));
        Assert.True(ticket.Principal.IsInRole("Administrator"));
        Assert.Equal(account.Email, ticket.Principal.Identity!.Name);
        Assert.Contains(BlogAuthSchemas.InitialAccountSetup, fixture.SignOuts);
        Assert.Contains(BlogAuthSchemas.LocalAccountSetup, fixture.SignOuts);
        Assert.Contains(BlogAuthSchemas.LocalAccountTwoFactor, fixture.SignOuts);
    }
}
