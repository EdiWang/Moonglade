using Edi.AspNetCore.Utils;
using LiteBus.Events.Abstractions;
using LiteBus.Queries.Abstractions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moonglade.ActivityLog;
using Moonglade.Auth;
using Moonglade.Configuration;
using Moonglade.Features.Asset;
using Moonglade.ImageStorage;
using Moonglade.Web.Controllers;
using Moonglade.Web.Tests.TestSupport;
using Moq;
using System.ComponentModel.DataAnnotations;

namespace Moonglade.Web.Tests.Controllers;

public partial class SettingsControllerTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task SettingsSave_WhenAuditFails_PreservesCommittedValuesAndReturnsSuccess(bool general, bool canceledAudit)
    {
        var config = new BlogConfig { GeneralSettings = GeneralSettings.DefaultValue };
        var failure = CreateAuditFailure(canceledAudit);
        using var mediator = new ConfigurationCommandRecorder { ActivityLogFailure = failure };
        var logger = new Mock<ILogger<SettingsController>>();
        var controller = CreateController(config, mediator, Mock.Of<IQueryMediator>(), new Mock<IAuthenticationService>(), logger: logger.Object);
        var content = new ContentSettings { DefaultEditor = EditorChoice.Markdown };
        var site = GeneralSettings.DefaultValue;
        site.SiteTitle = "Saved title";
        site.AvatarUrl = config.GeneralSettings.AvatarUrl;
        var expectedJson = general ? site.ToJson() : content.ToJson();
        var key = general ? nameof(GeneralSettings) : nameof(ContentSettings);

        var result = general ? await controller.General(site) : await controller.Content(content);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal(expectedJson, general ? config.GeneralSettings.ToJson() : config.ContentSettings.ToJson());
        Assert.Equal(expectedJson, await ReadStoredSettings(mediator, key));
        Assert.Collection(mediator.Commands,
            command => Assert.IsType<UpdateConfigurationCommand>(command),
            command => Assert.Equal(general ? EventType.SettingsGeneralUpdated : EventType.SettingsContentUpdated,
                Assert.IsType<CreateActivityLogCommand>(command).EventType));
        AssertAuditFailure(logger, controller, failure,
            general ? EventType.SettingsGeneralUpdated : EventType.SettingsContentUpdated, site.SiteTitle);
    }

    [Fact]
    public async Task Image_WhenAuditFails_PreservesPairedSettingsAndUploadedAvatar()
    {
        var config = new BlogConfig { GeneralSettings = GeneralSettings.DefaultValue, ImageSettings = ImageSettings.DefaultValue };
        config.GeneralSettings.AvatarUrl = "/assets/avatar";
        var failure = CreateAuditFailure();
        using var mediator = new ConfigurationCommandRecorder { ActivityLogFailure = failure };
        var query = new Mock<IQueryMediator>();
        query.Setup(p => p.QueryAsync(It.IsAny<GetAssetQuery>(), It.IsAny<QueryMediationSettings>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Convert.ToBase64String([1, 2, 3]));
        var storage = new Mock<IBlogImageStorage>();
        storage.Setup(p => p.InsertAsync(It.IsAny<string>(), It.IsAny<byte[]>())).ReturnsAsync("saved-avatar.png");
        var logger = new Mock<ILogger<SettingsController>>();
        var controller = CreateController(config, mediator, query.Object, new Mock<IAuthenticationService>(), logger: logger.Object);
        var image = ImageSettings.DefaultValue;
        image.EnableCDNRedirect = true;
        image.CDNEndpoint = "https://cdn.example.com/images";

        var result = await controller.Image(image, storage.Object);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal("https://cdn.example.com/images/saved-avatar.png", config.GeneralSettings.AvatarUrl);
        Assert.Equal(image.ToJson(), config.ImageSettings.ToJson());
        var storedGeneral = (await ReadStoredSettings(mediator, nameof(GeneralSettings))).FromJson<GeneralSettings>();
        Assert.Equal("https://cdn.example.com/images/saved-avatar.png", storedGeneral.AvatarUrl);
        Assert.Equal(config.GeneralSettings.SiteTitle, storedGeneral.SiteTitle);
        Assert.Equal(image.ToJson(), await ReadStoredSettings(mediator, nameof(ImageSettings)));
        storage.Verify(p => p.InsertAsync($"avatar-{AssetId.AvatarBase64:N}.png",
            It.Is<byte[]>(bytes => bytes.SequenceEqual(new byte[] { 1, 2, 3 }))), Times.Once);
        AssertAuditFailure(logger, controller, failure, EventType.SettingsImageUpdated, image.CDNEndpoint);
    }

    [Fact]
    public async Task Password_WhenAuditFails_PreservesNewCredentialsAndExistingSession()
    {
        var original = CreateLocalAccount();
        var config = new BlogConfig { LocalAccountSettings = original };
        var failure = CreateAuditFailure();
        using var mediator = new ConfigurationCommandRecorder { ActivityLogFailure = failure };
        var authentication = new Mock<IAuthenticationService>();
        var logger = new Mock<ILogger<SettingsController>>();
        var controller = CreateController(config, mediator, Mock.Of<IQueryMediator>(), authentication, logger: logger.Object);
        var request = new UpdateLocalAccountPasswordRequest
        {
            OldPassword = "Password1",
            NewPassword = "NewPassword2",
            NewUsername = "updated-admin"
        };

        var result = await controller.UpdateLocalAccountPassword(request);

        Assert.IsType<NoContentResult>(result);
        var saved = (await ReadStoredSettings(mediator, nameof(LocalAccountSettings))).FromJson<LocalAccountSettings>();
        Assert.Equal("updated-admin", saved.Username);
        Assert.NotEqual(original.PasswordSalt, saved.PasswordSalt);
        Assert.Equal(SecurityHelper.HashPassword("NewPassword2", saved.PasswordSalt), saved.PasswordHash);
        Assert.Equal(original.TotpSecret, saved.TotpSecret);
        Assert.True(saved.IsTotpEnabled);
        Assert.Equal(saved.ToJson(), config.LocalAccountSettings.ToJson());
        authentication.Verify(p => p.SignOutAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<AuthenticationProperties>()), Times.Never);
        AssertAuditFailure(logger, controller, failure, EventType.SettingsPasswordUpdated,
            request.OldPassword, request.NewPassword, request.NewUsername, saved.PasswordHash, saved.PasswordSalt, saved.TotpSecret);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ResetTotp_WhenAuditFails_PreservesResetAndSignsOutAllSchemes(bool canceledAudit)
    {
        var original = CreateLocalAccount();
        var config = new BlogConfig { LocalAccountSettings = original };
        var failure = CreateAuditFailure(canceledAudit);
        using var mediator = new ConfigurationCommandRecorder { ActivityLogFailure = failure };
        var authentication = new Mock<IAuthenticationService>();
        var logger = new Mock<ILogger<SettingsController>>();
        var controller = CreateController(config, mediator, Mock.Of<IQueryMediator>(), authentication, logger: logger.Object);
        authentication.Setup(p => p.SignOutAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<AuthenticationProperties>()))
            .Callback(() => Assert.False(config.LocalAccountSettings.IsTotpEnabled))
            .Returns(Task.CompletedTask);

        var result = await controller.ResetLocalAccountTotp(new ResetLocalAccountTotpRequest { CurrentPassword = "Password1" });

        Assert.IsType<NoContentResult>(result);
        var saved = (await ReadStoredSettings(mediator, nameof(LocalAccountSettings))).FromJson<LocalAccountSettings>();
        Assert.Equal(original.Username, saved.Username);
        Assert.Equal(original.PasswordHash, saved.PasswordHash);
        Assert.Equal(original.PasswordSalt, saved.PasswordSalt);
        Assert.Empty(saved.TotpSecret);
        Assert.False(saved.IsTotpEnabled);
        Assert.Equal(saved.ToJson(), config.LocalAccountSettings.ToJson());
        foreach (var scheme in new[] { CookieAuthenticationDefaults.AuthenticationScheme, BlogAuthSchemas.LocalAccountSetup, BlogAuthSchemas.LocalAccountTwoFactor })
            authentication.Verify(p => p.SignOutAsync(controller.HttpContext, scheme, null), Times.Once);
        authentication.VerifyNoOtherCalls();
        AssertAuditFailure(logger, controller, failure, EventType.SettingsAuthenticatorReset,
            "Password1", original.PasswordHash, original.PasswordSalt, original.TotpSecret, original.Username);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AccountSave_WhenPersistenceFails_PreservesCredentialsWithoutAuditingOrSignOut(bool resetTotp)
    {
        var account = CreateLocalAccount();
        var config = new BlogConfig { LocalAccountSettings = account };
        using var mediator = new ConfigurationCommandRecorder { ActivityLogFailure = CreateAuditFailure() };
        var authentication = new Mock<IAuthenticationService>();
        var logger = new Mock<ILogger<SettingsController>>();
        var controller = CreateController(config, mediator, Mock.Of<IQueryMediator>(), authentication, logger: logger.Object);
        await mediator.Db.Database.ExecuteSqlRawAsync(
            "CREATE TRIGGER reject_settings BEFORE UPDATE ON BlogConfiguration BEGIN SELECT RAISE(ABORT, 'Save rejected'); END;",
            TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<DbUpdateException>(() => resetTotp
            ? controller.ResetLocalAccountTotp(new ResetLocalAccountTotpRequest { CurrentPassword = "Password1" })
            : controller.UpdateLocalAccountPassword(new UpdateLocalAccountPasswordRequest
            {
                OldPassword = "Password1", NewPassword = "NewPassword2", NewUsername = "updated-admin"
            }));

        Assert.Same(account, config.LocalAccountSettings);
        Assert.Equal(account.ToJson(), await ReadStoredSettings(mediator, nameof(LocalAccountSettings)));
        Assert.Empty(mediator.Commands.OfType<CreateActivityLogCommand>());
        Assert.Empty(logger.Invocations);
        authentication.Verify(p => p.SignOutAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<AuthenticationProperties>()), Times.Never);
    }

    [Fact]
    public async Task Image_WhenStorageFails_PropagatesFailureWithoutSavingOrAuditing()
    {
        var config = new BlogConfig { GeneralSettings = GeneralSettings.DefaultValue, ImageSettings = ImageSettings.DefaultValue };
        config.GeneralSettings.AvatarUrl = "/assets/avatar";
        var originalGeneral = config.GeneralSettings;
        var originalImage = config.ImageSettings;
        using var mediator = new ConfigurationCommandRecorder { ActivityLogFailure = CreateAuditFailure() };
        var query = new Mock<IQueryMediator>();
        query.Setup(p => p.QueryAsync(It.IsAny<GetAssetQuery>(), It.IsAny<QueryMediationSettings>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Convert.ToBase64String([1, 2, 3]));
        var failure = new IOException("Image storage unavailable.");
        var storage = new Mock<IBlogImageStorage>();
        storage.Setup(p => p.InsertAsync(It.IsAny<string>(), It.IsAny<byte[]>())).ThrowsAsync(failure);
        var controller = CreateController(config, mediator, query.Object, new Mock<IAuthenticationService>());
        var image = ImageSettings.DefaultValue;
        image.EnableCDNRedirect = true;
        image.CDNEndpoint = "https://cdn.example.com/images";

        Assert.Same(failure, await Assert.ThrowsAsync<IOException>(() => controller.Image(image, storage.Object)));

        Assert.Empty(mediator.Commands);
        Assert.Same(originalGeneral, config.GeneralSettings);
        Assert.Same(originalImage, config.ImageSettings);
        Assert.Equal(originalGeneral.ToJson(), await ReadStoredSettings(mediator, nameof(GeneralSettings)));
        Assert.Equal(originalImage.ToJson(), await ReadStoredSettings(mediator, nameof(ImageSettings)));
    }

    [Fact]
    public async Task Content_WhenValidationFails_DoesNotSaveOrAudit()
    {
        var config = new BlogConfig();
        var original = config.ContentSettings;
        using var mediator = new ConfigurationCommandRecorder { ActivityLogFailure = CreateAuditFailure() };
        var controller = CreateController(config, mediator, Mock.Of<IQueryMediator>(), new Mock<IAuthenticationService>());

        await Assert.ThrowsAsync<ValidationException>(() => controller.Content(new ContentSettings { PostListPageSize = 0 }));

        Assert.Same(original, config.ContentSettings);
        Assert.Equal(original.ToJson(), await ReadStoredSettings(mediator, nameof(ContentSettings)));
        Assert.Empty(mediator.Commands.OfType<CreateActivityLogCommand>());
    }

    [Fact]
    public async Task Content_WhenCacheInvalidationFails_PropagatesFailureWithoutAuditing()
    {
        var config = new BlogConfig();
        using var mediator = new ConfigurationCommandRecorder { ActivityLogFailure = CreateAuditFailure() };
        var failure = new InvalidOperationException("Cache invalidation failed.");
        Mock.Get(mediator.EventMediator).Setup(p => p.PublishAsync(
                It.IsAny<IEvent>(), It.IsAny<EventMediationSettings>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(failure);
        var controller = CreateController(config, mediator, Mock.Of<IQueryMediator>(), new Mock<IAuthenticationService>());
        var content = new ContentSettings { DefaultEditor = EditorChoice.Markdown };

        Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => controller.Content(content)));

        Assert.Equal(content.ToJson(), config.ContentSettings.ToJson());
        Assert.Equal(content.ToJson(), await ReadStoredSettings(mediator, nameof(ContentSettings)));
        Assert.Empty(mediator.Commands.OfType<CreateActivityLogCommand>());
    }

    [Fact]
    public async Task ResetTotp_WhenSignOutFails_PropagatesFailureEvenAfterAuditFailure()
    {
        var config = new BlogConfig { LocalAccountSettings = CreateLocalAccount() };
        using var mediator = new ConfigurationCommandRecorder { ActivityLogFailure = CreateAuditFailure() };
        var failure = new InvalidOperationException("Sign-out failed.");
        var authentication = new Mock<IAuthenticationService>();
        authentication.Setup(p => p.SignOutAsync(It.IsAny<HttpContext>(), CookieAuthenticationDefaults.AuthenticationScheme, null))
            .ThrowsAsync(failure);
        var controller = CreateController(config, mediator, Mock.Of<IQueryMediator>(), authentication);

        Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() =>
            controller.ResetLocalAccountTotp(new ResetLocalAccountTotpRequest { CurrentPassword = "Password1" })));

        Assert.False(config.LocalAccountSettings.IsTotpEnabled);
        Assert.Empty(config.LocalAccountSettings.TotpSecret);
        var saved = (await ReadStoredSettings(mediator, nameof(LocalAccountSettings))).FromJson<LocalAccountSettings>();
        Assert.False(saved.IsTotpEnabled);
        Assert.Empty(saved.TotpSecret);
        Assert.Single(mediator.Commands.OfType<CreateActivityLogCommand>());
        authentication.Verify(p => p.SignOutAsync(controller.HttpContext, CookieAuthenticationDefaults.AuthenticationScheme, null), Times.Once);
    }

    private static Exception CreateAuditFailure(bool canceled = false) => canceled
        ? new OperationCanceledException("Audit dispatch canceled.")
        : new InvalidOperationException("Audit dispatch unavailable.");

    private static async Task<string> ReadStoredSettings(ConfigurationCommandRecorder mediator, string key) =>
        (await mediator.Db.BlogConfiguration.AsNoTracking().SingleAsync(p => p.CfgKey == key, TestContext.Current.CancellationToken)).CfgValue;

    private static void AssertAuditFailure(Mock<ILogger<SettingsController>> logger, SettingsController controller,
        Exception failure, EventType eventType, params string[] excludedValues)
    {
        var invocation = Assert.Single(logger.Invocations, p => p.Method.Name == nameof(ILogger.Log));
        Assert.Equal(LogLevel.Error, invocation.Arguments[0]);
        Assert.Same(failure, invocation.Arguments[3]);
        var state = Assert.IsAssignableFrom<IReadOnlyList<KeyValuePair<string, object?>>>(invocation.Arguments[2]);
        Assert.Contains(state, p => p.Key == "EventType" && Equals(p.Value, eventType));
        Assert.Contains(state, p => p.Key == "TraceIdentifier" && Equals(p.Value, controller.HttpContext.TraceIdentifier));
        var message = invocation.Arguments[2]?.ToString() ?? string.Empty;
        Assert.Contains("saved", message, StringComparison.OrdinalIgnoreCase);
        foreach (var value in excludedValues)
            Assert.DoesNotContain(value, message);
    }
}
