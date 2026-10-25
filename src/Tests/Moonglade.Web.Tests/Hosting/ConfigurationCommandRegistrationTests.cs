using LiteBus.Commands;
using LiteBus.Commands.Abstractions;
using LiteBus.Extensions.Microsoft.DependencyInjection;
using LiteBus.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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
        services.AddSingleton<IBlogConfig>(config);
        services.AddLiteBus(bus =>
        {
            bus.AddMessaging(_ => { });
            bus.AddCommands(module => module.RegisterFromAssembly(typeof(UpdateConfigurationCommand).Assembly));
        });
        using var provider = services.BuildServiceProvider();

        await provider.GetRequiredService<ICommandMediator>().SendAsync(UpdateConfigurationCommand.Create(
            new GeneralSettings { SiteTitle = "Saved through LiteBus" }), TestContext.Current.CancellationToken);

        Assert.Equal("Saved through LiteBus", config.GeneralSettings.SiteTitle);
        Assert.Equal(config.GeneralSettings.ToJson(), (await store.Db.BlogConfiguration.SingleAsync(
            p => p.CfgKey == nameof(GeneralSettings), TestContext.Current.CancellationToken)).CfgValue);
    }
}
