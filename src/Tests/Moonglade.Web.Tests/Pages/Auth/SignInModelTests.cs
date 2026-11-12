using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Moonglade.Auth;
using Moonglade.Web.Pages;
using Moonglade.Web.Tests.TestSupport;
using Moq;
using System.Security.Claims;

namespace Moonglade.Web.Tests.Pages.Auth;

public class SignInModelTests
{
    [Fact]
    public async Task OnPostAsync_ValidPasswordWithoutTotp_RedirectsToSetup()
    {
        await using var fixture = new LocalAccountPageFixture();
        var account = await fixture.InitializeAsync();
        var model = CreateModel(fixture);

        var result = await model.OnPostAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/SetupAuthenticator", redirect.PageName);
        AssertPartialTicket(fixture, account, BlogAuthSchemas.LocalAccountSetup);
        Assert.Contains(BlogAuthSchemas.LocalAccountTwoFactor, fixture.SignOuts);
    }

    [Fact]
    public async Task OnPostAsync_EnabledTotp_RedirectsToVerifyAuthenticator()
    {
        await using var fixture = new LocalAccountPageFixture();
        var account = await fixture.InitializeAsync(withTotp: true);
        var model = CreateModel(fixture);

        var result = await model.OnPostAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/VerifyAuthenticator", redirect.PageName);
        AssertPartialTicket(fixture, account, BlogAuthSchemas.LocalAccountTwoFactor);
        Assert.Contains(BlogAuthSchemas.LocalAccountSetup, fixture.SignOuts);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OnPostAsync_WhenTotpIsNotRequired_SignsInAdminWithOrWithoutBoundAuthenticator(bool bound)
    {
        await using var fixture = new LocalAccountPageFixture(required: false);
        var account = await fixture.InitializeAsync(bound);
        var model = CreateModel(fixture);

        var result = await model.OnPostAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Admin/Dashboard", redirect.PageName);
        var ticket = Assert.Single(fixture.SignIns);
        Assert.Equal(CookieAuthenticationDefaults.AuthenticationScheme, ticket.AuthenticationScheme);
        Assert.True(LocalAccountPrincipal.Matches(ticket.Principal, account));
        Assert.True(ticket.Principal.IsInRole("Administrator"));
        Assert.Equal(account.Email, ticket.Principal.Identity!.Name);
    }

    [Fact]
    public async Task OnPostAsync_WhenPasswordIsInvalid_DoesNotSignInEvenWithTotpDisabled()
    {
        await using var fixture = new LocalAccountPageFixture(required: false);
        var account = await fixture.InitializeAsync();
        var model = CreateModel(fixture);
        model.Password = "WrongPassword456!";

        var result = await model.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.False(model.ModelState.IsValid);
        Assert.Empty(fixture.SignIns);
        Assert.Equal(account, await fixture.Accounts.ReadAsync(TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OnPostAsync_InvalidModelOrMissingUserAgent_DoesNotAuthenticate(bool invalidModel)
    {
        await using var fixture = new LocalAccountPageFixture(required: false);
        await fixture.InitializeAsync();
        var model = CreateModel(fixture);
        if (invalidModel) model.ModelState.AddModelError(nameof(model.Email), "Invalid");
        else model.Request.Headers.UserAgent = string.Empty;

        var result = await model.OnPostAsync();

        if (invalidModel)
        {
            Assert.IsType<PageResult>(result);
            Assert.Equal(400, model.Response.StatusCode);
        }
        else Assert.IsType<UnauthorizedResult>(result);
        Assert.Empty(fixture.SignIns);
    }

    private static SignInModel CreateModel(LocalAccountPageFixture fixture)
    {
        var model = fixture.Attach(new SignInModel(fixture.Options, fixture.Accounts, Mock.Of<ILogger<SignInModel>>()));
        model.Email = LocalAccountPageFixture.Email;
        model.Password = LocalAccountPageFixture.Password;
        return model;
    }

    private static void AssertPartialTicket(LocalAccountPageFixture fixture, OwnerAccountSnapshot account, string scheme)
    {
        var ticket = Assert.Single(fixture.SignIns);
        Assert.Equal(scheme, ticket.AuthenticationScheme);
        Assert.True(LocalAccountPrincipal.Matches(ticket.Principal, account));
        Assert.False(ticket.Principal.IsInRole("Administrator"));
        Assert.Equal(account.Id.ToString(), ticket.Principal.FindFirstValue(ClaimTypes.NameIdentifier));
    }
}
