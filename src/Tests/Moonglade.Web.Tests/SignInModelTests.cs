using LiteBus.Commands.Abstractions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Auth;
using Moonglade.Configuration;
using Moonglade.Web.Pages;
using Moq;
using System.Security.Claims;

namespace Moonglade.Web.Tests;

public class SignInModelTests
{
    [Fact]
    public async Task OnPostAsync_ValidPasswordWithoutTotp_RedirectsToSetup()
    {
        var authenticationService = new Mock<IAuthenticationService>();
        var model = CreateModel(
            LocalAccountSettings.DefaultValue,
            loginValid: true,
            authenticationService);
        model.Username = "admin";
        model.Password = "admin123";
        model.AuthenticatorCode = string.Empty;

        var result = await model.OnPostAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/SetupAuthenticator", redirect.PageName);
        authenticationService.Verify(
            x => x.SignInAsync(
                model.HttpContext,
                BlogAuthSchemas.LocalAccountSetup,
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<AuthenticationProperties>()),
            Times.Once);
    }

    [Fact]
    public async Task OnPostAsync_EnabledTotpWithWrongCode_ReturnsPageWithoutSignIn()
    {
        var authenticationService = new Mock<IAuthenticationService>();
        var account = LocalAccountSettings.DefaultValue;
        account.TotpSecret = "ABCDEF234567";
        account.IsTotpEnabled = true;
        var model = CreateModel(account, loginValid: true, authenticationService);
        model.Username = "admin";
        model.Password = "admin123";
        model.AuthenticatorCode = "000000";

        var result = await model.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.False(model.ModelState.IsValid);
        authenticationService.Verify(
            x => x.SignInAsync(
                It.IsAny<HttpContext>(),
                It.IsAny<string>(),
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<AuthenticationProperties>()),
            Times.Never);
    }

    private static SignInModel CreateModel(
        LocalAccountSettings account,
        bool loginValid,
        Mock<IAuthenticationService> authenticationService)
    {
        var services = new ServiceCollection()
            .AddSingleton(authenticationService.Object)
            .BuildServiceProvider();

        var httpContext = new DefaultHttpContext
        {
            RequestServices = services
        };
        httpContext.Request.Headers.UserAgent = "UnitTest";

        var blogConfig = new BlogConfig
        {
            LocalAccountSettings = account
        };

        var model = new SignInModel(
            Options.Create(new AuthenticationSettings { Provider = AuthenticationProvider.Local }),
            new StubCommandMediator(loginValid),
            Mock.Of<ILogger<SignInModel>>(),
            blogConfig,
            new LocalAccountTotpService());

        model.PageContext = new PageContext
        {
            HttpContext = httpContext
        };

        return model;
    }

    private sealed class StubCommandMediator(bool loginValid) : ICommandMediator
    {
        public Task SendAsync(ICommand command, CommandMediationSettings? settings, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<TCommandResult> SendAsync<TCommandResult>(
            ICommand<TCommandResult> command,
            CommandMediationSettings? settings,
            CancellationToken cancellationToken)
        {
            if (command is ValidateLoginCommand && typeof(TCommandResult) == typeof(bool))
            {
                return Task.FromResult((TCommandResult)(object)loginValid);
            }

            return Task.FromException<TCommandResult>(new NotSupportedException("No command results configured for this test."));
        }
    }
}
