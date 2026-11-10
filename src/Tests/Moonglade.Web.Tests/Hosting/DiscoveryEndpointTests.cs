using Edi.CacheAside.InMemory;
using LiteBus.Commands.Abstractions;
using LiteBus.Queries.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moonglade.Configuration;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Web.Extensions;
using Moonglade.Web.Handlers;
using Moonglade.Widgets;
using Moq;
using System.Net;

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
}
