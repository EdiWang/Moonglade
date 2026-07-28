using LiteBus.Commands.Abstractions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Auth;
using Moonglade.Configuration;
using Moonglade.Data;
using Moonglade.Web.Pages;
using Moq;
using System.Security.Claims;
using System.Text.Json;

namespace Moonglade.Web.Tests;

public class SetupAuthenticatorModelTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public async Task OnGetAsync_GeneratesSecretAndBuildsAuthenticatorDisplay()
    {
        var commandMediator = new RecordingCommandMediator();
        var model = CreateModel(
            LocalAccountSettings.DefaultValue,
            commandMediator,
            new FakeTotpService { GeneratedSecret = "JBSWY3DPEHPK3PXP" });

        var result = await model.OnGetAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal("JBSWY3DPEHPK3PXP", model.SetupKey);
        Assert.StartsWith("data:image/png;base64,", model.QrCodeImageUri);

        var settings = JsonSerializer.Deserialize<LocalAccountSettings>(
            commandMediator.Single<UpdateConfigurationCommand>().Json,
            JsonOptions)!;
        Assert.Equal("JBSWY3DPEHPK3PXP", settings.TotpSecret);
        Assert.False(settings.IsTotpEnabled);
    }

    [Fact]
    public async Task OnPostAsync_InvalidModelState_ReturnsBadRequestAndKeepsSetupDisplay()
    {
        var commandMediator = new RecordingCommandMediator();
        var authenticationService = new Mock<IAuthenticationService>();
        var model = CreateModel(
            LocalAccountSettings.DefaultValue,
            commandMediator,
            new FakeTotpService { GeneratedSecret = "JBSWY3DPEHPK3PXP" },
            authenticationService);
        model.ModelState.AddModelError(nameof(SetupAuthenticatorModel.AuthenticatorCode), "Required");

        var result = await model.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, model.Response.StatusCode);
        Assert.Equal("JBSWY3DPEHPK3PXP", model.SetupKey);
        Assert.StartsWith("data:image/png;base64,", model.QrCodeImageUri);
        authenticationService.Verify(
            x => x.SignInAsync(
                It.IsAny<HttpContext>(),
                It.IsAny<string>(),
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<AuthenticationProperties>()),
            Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_InvalidCode_ReturnsPageWithoutSigningIn()
    {
        var commandMediator = new RecordingCommandMediator();
        var authenticationService = new Mock<IAuthenticationService>();
        var account = LocalAccountSettings.DefaultValue;
        account.TotpSecret = "JBSWY3DPEHPK3PXP";
        var model = CreateModel(
            account,
            commandMediator,
            new FakeTotpService { VerifyResult = false },
            authenticationService);
        model.AuthenticatorCode = "123456";

        var result = await model.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.False(model.ModelState.IsValid);
        Assert.Equal("JBSWY3DPEHPK3PXP", model.SetupKey);
        Assert.Empty(commandMediator.Commands);
        authenticationService.Verify(
            x => x.SignInAsync(
                It.IsAny<HttpContext>(),
                It.IsAny<string>(),
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<AuthenticationProperties>()),
            Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_ValidCode_EnablesTotpAndSignsInAdmin()
    {
        var commandMediator = new RecordingCommandMediator();
        var authenticationService = new Mock<IAuthenticationService>();
        var account = LocalAccountSettings.DefaultValue;
        account.TotpSecret = "JBSWY3DPEHPK3PXP";
        var model = CreateModel(
            account,
            commandMediator,
            new FakeTotpService { VerifyResult = true },
            authenticationService);
        model.AuthenticatorCode = "123456";

        var result = await model.OnPostAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Admin/Dashboard", redirect.PageName);

        var settings = JsonSerializer.Deserialize<LocalAccountSettings>(
            commandMediator.Single<UpdateConfigurationCommand>().Json,
            JsonOptions)!;
        Assert.True(settings.IsTotpEnabled);
        Assert.Equal("JBSWY3DPEHPK3PXP", settings.TotpSecret);

        authenticationService.Verify(
            x => x.SignOutAsync(model.HttpContext, BlogAuthSchemas.LocalAccountSetup, null),
            Times.Once);
        authenticationService.Verify(
            x => x.SignInAsync(
                model.HttpContext,
                CookieAuthenticationDefaults.AuthenticationScheme,
                It.Is<ClaimsPrincipal>(p => p.Identity!.Name == account.Username),
                It.IsAny<AuthenticationProperties>()),
            Times.Once);
    }

    private static SetupAuthenticatorModel CreateModel(
        LocalAccountSettings account,
        RecordingCommandMediator commandMediator,
        ILocalAccountTotpService totpService,
        Mock<IAuthenticationService>? authenticationService = null)
    {
        authenticationService ??= new Mock<IAuthenticationService>();
        var services = new ServiceCollection()
            .AddSingleton(authenticationService.Object)
            .BuildServiceProvider();

        var httpContext = new DefaultHttpContext
        {
            RequestServices = services
        };

        var model = new SetupAuthenticatorModel(
            Options.Create(new AuthenticationSettings
            {
                Provider = AuthenticationProvider.Local,
                Totp = new TotpAuthenticationSettings { Issuer = "Moonglade Test" }
            }),
            new BlogConfig { LocalAccountSettings = account },
            commandMediator,
            totpService,
            Mock.Of<ILogger<SetupAuthenticatorModel>>());

        model.PageContext = new PageContext
        {
            HttpContext = httpContext
        };

        return model;
    }

    private sealed class FakeTotpService : ILocalAccountTotpService
    {
        public string GeneratedSecret { get; init; } = "JBSWY3DPEHPK3PXP";
        public bool VerifyResult { get; init; }

        public string GenerateSecret() => GeneratedSecret;

        public bool VerifyCode(string secret, string code) => VerifyResult;

        public string BuildAuthenticatorUri(string issuer, string accountName, string secret) =>
            $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(accountName)}?secret={secret}&issuer={Uri.EscapeDataString(issuer)}";
    }

    private sealed class RecordingCommandMediator : ICommandMediator
    {
        public List<ICommand> Commands { get; } = [];

        public Task SendAsync(ICommand command, CommandMediationSettings? settings, CancellationToken cancellationToken)
        {
            Commands.Add(command);
            return Task.CompletedTask;
        }

        public Task<TCommandResult> SendAsync<TCommandResult>(
            ICommand<TCommandResult> command,
            CommandMediationSettings? settings,
            CancellationToken cancellationToken)
        {
            Commands.Add(command);

            if (typeof(TCommandResult) == typeof(OperationCode))
            {
                return Task.FromResult((TCommandResult)(object)OperationCode.Done);
            }

            return Task.FromException<TCommandResult>(new NotSupportedException("No command results configured for this test."));
        }

        public TCommand Single<TCommand>() where TCommand : ICommand =>
            Commands.OfType<TCommand>().Single();
    }
}
