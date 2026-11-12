using Edi.AspNetCore.Utils;
using LiteBus.Events.Abstractions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.ActivityLog;
using Moonglade.Auth;
using Moonglade.Configuration;
using Moonglade.Data.Entities;
using Moonglade.Email;
using Moonglade.Email.Core;
using Moonglade.Web.Controllers;
using Moonglade.Web.Tests.TestSupport;
using Moq;
using System.Net;
using System.Security.Claims;
using System.Text.Json;

namespace Moonglade.Web.Tests.Controllers;

public partial class SettingsControllerTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Comment_PersistsMarkdownLinksAndUpdatesRuntimeSettings(bool enableLinks)
    {
        var blogConfig = new BlogConfig(new BlogSettingsSnapshot
        {
            CommentSettings = new CommentSettings { EnableMarkdownLinks = !enableLinks }
        });
        using var commandMediator = new ConfigurationCommandRecorder();
        var controller = CreateController(
            blogConfig,
            commandMediator,
            new Mock<IAuthenticationService>());
        var model = new CommentSettings { EnableMarkdownLinks = enableLinks };

        var result = await controller.Comment(model);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal(enableLinks, blogConfig.Capture().CommentSettings.EnableMarkdownLinks);
        var update = commandMediator.Single<UpdateConfigurationCommand>();
        Assert.Equal(enableLinks, Assert.IsType<CommentSettings>(update.Settings).EnableMarkdownLinks);
        Assert.Equal(ActivityEvents.SettingsCommentUpdated, commandMediator.Single<CreateActivityLogCommand>().EventKey);
    }

    [Theory]
    [InlineData(EditorChoice.Html)]
    [InlineData(EditorChoice.Markdown)]
    public async Task Content_PersistsDefaultEditorAndUpdatesRuntimeSettings(EditorChoice editor)
    {
        var blogConfig = new BlogConfig(new BlogSettingsSnapshot
        {
            ContentSettings = new ContentSettings { DefaultEditor = editor == EditorChoice.Html ? EditorChoice.Markdown : EditorChoice.Html }
        });
        using var commandMediator = new ConfigurationCommandRecorder();
        var controller = CreateController(
            blogConfig,
            commandMediator,
            new Mock<IAuthenticationService>());
        var model = new ContentSettings { DefaultEditor = editor };

        var result = await controller.Content(model);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal(editor, blogConfig.Capture().ContentSettings.DefaultEditor);
        var update = commandMediator.Single<UpdateConfigurationCommand>();
        Assert.Equal(editor, Assert.IsType<ContentSettings>(update.Settings).DefaultEditor);
        Assert.Equal(ActivityEvents.SettingsContentUpdated, commandMediator.Single<CreateActivityLogCommand>().EventKey);
    }

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
        "Email:Enabled")]
    public async Task TestEmail_WhenCapabilityUnavailable_ReturnsProblemWithoutPublishing(
        EmailCapabilityState state,
        int expectedStatusCode,
        string expectedDetail)
    {
        var eventMediator = new Mock<IEventMediator>(MockBehavior.Strict);
        var controller = CreateController(
            CreateEmailEnabledBlogConfig(),
            new ConfigurationCommandRecorder(),
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
        blogConfig.Publish(blogConfig.Capture() with { NotificationSettings = blogConfig.Capture().NotificationSettings with { EnableEmailSending = false } });
        var controller = CreateController(
            blogConfig,
            new ConfigurationCommandRecorder(),
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
            new ConfigurationCommandRecorder(),
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
            new ConfigurationCommandRecorder(),
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
        using var commandMediator = new ConfigurationCommandRecorder();
        var authenticationService = new Mock<IAuthenticationService>();
        var controller = CreateController(account, commandMediator, authenticationService);

        var result = await controller.ResetLocalAccountTotp(new ResetLocalAccountTotpRequest
        {
            CurrentPassword = "Password1"
        });

        Assert.IsType<NoContentResult>(result);

        var settings = await commandMediator.Db.OwnerAccount.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(account.Email, settings.Email);
        Assert.Equal(account.PasswordHash, settings.PasswordHash);
        Assert.Equal(account.PasswordSalt, settings.PasswordSalt);
        Assert.Null(settings.TotpSecret);
        Assert.NotNull(settings.LocalInitializedUtc);

        var activityLog = commandMediator.Single<CreateActivityLogCommand>();
        Assert.Equal(ActivityEvents.SettingsAuthenticatorReset, activityLog.EventKey);
        Assert.Equal(account.Email, activityLog.TargetDisplayName);
        Assert.Null(activityLog.Details);

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
        using var commandMediator = new ConfigurationCommandRecorder();
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Image_SavesImageSettingsAndPreservesGeneralSettings(bool enableCdn)
    {
        var blogConfig = new BlogConfig(new BlogSettingsSnapshot
        {
            GeneralSettings = GeneralSettings.DefaultValue,
            ImageSettings = ImageSettings.DefaultValue
        });
        using var mediator = new ConfigurationCommandRecorder();
        var controller = CreateController(blogConfig, mediator, new Mock<IAuthenticationService>());
        var originalGeneral = blogConfig.Capture().GeneralSettings;
        var originalRow = await mediator.Db.BlogConfiguration.AsNoTracking().SingleAsync(
            row => row.CfgKey == nameof(GeneralSettings), TestContext.Current.CancellationToken);
        var image = ImageSettings.DefaultValue with { EnableCDNRedirect = enableCdn, CDNEndpoint = "https://cdn.example.com/images" };

        Assert.IsType<NoContentResult>(await controller.Image(image));

        Assert.Same(originalGeneral, blogConfig.Capture().GeneralSettings);
        var saved = await mediator.Db.BlogConfiguration.AsNoTracking().SingleAsync(
            row => row.CfgKey == nameof(GeneralSettings), TestContext.Current.CancellationToken);
        Assert.Equal(originalRow.CfgValue, saved.CfgValue);
        Assert.Equal(originalRow.LastModifiedTimeUtc, saved.LastModifiedTimeUtc);
        Assert.Equal(image.ToJson(), blogConfig.Capture().ImageSettings.ToJson());
        Assert.IsType<ImageSettings>(mediator.Single<UpdateConfigurationCommand>().Settings);
    }

    [Fact]
    public async Task Content_WhenSaveFails_DoesNotChangeRuntimeOrLogSuccess()
    {
        var config = new BlogConfig();
        var original = config.Capture().ContentSettings;
        using var mediator = new ConfigurationCommandRecorder();
        var controller = CreateController(config, mediator, new Mock<IAuthenticationService>());
        await mediator.Db.Database.ExecuteSqlRawAsync(
            "CREATE TRIGGER reject_settings BEFORE UPDATE ON BlogConfiguration BEGIN SELECT RAISE(ABORT, 'Save rejected'); END;",
            TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<DbUpdateException>(() => controller.Content(new ContentSettings { DefaultEditor = EditorChoice.Markdown }));

        Assert.Same(original, config.Capture().ContentSettings);
        Assert.Empty(mediator.Commands.OfType<CreateActivityLogCommand>());
        mediator.Db.ChangeTracker.Clear();
        Assert.Equal(original.ToJson(), (await mediator.Db.BlogConfiguration.SingleAsync(
            p => p.CfgKey == nameof(ContentSettings), TestContext.Current.CancellationToken)).CfgValue);
    }

    [Fact]
    public async Task Image_WhenPersistenceFails_PreservesBothImageAndGeneralSettings()
    {
        var config = new BlogConfig(new BlogSettingsSnapshot
        {
            GeneralSettings = GeneralSettings.DefaultValue,
            ImageSettings = ImageSettings.DefaultValue
        });
        using var mediator = new ConfigurationCommandRecorder();
        var controller = CreateController(config, mediator, new Mock<IAuthenticationService>());
        var original = config.Capture();
        await mediator.Db.Database.ExecuteSqlRawAsync(
            "CREATE TRIGGER reject_image_settings BEFORE UPDATE ON BlogConfiguration BEGIN SELECT RAISE(ABORT, 'Settings save rejected'); END;",
            TestContext.Current.CancellationToken);
        var image = ImageSettings.DefaultValue with { EnableCDNRedirect = true, CDNEndpoint = "https://cdn.example.com" };

        await Assert.ThrowsAsync<DbUpdateException>(() => controller.Image(image));

        Assert.Same(original, config.Capture());
        var rows = await mediator.Db.BlogConfiguration.AsNoTracking().ToDictionaryAsync(row => row.CfgKey, TestContext.Current.CancellationToken);
        Assert.Equal(original.GeneralSettings.ToJson(), rows[nameof(GeneralSettings)].CfgValue);
        Assert.Equal(original.ImageSettings.ToJson(), rows[nameof(ImageSettings)].CfgValue);
        Assert.Empty(mediator.Commands.OfType<CreateActivityLogCommand>());
    }

    [Fact]
    public async Task ResetLocalAccountTotp_WhenAccountIsMissing_DoesNotSignOutOrAudit()
    {
        var account = CreateLocalAccount();
        using var mediator = new ConfigurationCommandRecorder();
        var authentication = new Mock<IAuthenticationService>();
        var controller = CreateController(account, mediator, authentication);
        await mediator.Db.OwnerAccount.ExecuteDeleteAsync(TestContext.Current.CancellationToken);

        Assert.IsType<UnauthorizedResult>(await controller.ResetLocalAccountTotp(
            new ResetLocalAccountTotpRequest { CurrentPassword = "Password1" }));

        Assert.Null(await mediator.Accounts.ReadAsync(TestContext.Current.CancellationToken));
        Assert.Empty(mediator.Commands.OfType<CreateActivityLogCommand>());
        authentication.Verify(p => p.SignOutAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<AuthenticationProperties>()), Times.Never);
    }

    private static OwnerAccountEntity CreateLocalAccount()
    {
        var salt = SecurityHelper.GenerateSalt();

        return new()
        {
            Id = Guid.NewGuid(),
            Email = "owner@example.test",
            NormalizedEmail = "OWNER@EXAMPLE.TEST",
            Revision = Guid.NewGuid(),
            LocalInitializedUtc = DateTime.UtcNow.AddDays(-1),
            PasswordSalt = salt,
            PasswordHash = SecurityHelper.HashPassword("Password1", salt),
            TotpSecret = "JBSWY3DPEHPK3PXP"
        };
    }

    private static BlogConfig CreateEmailEnabledBlogConfig() => new(new BlogSettingsSnapshot
    {
        NotificationSettings = new NotificationSettings
        {
            EnableEmailSending = true,
            EmailDisplayName = "Moonglade"
        }
    });

    private static EmailCapabilityStatus CreateEmailCapabilityStatus(EmailCapabilityState state)
    {
        var serviceOptions = new EmailServiceOptions
        {
            SmtpServer = "smtp.example.com",
            SmtpUserName = "sender@example.com",
            SmtpPassword = "password",
            SmtpPort = 587
        };
        var workerOptions = new EmailOutboxWorkerOptions();

        switch (state)
        {
            case EmailCapabilityState.NotConfigured:
                serviceOptions.SmtpPassword = "";
                break;

            case EmailCapabilityState.Invalid:
                workerOptions.PollIntervalSeconds = -2;
                break;

            case EmailCapabilityState.Disabled:
                serviceOptions.Enabled = false;
                break;
        }

        var evaluator = new EmailCapabilityStatusEvaluator(
            new EmailServiceOptionsValidator(),
            new EmailOutboxWorkerOptionsValidator());
        var status = evaluator.Evaluate(serviceOptions, workerOptions);
        Assert.Equal(state, status.State);
        return status;
    }

    private static AccountController CreateController(
        OwnerAccountEntity account,
        ConfigurationCommandRecorder commandMediator,
        Mock<IAuthenticationService> authenticationService,
        ILogger<AccountController>? logger = null)
    {
        commandMediator.Db.OwnerAccount.Add(account);
        commandMediator.Db.SaveChanges();
        commandMediator.Db.Entry(account).State = EntityState.Detached;
        var services = new ServiceCollection().AddSingleton(authenticationService.Object).BuildServiceProvider();
        var snapshot = new OwnerAccountSnapshot(account.Id, account.Email, account.Revision, true, account.TotpSecret is not null);
        var controller = new AccountController(commandMediator.Accounts,
            commandMediator, logger ?? Mock.Of<ILogger<AccountController>>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    RequestServices = services,
                    User = LocalAccountPrincipal.Create(snapshot, CookieAuthenticationDefaults.AuthenticationScheme)
                }
            }
        };
        controller.HttpContext.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");
        controller.Request.Headers.UserAgent = "unit-test-agent";
        return controller;
    }

    private static SettingsController CreateController(
        BlogConfig blogConfig,
        ConfigurationCommandRecorder commandMediator,
        Mock<IAuthenticationService> authenticationService,
        string username = "admin",
        IEventMediator? eventMediator = null,
        EmailCapabilityStatus? emailCapabilityStatus = null,
        ILogger<SettingsController>? logger = null)
    {
        commandMediator.Initialize(blogConfig);
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
            logger ?? Mock.Of<ILogger<SettingsController>>(),
            eventMediator ?? Mock.Of<IEventMediator>(),
            emailCapabilityStatus ?? CreateEmailCapabilityStatus(EmailCapabilityState.Available),
            commandMediator,
            new TestOwnerProfileReader());

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext,
            RouteData = new RouteData(),
            ActionDescriptor = new ControllerActionDescriptor()
        };

        return controller;
    }

}
