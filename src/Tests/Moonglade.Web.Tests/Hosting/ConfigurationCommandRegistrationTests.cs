using Edi.CacheAside.InMemory;
using LiteBus.Commands;
using LiteBus.Commands.Abstractions;
using LiteBus.Events;
using LiteBus.Extensions.Microsoft.DependencyInjection;
using LiteBus.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moonglade.Auth;
using Moonglade.BackgroundServices;
using Moonglade.Configuration;
using Moonglade.Web.Handlers;
using Moonglade.Web.Tests.TestSupport;

namespace Moonglade.Web.Tests.Hosting;

public class ConfigurationCommandRegistrationTests
{
    [Fact]
    public async Task AuthRegistration_ResolvesIndependentAvatarStore()
    {
        using var store = new ConfigurationCommandRecorder();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(store.Db);
        services.AddBlogAuthentication(new ConfigurationBuilder().Build());
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var avatars = scope.ServiceProvider.GetRequiredService<OwnerAvatarStore>();
        var ct = TestContext.Current.CancellationToken;

        await avatars.SetGravatarAsync(true, ct);

        Assert.True(await avatars.UsesGravatarAsync(ct));
        Assert.False(await store.Db.OwnerAccount.AnyAsync(ct));
        Assert.False(await store.Db.BlogConfiguration.AnyAsync(ct));
    }

    [Fact]
    public async Task AssemblyRegistration_ResolvesAndDispatchesConfigurationUpdate()
    {
        var config = new BlogConfig();
        using var store = new ConfigurationCommandRecorder();
        store.Initialize(config);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(store.Db);
        services.AddBlogConfig();
        services.AddBlogAuthentication(new ConfigurationBuilder().Build());
        services.AddSingleton(config);
        services.AddSingleton<IBlogConfig>(sp => sp.GetRequiredService<BlogConfig>());
        services.AddInMemoryCacheAside();
        services.AddSingleton<PostCacheInvalidator>();
        services.AddSingleton<ThemeCacheInvalidator>();
        services.AddSingleton<ScheduledPublishWakeUp>();
        services.AddLiteBus(bus =>
        {
            bus.AddMessaging(_ => { });
            bus.AddCommands(module =>
            {
                module.RegisterFromAssembly(typeof(UpdateConfigurationCommand).Assembly);
            });
            bus.AddEvents(module => module.RegisterFromAssembly(typeof(ConfigurationChangedEventHandler).Assembly));
        });
        using var provider = services.BuildServiceProvider();
        var feeds = provider.GetRequiredService<PostCacheInvalidator>();
        var previousFeedKey = feeds.GetFeedCacheKey("rss");

        var settings = GeneralSettings.DefaultValue;
        settings = settings with { SiteTitle = "Saved via bus" };
        var result = await provider.GetRequiredService<ICommandMediator>().SendAsync(UpdateConfigurationCommand.Create(settings), TestContext.Current.CancellationToken);

        Assert.Equal(typeof(GeneralSettings), result.SettingsType);
        Assert.True(result.HasEffectiveChanges);
        Assert.Equal(new SettingValueChange("SiteTitle", null, "Saved via bus"),
            Assert.Single(result.Changes, change => change.Key == "SiteTitle"));
        Assert.Equal("Saved via bus", config.Capture().GeneralSettings.SiteTitle);
        Assert.NotEqual(previousFeedKey, feeds.GetFeedCacheKey("rss"));
        Assert.Equal(config.Capture().GeneralSettings.ToJson(), (await store.Db.BlogConfiguration.SingleAsync(
            p => p.CfgKey == nameof(GeneralSettings), TestContext.Current.CancellationToken)).CfgValue);
        var patch = PatchConfigurationCommand.Create<GeneralSettings>(static value => value with { },
            System.Text.Json.JsonSerializer.SerializeToElement(new Dictionary<string, string> { ["SiteTitle"] = "Patched via bus" }), "SiteTitle");
        var patched = await provider.GetRequiredService<ICommandMediator>().SendAsync(patch, TestContext.Current.CancellationToken);
        Assert.Equal(new SettingValueChange("SiteTitle", "Saved via bus", "Patched via bus"), Assert.Single(patched.Changes));
        Assert.Equal("Patched via bus", config.Capture().GeneralSettings.SiteTitle);

        var gate = Assert.Single(provider.GetServices<ConfigurationWriteGate>());
        Assert.Same(gate, provider.GetRequiredService<ConfigurationWriteGate>());
        var feedKeyBeforeAccountSave = feeds.GetFeedCacheKey("rss");
        using var scope = provider.CreateScope();
        var accounts = scope.ServiceProvider.GetRequiredService<OwnerAccountStore>();
        var disabledTotpAccounts = new OwnerAccountStore(store.Db, scope.ServiceProvider.GetRequiredService<ILocalAccountTotpService>(),
            Microsoft.Extensions.Options.Options.Create(new AuthenticationSettings
            { Totp = new TotpAuthenticationSettings { Required = false } }), TimeProvider.System,
            scope.ServiceProvider.GetRequiredService<LiteBus.Events.Abstractions.IEventMediator>());
        var pending = await disabledTotpAccounts.PrepareLocalSetupAsync("owner@example.test", "Synthetic123!", TestContext.Current.CancellationToken);
        var account = await disabledTotpAccounts.CompleteLocalSetupAsync(pending, null, TestContext.Current.CancellationToken);
        Assert.Equal(account, await accounts.AuthenticateLocalAsync(account.Email, "Synthetic123!", TestContext.Current.CancellationToken));
        Assert.Equal(new OwnerProfile(account.Id, account.Email),
            await scope.ServiceProvider.GetRequiredService<IOwnerProfileReader>().ReadProfileAsync(TestContext.Current.CancellationToken));
        Assert.NotEqual(feedKeyBeforeAccountSave, feeds.GetFeedCacheKey("rss"));
    }
}
