using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moonglade.ImageStorage;
using Moonglade.Setup;
using Moonglade.Syndication;
using Moonglade.Web.Extensions;
using Moonglade.Web.Services;
using Moq;

namespace Moonglade.Web.Tests.Hosting;

public class ConcreteServiceRegistrationTests
{
    [Fact]
    public void WebServices_ResolveConcreteServicesWithTheirOriginalLifetimes()
    {
        var imageRoot = Path.Combine(Path.GetTempPath(), "moonglade-registration-tests", Guid.NewGuid().ToString("N"));
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DatabaseProvider"] = "PostgreSql",
            ["ConnectionStrings:MoongladeDatabase"] = "Host=localhost;Database=registration-test",
            ["ImageStorage:FileSystemPath"] = Path.Combine(imageRoot, "primary"),
            ["ImageStorage:OriginalFileSystemPath"] = Path.Combine(imageRoot, "original")
        }).Build();
        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(value => value.EnvironmentName).Returns("Development");
        environment.SetupGet(value => value.ApplicationName).Returns(typeof(Program).Assembly.GetName().Name!);
        environment.SetupGet(value => value.WebRootPath).Returns(imageRoot);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<IHostEnvironment>(environment.Object);
        services.AddSingleton(environment.Object);
        services.AddMoongladeWebServices(configuration, []);
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        var context = new DefaultHttpContext();
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("blog.example.com");
        provider.GetRequiredService<IHttpContextAccessor>().HttpContext = context;
        using var first = provider.CreateScope();
        using var second = provider.CreateScope();

        foreach (var type in new[] { typeof(ImageUploadValidator), typeof(CommentSubmissionGuard) })
        {
            Assert.Same(first.ServiceProvider.GetRequiredService(type), second.ServiceProvider.GetRequiredService(type));
        }

        foreach (var type in new[]
        {
            typeof(StartUpInitializer), typeof(ConfigInitializer),
            typeof(SyndicationDataSource), typeof(DatedGuidFileNameGenerator)
        })
        {
            var service = first.ServiceProvider.GetRequiredService(type);
            Assert.Same(service, first.ServiceProvider.GetRequiredService(type));
            Assert.NotSame(service, second.ServiceProvider.GetRequiredService(type));
        }

        Assert.NotSame(first.ServiceProvider.GetRequiredService<SiteIconBuilder>(), first.ServiceProvider.GetRequiredService<SiteIconBuilder>());
    }
}
