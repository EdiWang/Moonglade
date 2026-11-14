using Edi.CacheAside.InMemory;
using LiteBus.Commands.Abstractions;
using LiteBus.Queries.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moonglade.Auth;
using Moonglade.Configuration;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Web.Extensions;
using Moonglade.Web.Handlers;
using Moonglade.Widgets;
using Moq;
using System.Net;
using System.Xml.Linq;

namespace Moonglade.Web.Tests.Hosting;

public class DiscoveryEndpointTests
{
    [Theory]
    [InlineData("/foaf.xml", "application/rdf+xml", true)]
    [InlineData("/sitemap.xml", "text/xml", false)]
    public async Task DiscoveryEndpoint_UsesCurrentSettingsWithoutRestart(string path, string mediaType, bool enableFoaf)
    {
        var ct = TestContext.Current.CancellationToken;
        var disabled = new BlogSettingsSnapshot
        {
            AdvancedSettings = new AdvancedSettings { EnableFoaf = false },
            SEOSettings = new SEOSettings { EnableSiteMap = false }
        };
        var config = new BlogConfig(disabled);

        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(ct);

        var queries = new Mock<IQueryMediator>();
        queries.Setup(q => q.QueryAsync(It.IsAny<ListWidgetsQuery>(), It.IsAny<QueryMediationSettings>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WidgetEntity>());
        var commands = new Mock<ICommandMediator>();
        commands.Setup(c => c.SendAsync(It.IsAny<WriteFoafCommand>(), It.IsAny<CommandMediationSettings>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("<rdf />");

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ApplicationName = typeof(Program).Assembly.GetName().Name
        });
        builder.WebHost.UseTestServer();
        builder.Services.AddControllers();
        builder.Services.AddRazorPages();
        builder.Services.AddHealthChecks();
        builder.Services.AddSingleton<IBlogConfig>(config);
        builder.Services.AddSingleton<IOwnerProfileReader>(new Moonglade.Web.Tests.TestSupport.TestOwnerProfileReader());
        builder.Services.AddSingleton<PostCacheInvalidator>();
        builder.Services.AddInMemoryCacheAside();
        builder.Services.AddSingleton(queries.Object);
        builder.Services.AddSingleton(commands.Object);
        builder.Services.AddDbContext<BlogDbContext>(options => options.UseSqlite(connection));

        await using var app = builder.Build();
        await using (var scope = app.Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<BlogDbContext>().Database.EnsureCreatedAsync(ct);
        }

        app.MapMoongladeEndpoints();
        await app.StartAsync(ct);
        using var client = app.GetTestClient();

        using var before = await client.GetAsync(path, ct);
        Assert.Equal(HttpStatusCode.NotFound, before.StatusCode);

        config.Publish(disabled with
        {
            AdvancedSettings = new AdvancedSettings { EnableFoaf = enableFoaf },
            SEOSettings = new SEOSettings { EnableSiteMap = !enableFoaf }
        });
        using var enabled = await client.GetAsync(path, ct);
        Assert.Equal(HttpStatusCode.OK, enabled.StatusCode);
        Assert.Equal(mediaType, enabled.Content.Headers.ContentType?.MediaType);

        config.Publish(disabled);
        using var after = await client.GetAsync(path, ct);
        Assert.Equal(HttpStatusCode.NotFound, after.StatusCode);
    }

    [Fact]
    public async Task Foaf_ProfileMissingOrConfigured_UsesCurrentMailboxHashAndRetainsOwnerIdentity()
    {
        var ct = TestContext.Current.CancellationToken;
        var profiles = new Moonglade.Web.Tests.TestSupport.TestOwnerProfileReader { Owner = null, Presentation = OwnerPresentation.Default with { Name = "Protocol owner" } };
        var config = new BlogConfig(BlogConfig.Parse(new Dictionary<string, string>()).Snapshot);
        config.Publish(config.Capture() with
        {
            AdvancedSettings = config.Capture().AdvancedSettings with { EnableFoaf = true }
        });
        var queries = new Mock<IQueryMediator>();
        var widgetsStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var widgetsResume = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var pauseWidgets = false;
        queries.Setup(query => query.QueryAsync(It.IsAny<ListWidgetsQuery>(), It.IsAny<QueryMediationSettings>(), It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                if (pauseWidgets)
                {
                    pauseWidgets = false;
                    widgetsStarted.TrySetResult();
                    await widgetsResume.Task.WaitAsync(ct);
                }
                return new List<WidgetEntity>();
            });
        var commands = new Mock<ICommandMediator>();
        commands.Setup(command => command.SendAsync(It.IsAny<WriteFoafCommand>(), It.IsAny<CommandMediationSettings>(), It.IsAny<CancellationToken>()))
            .Returns((WriteFoafCommand command, CommandMediationSettings _, CancellationToken token) =>
                new WriteFoafCommandHandler().HandleAsync(command, token));
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddRouting();
        builder.Services.AddSingleton<IBlogConfig>(config);
        builder.Services.AddSingleton<IOwnerProfileReader>(profiles);
        builder.Services.AddSingleton(queries.Object);
        builder.Services.AddSingleton(commands.Object);
        await using var app = builder.Build();
        app.MapGet("/foaf.xml", FoafMapHandler.Handler);
        await app.StartAsync(ct);
        using var client = app.GetTestClient();
        XNamespace foaf = "http://xmlns.com/foaf/0.1/";

        var missing = XDocument.Parse(await client.GetStringAsync("/foaf.xml", ct));
        Assert.Equal("Protocol owner", Assert.Single(missing.Descendants(foaf + "name")).Value);
        Assert.Empty(missing.Descendants(foaf + "mbox_sha1sum"));
        profiles.Owner = new OwnerProfile(Guid.NewGuid(), "public@example.test");
        pauseWidgets = true;
        var pending = client.GetStringAsync("/foaf.xml", ct);
        await widgetsStarted.Task.WaitAsync(TimeSpan.FromSeconds(10), ct);
        profiles.Owner = profiles.Owner with { Email = "next@example.test" };
        profiles.Presentation = profiles.Presentation with { Name = "Next owner" };
        widgetsResume.TrySetResult();
        var configured = XDocument.Parse(await pending);

        Assert.Equal("Protocol owner", Assert.Single(configured.Descendants(foaf + "name")).Value);
        var hash = Convert.ToHexString(System.Security.Cryptography.SHA1.HashData(
            System.Text.Encoding.UTF8.GetBytes("mailto:public@example.test"))).ToLowerInvariant();
        Assert.Equal(hash, Assert.Single(configured.Descendants(foaf + "mbox_sha1sum")).Value);
        Assert.Equal(2, profiles.Reads);
        var next = XDocument.Parse(await client.GetStringAsync("/foaf.xml", ct));
        Assert.Equal("Next owner", Assert.Single(next.Descendants(foaf + "name")).Value);
        var nextHash = Convert.ToHexString(System.Security.Cryptography.SHA1.HashData(
            System.Text.Encoding.UTF8.GetBytes("mailto:next@example.test"))).ToLowerInvariant();
        Assert.Equal(nextHash, Assert.Single(next.Descendants(foaf + "mbox_sha1sum")).Value);
        config.Publish(config.Capture() with { AdvancedSettings = config.Capture().AdvancedSettings with { EnableFoaf = false } });
        using var disabled = await client.GetAsync("/foaf.xml", ct);
        Assert.Equal(HttpStatusCode.NotFound, disabled.StatusCode);
        Assert.Equal(3, profiles.Reads);
    }
}
