using LiteBus.Commands;
using LiteBus.Commands.Abstractions;
using LiteBus.Events;
using Edi.CacheAside.InMemory;
using Moonglade.BackgroundServices;
using Moonglade.Web.Handlers;
using LiteBus.Extensions.Microsoft.DependencyInjection;
using LiteBus.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Moonglade.Auth;
using Moonglade.Configuration;
using Moonglade.Web.Tests.TestSupport;

namespace Moonglade.Web.Tests.Hosting;

public class ConfigurationCommandRegistrationTests
{
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
        services.AddSingleton<IBlogConfig>(config);
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
                module.RegisterFromAssembly(typeof(UpdateLocalAccountCommand).Assembly);
            });
            bus.AddEvents(module => module.RegisterFromAssembly(typeof(ConfigurationChangedEventHandler).Assembly));
        });
        using var provider = services.BuildServiceProvider();
        var feeds = provider.GetRequiredService<PostCacheInvalidator>();
        var previousFeedKey = feeds.GetFeedCacheKey("rss");

        var settings = GeneralSettings.DefaultValue;
        settings.SiteTitle = "Saved via bus";
        await provider.GetRequiredService<ICommandMediator>().SendAsync(UpdateConfigurationCommand.Create(settings), TestContext.Current.CancellationToken);

        Assert.Equal("Saved via bus", config.GeneralSettings.SiteTitle);
        Assert.NotEqual(previousFeedKey, feeds.GetFeedCacheKey("rss"));
        Assert.Equal(config.GeneralSettings.ToJson(), (await store.Db.BlogConfiguration.SingleAsync(
            p => p.CfgKey == nameof(GeneralSettings), TestContext.Current.CancellationToken)).CfgValue);

        var gate = Assert.Single(provider.GetServices<ConfigurationWriteGate>());
        Assert.Same(gate, provider.GetRequiredService<ConfigurationWriteGate>());
        Assert.Same(provider.GetRequiredService<LocalAccountState>(), provider.GetRequiredService<ILocalAccountReader>());
        var account = LocalAccountSettings.DefaultValue;
        account.Username = "saved-via-auth-bus";
        var feedKeyBeforeAccountSave = feeds.GetFeedCacheKey("rss");

        await provider.GetRequiredService<ICommandMediator>().SendAsync(new UpdateLocalAccountCommand(account), TestContext.Current.CancellationToken);

        Assert.Equal(account.ToJson(), provider.GetRequiredService<ILocalAccountReader>().Read().ToJson());
        Assert.Equal(account.ToJson(), (await store.Db.BlogConfiguration.SingleAsync(
            p => p.CfgKey == nameof(LocalAccountSettings), TestContext.Current.CancellationToken)).CfgValue);
        Assert.Equal(feedKeyBeforeAccountSave, feeds.GetFeedCacheKey("rss"));
    }
}
