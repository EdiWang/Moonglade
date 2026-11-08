using LiteBus.Events.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Features.Post;
using Moonglade.Web.Extensions;
using Moq;

namespace Moonglade.Web.Tests.Hosting;

public class ScheduledPublicationRegistrationTests
{
    [Fact]
    public async Task WebServices_ActualLiteBusDiscovery_DispatchesScheduledPublicationToSystemWriter()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(ct);
        var options = new DbContextOptionsBuilder<BlogDbContext>().UseSqlite(connection).Options;
        var imageRoot = Path.Combine(Path.GetTempPath(), "moonglade-registration-tests", Guid.NewGuid().ToString("N"));
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DatabaseProvider"] = "PostgreSql",
            ["ConnectionStrings:MoongladeDatabase"] = "Host=localhost;Database=not-opened-b4",
            ["ImageStorage:FileSystemPath"] = Path.Combine(imageRoot, "primary"),
            ["ImageStorage:OriginalFileSystemPath"] = Path.Combine(imageRoot, "original")
        }).Build();
        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(value => value.EnvironmentName).Returns("Testing");
        environment.SetupGet(value => value.ApplicationName).Returns(typeof(Program).Assembly.GetName().Name!);
        environment.SetupGet(value => value.WebRootPath).Returns(imageRoot);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(config);
        services.AddSingleton<IHostEnvironment>(environment.Object);
        services.AddSingleton(environment.Object);
        services.AddMoongladeWebServices(config, []);
        // Replace only the database boundary; production assembly discovery remains untouched.
        services.RemoveAll<BlogDbContext>();
        services.AddScoped(_ => new BlogDbContext(options));
        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();
        await db.Database.EnsureCreatedAsync(ct);
        var postId = Guid.NewGuid();
        var occurrence = new DateTime(2026, 11, 1, 1, 2, 3, DateTimeKind.Utc);

        await scope.ServiceProvider.GetRequiredService<IEventMediator>()
            .PublishAsync(new ScheduledPostsPublishedEvent(Array.AsReadOnly(new[]
            {
                new ScheduledPostPublication(postId, "Discovered captured title", occurrence)
            })), cancellationToken: ct);

        var row = await db.ActivityLog.AsNoTracking().SingleAsync(ct);
        Assert.Equal("post.automatically-published", row.EventKey);
        Assert.Equal(ActivityActorKind.System, row.ActorKind);
        Assert.Equal(postId.ToString(), row.TargetId);
        Assert.Equal("Discovered captured title", row.TargetDisplayName);
        Assert.Equal(occurrence, row.EventTimeUtc);
        Assert.Null(row.ActorDisplayName);
        Assert.Null(row.IpAddress);
        Assert.Null(row.UserAgent);
    }
}
