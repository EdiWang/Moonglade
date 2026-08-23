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
using Moonglade.Email;
using Moonglade.Email.Core;
using Moonglade.Features.Asset;
using Moonglade.ImageStorage;
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
    public void TestEmail_UsesGlobalAntiforgeryValidation()
    {
        var method = typeof(SettingsController).GetMethod(nameof(SettingsController.TestEmail), Type.EmptyTypes);

        Assert.NotNull(method);
        Assert.Empty(method!.GetCustomAttributes(typeof(IgnoreAntiforgeryTokenAttribute), inherit: false));
    }

    [Theory]
    [InlineData(
        EmailCapabilityState.NotConfigured,
        StatusCodes.Status503ServiceUnavailable,
        "Add the required Email provider settings")]
    [InlineData(
        EmailCapabilityState.Invalid,
        StatusCodes.Status503ServiceUnavailable,
        "Email:OutboxWorker:PollIntervalSeconds")]
    [InlineData(
        EmailCapabilityState.Disabled,
        StatusCodes.Status409Conflict,
        "Email:OutboxWorker:Enabled")]
    public async Task TestEmail_WhenCapabilityUnavailable_ReturnsProblemWithoutPublishing(
        EmailCapabilityState state,
        int expectedStatusCode,
        string expectedDetail)
    {
        var eventMediator = new Mock<IEventMediator>(MockBehavior.Strict);
        var controller = CreateController(
            CreateEmailEnabledBlogConfig(),
            new RecordingCommandMediator(),
            Mock.Of<IQueryMediator>(),
            new Mock<IAuthenticationService>(),
            eventMediator: eventMediator.Object,
            emailCapabilityStatus: CreateEmailCapabilityStatus(state));

        var result = await controller.TestEmail();

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(expectedStatusCode, objectResult.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal(expectedStatusCode, problem.Status);
        Assert.Contains(expectedDetail, problem.Detail);
        eventMediator.Verify(
            mediator => mediator.PublishAsync(
                It.IsAny<TestEmailEvent>(),
                It.IsAny<EventMediationSettings>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task TestEmail_WhenBlogEmailSendingDisabled_ReturnsConflictWithoutPublishing()
    {
        var eventMediator = new Mock<IEventMediator>(MockBehavior.Strict);
        var blogConfig = CreateEmailEnabledBlogConfig();
        blogConfig.NotificationSettings.EnableEmailSending = false;
        var controller = CreateController(
            blogConfig,
            new RecordingCommandMediator(),
            Mock.Of<IQueryMediator>(),
            new Mock<IAuthenticationService>(),
            eventMediator: eventMediator.Object,
            emailCapabilityStatus: CreateEmailCapabilityStatus(EmailCapabilityState.Available));

        var result = await controller.TestEmail();

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, objectResult.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Contains("Enable email sending in Notification settings", problem.Detail);
        eventMediator.Verify(
            mediator => mediator.PublishAsync(
                It.IsAny<TestEmailEvent>(),
                It.IsAny<EventMediationSettings>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task TestEmail_WhenCapabilityAvailable_QueuesTestEmail()
    {
        var eventMediator = new Mock<IEventMediator>();
        eventMediator
            .Setup(mediator => mediator.PublishAsync(
                It.IsAny<TestEmailEvent>(),
                It.IsAny<EventMediationSettings>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var controller = CreateController(
            CreateEmailEnabledBlogConfig(),
            new RecordingCommandMediator(),
            Mock.Of<IQueryMediator>(),
            new Mock<IAuthenticationService>(),
            eventMediator: eventMediator.Object,
            emailCapabilityStatus: CreateEmailCapabilityStatus(EmailCapabilityState.Available));

        var result = await controller.TestEmail();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.True(Assert.IsType<bool>(okResult.Value));
        eventMediator.Verify(
            mediator => mediator.PublishAsync(
                It.IsAny<TestEmailEvent>(),
                It.IsAny<EventMediationSettings>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task TestEmail_WhenQueueingFails_ReturnsProblem()
    {
        var eventMediator = new Mock<IEventMediator>();
        eventMediator
            .Setup(mediator => mediator.PublishAsync(
                It.IsAny<TestEmailEvent>(),
                It.IsAny<EventMediationSettings>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Queue failed"));
        var controller = CreateController(
            CreateEmailEnabledBlogConfig(),
            new RecordingCommandMediator(),
            Mock.Of<IQueryMediator>(),
            new Mock<IAuthenticationService>(),
            eventMediator: eventMediator.Object,
            emailCapabilityStatus: CreateEmailCapabilityStatus(EmailCapabilityState.Available));

        var result = await controller.TestEmail();

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal("The test email could not be queued.", problem.Detail);
    }

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

    [Fact]
    public async Task Image_WhenCdnIsEnabled_MigratesAvatarToPrimaryStorageAndUsesDirectCdnUrl()
    {
        var avatarBytes = new byte[] { 1, 2, 3, 4 };
        var generalSettings = GeneralSettings.DefaultValue;
        generalSettings.AvatarUrl = "/assets/avatar";
        var blogConfig = new BlogConfig
        {
            GeneralSettings = generalSettings,
            ImageSettings = ImageSettings.DefaultValue
        };
        var commandMediator = new RecordingCommandMediator();
        var queryMediator = new Mock<IQueryMediator>();
        queryMediator
            .Setup(x => x.QueryAsync(
                It.Is<GetAssetQuery>(query => query.AssetId == AssetId.AvatarBase64),
                It.IsAny<QueryMediationSettings>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Convert.ToBase64String(avatarBytes));
        var imageStorage = new Mock<IBlogImageStorage>();
        var expectedFileName = $"avatar-{AssetId.AvatarBase64:N}.png";
        imageStorage
            .Setup(x => x.InsertAsync(expectedFileName, It.IsAny<byte[]>()))
            .ReturnsAsync("avatar-cdn.png");
        var controller = CreateController(
            blogConfig,
            commandMediator,
            queryMediator.Object,
            new Mock<IAuthenticationService>());
        var model = new ImageSettings
        {
            EnableCDNRedirect = true,
            CDNEndpoint = "https://cdn.example.com/images"
        };

        var result = await controller.Image(model, imageStorage.Object);

        Assert.IsType<NoContentResult>(result);
        imageStorage.Verify(x => x.InsertAsync(expectedFileName, It.Is<byte[]>(bytes => bytes.SequenceEqual(avatarBytes))), Times.Once);
        imageStorage.Verify(x => x.InsertOriginalAsync(It.IsAny<string>(), It.IsAny<byte[]>()), Times.Never);
        Assert.Equal("https://cdn.example.com/images/avatar-cdn.png", blogConfig.GeneralSettings.AvatarUrl);

        var settingsUpdates = commandMediator.Commands.OfType<UpdateConfigurationCommand>().ToList();
        Assert.Contains(settingsUpdates, command => command.Name == nameof(GeneralSettings));
        Assert.Contains(settingsUpdates, command => command.Name == nameof(ImageSettings));
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

    private static BlogConfig CreateEmailEnabledBlogConfig() => new()
    {
        NotificationSettings = new NotificationSettings
        {
            EnableEmailSending = true,
            EmailDisplayName = "Moonglade"
        }
    };

    private static EmailCapabilityStatus CreateEmailCapabilityStatus(EmailCapabilityState state)
    {
        var serviceOptions = new EmailServiceOptions
        {
            Provider = "smtp",
            SmtpServer = "smtp.example.com",
            SmtpUserName = "sender@example.com",
            SmtpPassword = "password",
            SmtpPort = 587
        };
        var workerOptions = new EmailOutboxWorkerOptions();

        switch (state)
        {
            case EmailCapabilityState.NotConfigured:
                serviceOptions.Provider = "AzureCommunication";
                serviceOptions.AcsConnectionString = "";
                serviceOptions.AcsSenderAddress = "";
                break;

            case EmailCapabilityState.Invalid:
                workerOptions.PollIntervalSeconds = -2;
                break;

            case EmailCapabilityState.Disabled:
                workerOptions.Enabled = false;
                break;
        }

        var evaluator = new EmailCapabilityStatusEvaluator(
            new EmailServiceOptionsValidator(),
            new EmailOutboxWorkerOptionsValidator());
        var status = evaluator.Evaluate(serviceOptions, workerOptions);
        Assert.Equal(state, status.State);
        return status;
    }

    private static SettingsController CreateController(
        LocalAccountSettings account,
        RecordingCommandMediator commandMediator,
        Mock<IAuthenticationService> authenticationService)
    {
        return CreateController(
            new BlogConfig { LocalAccountSettings = account },
            commandMediator,
            Mock.Of<IQueryMediator>(),
            authenticationService,
            account.Username);
    }

    private static SettingsController CreateController(
        BlogConfig blogConfig,
        RecordingCommandMediator commandMediator,
        IQueryMediator queryMediator,
        Mock<IAuthenticationService> authenticationService,
        string username = "admin",
        IEventMediator? eventMediator = null,
        EmailCapabilityStatus? emailCapabilityStatus = null)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(authenticationService.Object);
        serviceCollection.AddLogging();
        serviceCollection.AddControllers();
        var services = serviceCollection.BuildServiceProvider();

        var httpContext = new DefaultHttpContext
        {
            RequestServices = services
        };
        httpContext.User = new ClaimsPrincipal(
            new ClaimsIdentity([new Claim(ClaimTypes.Name, username)], "TestAuth"));
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");
        httpContext.Request.Headers.UserAgent = "unit-test-agent";

        var controller = new SettingsController(
            blogConfig,
            Mock.Of<ILogger<SettingsController>>(),
            eventMediator ?? Mock.Of<IEventMediator>(),
            emailCapabilityStatus ?? CreateEmailCapabilityStatus(EmailCapabilityState.Available),
            queryMediator,
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
