using Edi.AspNetCore.Utils;
using LiteBus.Events.Abstractions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moonglade.ActivityLog;
using Moonglade.Auth;
using Moonglade.Configuration;
using Moonglade.Web.Controllers;
using Moonglade.Web.Tests.TestSupport;
using Moq;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

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
        var config = new BlogConfig(new BlogSettingsSnapshot { GeneralSettings = GeneralSettings.DefaultValue });
        var failure = CreateAuditFailure(canceledAudit);
        using var mediator = new ConfigurationCommandRecorder { ActivityLogFailure = failure };
        var logger = new Mock<ILogger<SettingsController>>();
        var controller = CreateController(config, mediator, new Mock<IAuthenticationService>(), logger: logger.Object);
        var content = new ContentSettings { DefaultEditor = EditorChoice.Markdown };
        var site = GeneralSettings.DefaultValue;
        site = site with { SiteTitle = "Saved title" };
        var expectedJson = general ? site.ToJson() : content.ToJson();
        var key = general ? nameof(GeneralSettings) : nameof(ContentSettings);

        var result = general ? await controller.General(site) : await controller.Content(content);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal(expectedJson, general ? config.Capture().GeneralSettings.ToJson() : config.Capture().ContentSettings.ToJson());
        Assert.Equal(expectedJson, await ReadStoredSettings(mediator, key));
        Assert.Collection(mediator.Commands,
            command => Assert.IsType<UpdateConfigurationCommand>(command),
            command => Assert.Equal(general ? ActivityEvents.SettingsGeneralUpdated : ActivityEvents.SettingsContentUpdated,
                Assert.IsType<CreateActivityLogCommand>(command).EventKey));
        AssertAuditFailure(logger, controller, failure,
            general ? ActivityEvents.SettingsGeneralUpdated : ActivityEvents.SettingsContentUpdated, site.SiteTitle);
    }

    [Fact]
    public async Task Image_WhenAuditFails_PreservesSavedImageSettingsAndUnchangedGeneral()
    {
        var config = new BlogConfig(new BlogSettingsSnapshot
        {
            GeneralSettings = GeneralSettings.DefaultValue,
            ImageSettings = ImageSettings.DefaultValue
        });
        var original = config.Capture().GeneralSettings;
        var failure = CreateAuditFailure();
        using var mediator = new ConfigurationCommandRecorder { ActivityLogFailure = failure };
        var logger = new Mock<ILogger<SettingsController>>();
        var controller = CreateController(config, mediator, new Mock<IAuthenticationService>(), logger: logger.Object);
        var image = ImageSettings.DefaultValue with { EnableCDNRedirect = true, CDNEndpoint = "https://cdn.example.com/images" };

        Assert.IsType<NoContentResult>(await controller.Image(image));

        Assert.Same(original, config.Capture().GeneralSettings);
        Assert.Equal(original.ToJson(), await ReadStoredSettings(mediator, nameof(GeneralSettings)));
        Assert.Equal(image.ToJson(), await ReadStoredSettings(mediator, nameof(ImageSettings)));
        AssertAuditFailure(logger, controller, failure, ActivityEvents.SettingsImageUpdated, image.CDNEndpoint);
    }

    [Fact]
    public async Task Password_WhenAuditFails_PreservesNewCredentialsAndExistingSession()
    {
        var original = CreateLocalAccount();
        var config = new BlogConfig();
        var failure = CreateAuditFailure();
        using var mediator = new ConfigurationCommandRecorder { ActivityLogFailure = failure };
        var authentication = new Mock<IAuthenticationService>();
        var logger = new Mock<ILogger<AccountController>>();
        var controller = CreateController(original, mediator, authentication, logger.Object);
        var request = new UpdateLocalAccountPasswordRequest
        {
            OldPassword = "Password1",
            NewPassword = "NewPassword2"
        };

        var result = await controller.UpdateLocalAccountPassword(request);

        Assert.IsType<NoContentResult>(result);
        var saved = await mediator.Db.OwnerAccount.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(original.Email, saved.Email);
        Assert.Equal(original.Id, saved.Id);
        Assert.NotEqual(original.PasswordSalt, saved.PasswordSalt);
        Assert.Equal(SecurityHelper.HashPassword("NewPassword2", saved.PasswordSalt), saved.PasswordHash);
        Assert.Equal(original.TotpSecret, saved.TotpSecret);
        Assert.NotEqual(original.Revision, saved.Revision);
        var current = await mediator.Accounts.ReadAsync(TestContext.Current.CancellationToken);
        Assert.True(current!.IsTotpEnabled);
        authentication.Verify(p => p.SignInAsync(controller.HttpContext, CookieAuthenticationDefaults.AuthenticationScheme,
            It.Is<ClaimsPrincipal>(principal => LocalAccountPrincipal.Matches(principal, current)), It.IsAny<AuthenticationProperties>()), Times.Once);
        authentication.Verify(p => p.SignOutAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<AuthenticationProperties>()), Times.Never);
        AssertAuditFailure(logger, controller, failure, ActivityEvents.SettingsPasswordUpdated,
            request.OldPassword, request.NewPassword, saved.PasswordHash!, saved.PasswordSalt!, saved.TotpSecret!);
        Assert.Null(mediator.Single<CreateActivityLogCommand>().Details);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ResetTotp_WhenAuditFails_PreservesResetAndSignsOutAllSchemes(bool canceledAudit)
    {
        var original = CreateLocalAccount();
        var config = new BlogConfig();
        var failure = CreateAuditFailure(canceledAudit);
        using var mediator = new ConfigurationCommandRecorder { ActivityLogFailure = failure };
        var authentication = new Mock<IAuthenticationService>();
        var logger = new Mock<ILogger<AccountController>>();
        var controller = CreateController(original, mediator, authentication, logger.Object);
        authentication.Setup(p => p.SignOutAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<AuthenticationProperties>()))
            .Callback(() => Assert.Null(mediator.Db.OwnerAccount.AsNoTracking().Single().TotpSecret))
            .Returns(Task.CompletedTask);

        var result = await controller.ResetLocalAccountTotp(new ResetLocalAccountTotpRequest { CurrentPassword = "Password1" });

        Assert.IsType<NoContentResult>(result);
        var saved = await mediator.Db.OwnerAccount.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(original.Email, saved.Email);
        Assert.Equal(original.PasswordHash, saved.PasswordHash);
        Assert.Equal(original.PasswordSalt, saved.PasswordSalt);
        Assert.Null(saved.TotpSecret);
        Assert.False((await mediator.Accounts.ReadAsync(TestContext.Current.CancellationToken))!.IsTotpEnabled);
        Assert.NotNull(saved.LocalInitializedUtc);
        foreach (var scheme in new[] { CookieAuthenticationDefaults.AuthenticationScheme, BlogAuthSchemas.LocalAccountSetup, BlogAuthSchemas.LocalAccountTwoFactor })
            authentication.Verify(p => p.SignOutAsync(controller.HttpContext, scheme, null), Times.Once);
        authentication.VerifyNoOtherCalls();
        AssertAuditFailure(logger, controller, failure, ActivityEvents.SettingsAuthenticatorReset,
            "Password1", original.PasswordHash!, original.PasswordSalt!, original.TotpSecret!, original.Email);
        Assert.Null(mediator.Single<CreateActivityLogCommand>().Details);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AccountSave_WhenPersistenceFails_PreservesCredentialsWithoutAuditingOrSignOut(bool resetTotp)
    {
        var account = CreateLocalAccount();
        var config = new BlogConfig();
        using var mediator = new ConfigurationCommandRecorder { ActivityLogFailure = CreateAuditFailure() };
        var authentication = new Mock<IAuthenticationService>();
        var logger = new Mock<ILogger<AccountController>>();
        var controller = CreateController(account, mediator, authentication, logger.Object);
        await mediator.Db.Database.ExecuteSqlRawAsync(
            "CREATE TRIGGER reject_settings BEFORE UPDATE ON OwnerAccount BEGIN SELECT RAISE(ABORT, 'Save rejected'); END;",
            TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<DbUpdateException>(() => resetTotp
            ? controller.ResetLocalAccountTotp(new ResetLocalAccountTotpRequest { CurrentPassword = "Password1" })
            : controller.UpdateLocalAccountPassword(new UpdateLocalAccountPasswordRequest
            {
                OldPassword = "Password1",
                NewPassword = "NewPassword2"
            }));

        var saved = await mediator.Db.OwnerAccount.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(account.PasswordHash, saved.PasswordHash);
        Assert.Equal(account.PasswordSalt, saved.PasswordSalt);
        Assert.Equal(account.TotpSecret, saved.TotpSecret);
        Assert.Equal(account.Revision, saved.Revision);
        Assert.Empty(mediator.Commands.OfType<CreateActivityLogCommand>());
        Assert.Empty(logger.Invocations);
        authentication.Verify(p => p.SignOutAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<AuthenticationProperties>()), Times.Never);
    }

    [Fact]
    public async Task Image_DoesNotWriteGeneralSettingsEvenWhenThoseWritesWouldFail()
    {
        var config = new BlogConfig(new BlogSettingsSnapshot
        {
            GeneralSettings = GeneralSettings.DefaultValue,
            ImageSettings = ImageSettings.DefaultValue
        });
        using var mediator = new ConfigurationCommandRecorder();
        var controller = CreateController(config, mediator, new Mock<IAuthenticationService>());
        var originalGeneral = config.Capture().GeneralSettings;
        await mediator.Db.Database.ExecuteSqlRawAsync(
            "CREATE TRIGGER reject_general BEFORE UPDATE ON BlogConfiguration WHEN OLD.CfgKey = 'GeneralSettings' BEGIN SELECT RAISE(ABORT, 'General save rejected'); END;",
            TestContext.Current.CancellationToken);
        var image = ImageSettings.DefaultValue with { EnableCDNRedirect = true, CDNEndpoint = "https://cdn.example.com/images" };

        Assert.IsType<NoContentResult>(await controller.Image(image));

        Assert.Same(originalGeneral, config.Capture().GeneralSettings);
        Assert.Equal(originalGeneral.ToJson(), await ReadStoredSettings(mediator, nameof(GeneralSettings)));
        Assert.Equal(image.ToJson(), await ReadStoredSettings(mediator, nameof(ImageSettings)));
        Assert.Single(mediator.Commands.OfType<CreateActivityLogCommand>());
    }

    [Fact]
    public async Task Content_WhenValidationFails_DoesNotSaveOrAudit()
    {
        var config = new BlogConfig();
        var original = config.Capture().ContentSettings;
        using var mediator = new ConfigurationCommandRecorder { ActivityLogFailure = CreateAuditFailure() };
        var controller = CreateController(config, mediator, new Mock<IAuthenticationService>());

        await Assert.ThrowsAsync<ValidationException>(() => controller.Content(new ContentSettings { PostListPageSize = 0 }));

        Assert.Same(original, config.Capture().ContentSettings);
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
        var controller = CreateController(config, mediator, new Mock<IAuthenticationService>());
        var content = new ContentSettings { DefaultEditor = EditorChoice.Markdown };

        Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => controller.Content(content)));

        Assert.Equal(content.ToJson(), config.Capture().ContentSettings.ToJson());
        Assert.Equal(content.ToJson(), await ReadStoredSettings(mediator, nameof(ContentSettings)));
        Assert.Empty(mediator.Commands.OfType<CreateActivityLogCommand>());
    }

    [Fact]
    public async Task ResetTotp_WhenSignOutFails_PropagatesFailureEvenAfterAuditFailure()
    {
        var account = CreateLocalAccount();
        var config = new BlogConfig();
        using var mediator = new ConfigurationCommandRecorder { ActivityLogFailure = CreateAuditFailure() };
        var failure = new InvalidOperationException("Sign-out failed.");
        var authentication = new Mock<IAuthenticationService>();
        authentication.Setup(p => p.SignOutAsync(It.IsAny<HttpContext>(), CookieAuthenticationDefaults.AuthenticationScheme, null))
            .ThrowsAsync(failure);
        var controller = CreateController(account, mediator, authentication);

        Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() =>
            controller.ResetLocalAccountTotp(new ResetLocalAccountTotpRequest { CurrentPassword = "Password1" })));

        Assert.False((await mediator.Accounts.ReadAsync(TestContext.Current.CancellationToken))!.IsTotpEnabled);
        var saved = await mediator.Db.OwnerAccount.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);
        Assert.Null(saved.TotpSecret);
        Assert.Single(mediator.Commands.OfType<CreateActivityLogCommand>());
        authentication.Verify(p => p.SignOutAsync(controller.HttpContext, CookieAuthenticationDefaults.AuthenticationScheme, null), Times.Once);
    }

    private static Exception CreateAuditFailure(bool canceled = false) => canceled
        ? new OperationCanceledException("Audit dispatch canceled.")
        : new InvalidOperationException("Audit dispatch unavailable.");

    private static async Task<string> ReadStoredSettings(ConfigurationCommandRecorder mediator, string key) =>
        (await mediator.Db.BlogConfiguration.AsNoTracking().SingleAsync(p => p.CfgKey == key, TestContext.Current.CancellationToken)).CfgValue;

    private static void AssertAuditFailure<T>(Mock<ILogger<T>> logger, ControllerBase controller,
        Exception failure, string eventKey, params string[] excludedValues)
    {
        var invocation = Assert.Single(logger.Invocations, p => p.Method.Name == nameof(ILogger.Log));
        Assert.Equal(LogLevel.Error, invocation.Arguments[0]);
        Assert.Null(invocation.Arguments[3]);
        var state = Assert.IsAssignableFrom<IReadOnlyList<KeyValuePair<string, object?>>>(invocation.Arguments[2]);
        Assert.Contains(state, p => p.Key == "EventKey" && Equals(p.Value, eventKey));
        Assert.Contains(state, p => p.Key == "FailureType" && Equals(p.Value, failure.GetType().Name));
        Assert.Contains(state, p => p.Key == "TraceIdentifier" && Equals(p.Value, controller.HttpContext.TraceIdentifier));
        var message = invocation.Arguments[2]?.ToString() ?? string.Empty;
        Assert.Contains("saved", message, StringComparison.OrdinalIgnoreCase);
        foreach (var value in excludedValues)
            Assert.DoesNotContain(value, message);
    }
}
