using Edi.AspNetCore.Utils;
using LiteBus.Commands.Abstractions;
using LiteBus.Events.Abstractions;
using LiteBus.Queries.Abstractions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moonglade.ActivityLog;
using Moonglade.Auth;
using Moonglade.Configuration;
using Moonglade.Data;
using Moonglade.Web.Controllers;
using Moq;
using System.Net;
using System.Security.Claims;
using System.Text.Json;

namespace Moonglade.Web.Tests;

public class SettingsControllerTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public async Task ResetLocalAccountTotp_WhenPasswordIsValid_DisablesTotpAndSignsOut()
    {
        var account = CreateLocalAccount();
        var commandMediator = new RecordingCommandMediator();
        var authenticationService = new Mock<IAuthenticationService>();
        var controller = CreateController(account, commandMediator, authenticationService);

        var result = await controller.ResetLocalAccountTotp(new ResetLocalAccountTotpRequest
        {
            CurrentPassword = "Password1"
        });

        Assert.IsType<NoContentResult>(result);

        var settings = JsonSerializer.Deserialize<LocalAccountSettings>(
            commandMediator.Single<UpdateConfigurationCommand>().Json,
            JsonOptions)!;
        Assert.Equal(account.Username, settings.Username);
        Assert.Equal(account.PasswordHash, settings.PasswordHash);
        Assert.Equal(account.PasswordSalt, settings.PasswordSalt);
        Assert.Empty(settings.TotpSecret);
        Assert.False(settings.IsTotpEnabled);

        var activityLog = commandMediator.Single<CreateActivityLogCommand>();
        Assert.Equal(EventType.SettingsAuthenticatorReset, activityLog.EventType);
        Assert.Equal(account.Username, activityLog.TargetName);

        authenticationService.Verify(
            x => x.SignOutAsync(controller.HttpContext, CookieAuthenticationDefaults.AuthenticationScheme, null),
            Times.Once);
        authenticationService.Verify(
            x => x.SignOutAsync(controller.HttpContext, BlogAuthSchemas.LocalAccountSetup, null),
            Times.Once);
        authenticationService.Verify(
            x => x.SignOutAsync(controller.HttpContext, BlogAuthSchemas.LocalAccountTwoFactor, null),
            Times.Once);
    }

    [Fact]
    public async Task ResetLocalAccountTotp_WhenPasswordIsInvalid_DoesNotUpdateSettingsOrSignOut()
    {
        var commandMediator = new RecordingCommandMediator();
        var authenticationService = new Mock<IAuthenticationService>();
        var controller = CreateController(CreateLocalAccount(), commandMediator, authenticationService);

        var result = await controller.ResetLocalAccountTotp(new ResetLocalAccountTotpRequest
        {
            CurrentPassword = "WrongPassword1"
        });

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal("Current password is incorrect.", conflict.Value);
        Assert.Empty(commandMediator.Commands);
        authenticationService.Verify(
            x => x.SignOutAsync(
                It.IsAny<HttpContext>(),
                It.IsAny<string>(),
                It.IsAny<AuthenticationProperties>()),
            Times.Never);
    }

    private static LocalAccountSettings CreateLocalAccount()
    {
        var salt = SecurityHelper.GenerateSalt();

        return new()
        {
            Username = "admin",
            PasswordSalt = salt,
            PasswordHash = SecurityHelper.HashPassword("Password1", salt),
            TotpSecret = "JBSWY3DPEHPK3PXP",
            IsTotpEnabled = true
        };
    }

    private static SettingsController CreateController(
        LocalAccountSettings account,
        RecordingCommandMediator commandMediator,
        Mock<IAuthenticationService> authenticationService)
    {
        var services = new ServiceCollection()
            .AddSingleton(authenticationService.Object)
            .BuildServiceProvider();

        var httpContext = new DefaultHttpContext
        {
            RequestServices = services
        };
        httpContext.User = new ClaimsPrincipal(
            new ClaimsIdentity([new Claim(ClaimTypes.Name, account.Username)], "TestAuth"));
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");
        httpContext.Request.Headers.UserAgent = "unit-test-agent";

        var controller = new SettingsController(
            new BlogConfig { LocalAccountSettings = account },
            Mock.Of<ILogger<SettingsController>>(),
            Mock.Of<IEventMediator>(),
            Mock.Of<IQueryMediator>(),
            commandMediator);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext,
            RouteData = new RouteData(),
            ActionDescriptor = new ControllerActionDescriptor()
        };

        return controller;
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
