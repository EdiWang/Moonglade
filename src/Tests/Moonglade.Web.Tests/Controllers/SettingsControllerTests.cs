using Edi.AspNetCore.Utils;
using LiteBus.Events.Abstractions;
using LiteBus.Queries.Abstractions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moonglade.ActivityLog;
using Moonglade.Auth;
using Moonglade.Configuration;
using Moonglade.Email;
using Moonglade.Email.Core;
using Moonglade.Features.Asset;
using Moonglade.ImageStorage;
using Moonglade.Web.Controllers;
using Moq;
using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Moonglade.Web.Tests.TestSupport;

namespace Moonglade.Web.Tests.Controllers;

public class SettingsControllerTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Comment_PersistsMarkdownLinksAndUpdatesRuntimeSettings(bool enableLinks)
    {
        var blogConfig = new BlogConfig
        {
            CommentSettings = new CommentSettings { EnableMarkdownLinks = !enableLinks }
        };
        using var commandMediator = new ConfigurationCommandRecorder();
        var controller = CreateController(
            blogConfig,
            commandMediator,
            Mock.Of<IQueryMediator>(),
            new Mock<IAuthenticationService>());
        var model = new CommentSettings { EnableMarkdownLinks = enableLinks };

        var result = await controller.Comment(model);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal(enableLinks, blogConfig.CommentSettings.EnableMarkdownLinks);
        var update = commandMediator.Single<UpdateConfigurationCommand>();
        Assert.Equal(enableLinks, update.Settings[nameof(CommentSettings)].FromJson<CommentSettings>().EnableMarkdownLinks);
        Assert.Equal(EventType.SettingsCommentUpdated, commandMediator.Single<CreateActivityLogCommand>().EventType);
    }

    [Theory]
    [InlineData(EditorChoice.Html)]
    [InlineData(EditorChoice.Markdown)]
    public async Task Content_PersistsDefaultEditorAndUpdatesRuntimeSettings(EditorChoice editor)
    {
        var blogConfig = new BlogConfig();
        using var commandMediator = new ConfigurationCommandRecorder();
        var controller = CreateController(
            blogConfig,
            commandMediator,
            Mock.Of<IQueryMediator>(),
            new Mock<IAuthenticationService>());
        var model = new ContentSettings { DefaultEditor = editor };

        var result = await controller.Content(model);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal(editor, blogConfig.ContentSettings.DefaultEditor);
        var update = commandMediator.Single<UpdateConfigurationCommand>();
        Assert.Equal(editor, update.Settings[nameof(ContentSettings)].FromJson<ContentSettings>().DefaultEditor);
        Assert.Equal(EventType.SettingsContentUpdated, commandMediator.Single<CreateActivityLogCommand>().EventType);
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
            new ConfigurationCommandRecorder(),
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
            new ConfigurationCommandRecorder(),
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
            new ConfigurationCommandRecorder(),
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
        using var commandMediator = new ConfigurationCommandRecorder();
        var authenticationService = new Mock<IAuthenticationService>();
        var controller = CreateController(account, commandMediator, authenticationService);

        var result = await controller.ResetLocalAccountTotp(new ResetLocalAccountTotpRequest
        {
            CurrentPassword = "Password1"
        });

        Assert.IsType<NoContentResult>(result);

        var settings = JsonSerializer.Deserialize<LocalAccountSettings>(
            commandMediator.Single<UpdateConfigurationCommand>().Settings[nameof(LocalAccountSettings)],
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
        using var commandMediator = new ConfigurationCommandRecorder();
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

        var settingsUpdate = commandMediator.Single<UpdateConfigurationCommand>();
        Assert.Contains(nameof(GeneralSettings), settingsUpdate.Settings.Keys);
        Assert.Contains(nameof(ImageSettings), settingsUpdate.Settings.Keys);
    }

    [Fact]
    public async Task Content_WhenSaveFails_DoesNotChangeRuntimeOrLogSuccess()
    {
        var config = new BlogConfig();
        var original = config.ContentSettings;
        using var mediator = new ConfigurationCommandRecorder();
        var controller = CreateController(config, mediator, Mock.Of<IQueryMediator>(), new Mock<IAuthenticationService>());
        await mediator.Db.Database.ExecuteSqlRawAsync(
            "CREATE TRIGGER reject_settings BEFORE UPDATE ON BlogConfiguration BEGIN SELECT RAISE(ABORT, 'Save rejected'); END;",
            TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<DbUpdateException>(() => controller.Content(new ContentSettings { DefaultEditor = EditorChoice.Markdown }));

        Assert.Same(original, config.ContentSettings);
        Assert.Empty(mediator.Commands.OfType<CreateActivityLogCommand>());
        mediator.Db.ChangeTracker.Clear();
        Assert.Equal(original.ToJson(), (await mediator.Db.BlogConfiguration.SingleAsync(
            p => p.CfgKey == nameof(ContentSettings), TestContext.Current.CancellationToken)).CfgValue);
    }

    [Fact]
    public async Task Image_WhenOneSettingFails_RollsBackBothRowsAndPreservesRuntime()
    {
        var config = new BlogConfig
        {
            GeneralSettings = new GeneralSettings { AvatarUrl = "/assets/avatar" },
            ImageSettings = ImageSettings.DefaultValue
        };
        var originalGeneral = config.GeneralSettings;
        var originalImage = config.ImageSettings;
        using var mediator = new ConfigurationCommandRecorder();
        var query = new Mock<IQueryMediator>();
        query.Setup(p => p.QueryAsync(It.IsAny<GetAssetQuery>(), It.IsAny<QueryMediationSettings>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Convert.ToBase64String([1, 2, 3]));
        var storage = new Mock<IBlogImageStorage>();
        storage.Setup(p => p.InsertAsync(It.IsAny<string>(), It.IsAny<byte[]>())).ReturnsAsync("avatar.png");
        var controller = CreateController(config, mediator, query.Object, new Mock<IAuthenticationService>());
        await mediator.Db.Database.ExecuteSqlRawAsync(
            "CREATE TRIGGER reject_image_settings BEFORE UPDATE ON BlogConfiguration WHEN OLD.CfgKey = 'ImageSettings' BEGIN SELECT RAISE(ABORT, 'Save rejected'); END;",
            TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<DbUpdateException>(() => controller.Image(new ImageSettings
        {
            EnableCDNRedirect = true,
            CDNEndpoint = "https://cdn.example.com"
        }, storage.Object));

        Assert.Same(originalGeneral, config.GeneralSettings);
        Assert.Same(originalImage, config.ImageSettings);
        Assert.Empty(mediator.Commands.OfType<CreateActivityLogCommand>());
        mediator.Db.ChangeTracker.Clear();
        var rows = await mediator.Db.BlogConfiguration.ToDictionaryAsync(p => p.CfgKey, TestContext.Current.CancellationToken);
        Assert.Equal(originalGeneral.ToJson(), rows[nameof(GeneralSettings)].CfgValue);
        Assert.Equal(originalImage.ToJson(), rows[nameof(ImageSettings)].CfgValue);
    }

    [Fact]
    public async Task ResetLocalAccountTotp_WhenConfigurationIsMissing_DoesNotSignOutOrChangeAccount()
    {
        var account = CreateLocalAccount();
        using var mediator = new ConfigurationCommandRecorder();
        var authentication = new Mock<IAuthenticationService>();
        var controller = CreateController(account, mediator, authentication);
        await mediator.Db.BlogConfiguration.Where(p => p.CfgKey == nameof(LocalAccountSettings))
            .ExecuteDeleteAsync(TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<InvalidOperationException>(() => controller.ResetLocalAccountTotp(
            new ResetLocalAccountTotpRequest { CurrentPassword = "Password1" }));

        Assert.Same(account, mediator.Config.LocalAccountSettings);
        Assert.True(account.IsTotpEnabled);
        Assert.Empty(mediator.Commands.OfType<CreateActivityLogCommand>());
        authentication.Verify(p => p.SignOutAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<AuthenticationProperties>()), Times.Never);
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

    private static SettingsController CreateController(
        LocalAccountSettings account,
        ConfigurationCommandRecorder commandMediator,
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
        ConfigurationCommandRecorder commandMediator,
        IQueryMediator queryMediator,
        Mock<IAuthenticationService> authenticationService,
        string username = "admin",
        IEventMediator? eventMediator = null,
        EmailCapabilityStatus? emailCapabilityStatus = null)
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

}
