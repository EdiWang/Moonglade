using Edi.CacheAside.InMemory;
using LiteBus.Commands.Abstractions;
using LiteBus.Events.Abstractions;
using LiteBus.Queries.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moonglade.Auth;
using Moonglade.Configuration;
using Moonglade.Email.Core;
using Moonglade.ImageStorage;
using Moonglade.Web.Controllers;
using Moonglade.Web.Tests.TestSupport;
using Moq;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Moonglade.Web.Tests.Controllers;

public class SettingsValidationHttpTests
{
    [Theory]
    [InlineData("content", """{"postListPageSize":0}""", "PostListPageSize")]
    [InlineData("subscription", """{"feedItemCount":-1}""", "FeedItemCount")]
    [InlineData("advanced", """{"headScripts":"<p>private-marker</p>"}""", "HeadScripts")]
    [InlineData("appearance", """{"enableCustomCss":true,"cssCode":""}""", "CssCode")]
    [InlineData("custom-menu", """{"isEnabled":true,"menuJson":"[null]"}""", "Menus[0]")]
    [InlineData("custom-menu", """{"isEnabled":true,"menuJson":"null"}""", "Menus")]
    public async Task InvalidSettings_ReturnValidationProblemAndPreserveRuntime(string route, string json, string field)
    {
        using var store = new ConfigurationCommandRecorder();
        var config = Defaults();
        var original = config.CustomMenuSettings;
        await using var app = await CreateApp(store, config);
        using var client = app.GetTestClient();

        using var response = await client.PostAsync($"/api/settings/{route}", new StringContent(json, Encoding.UTF8, "application/json"),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var problem = JsonDocument.Parse(body);
        Assert.True(problem.RootElement.GetProperty("errors").TryGetProperty(field, out _));
        Assert.DoesNotContain("private-marker", body);
        Assert.Same(original, config.CustomMenuSettings);
        Assert.Empty(store.Commands.OfType<Moonglade.ActivityLog.CreateActivityLogCommand>());
    }

    [Fact]
    public async Task TooManyParsedMenus_AreRejectedAtTheSharedWriteBoundary()
    {
        using var store = new ConfigurationCommandRecorder();
        var config = Defaults();
        await using var app = await CreateApp(store, config);
        using var client = app.GetTestClient();
        var request = new CustomMenuSettingsJsonModel
        {
            IsEnabled = true,
            MenuJson = Enumerable.Range(0, 6).Select(_ => new Menu { Title = "Link" }).ToArray().ToJson()
        };

        using var response = await client.PostAsJsonAsync("/api/settings/custom-menu", request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Single(config.CustomMenuSettings.Menus);
        Assert.Empty(store.Commands.OfType<Moonglade.ActivityLog.CreateActivityLogCommand>());
    }

    [Fact]
    public async Task ApprovedBlankSenderDisabledMenuAndLongGeneratedAvatar_CanBeSaved()
    {
        using var store = new ConfigurationCommandRecorder();
        var config = Defaults();
        var avatar = "https://cdn.example.com/" + new string('a', 300);
        config.GeneralSettings.AvatarUrl = avatar;
        await using var app = await CreateApp(store, config);
        using var client = app.GetTestClient();
        using var sender = await client.PostAsJsonAsync("/api/settings/notification", new NotificationSettings { EmailDisplayName = null },
            TestContext.Current.CancellationToken);
        using var menu = await client.PostAsJsonAsync("/api/settings/custom-menu", new CustomMenuSettingsJsonModel { IsEnabled = false },
            TestContext.Current.CancellationToken);
        using var general = await client.PostAsJsonAsync("/api/settings/general", GeneralSettings.DefaultValue, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, sender.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, menu.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, general.StatusCode);
        Assert.Null(config.NotificationSettings.EmailDisplayName);
        Assert.False(config.CustomMenuSettings.IsEnabled);
        Assert.Null(config.CustomMenuSettings.Menus);
        Assert.Equal(avatar, config.GeneralSettings.AvatarUrl);
    }

    private static BlogConfig Defaults()
    {
        var config = new BlogConfig();
        config.LoadFromConfig(new Dictionary<string, string>(), true);
        return config;
    }

    private static async Task<WebApplication> CreateApp(ConfigurationCommandRecorder store, BlogConfig config)
    {
        store.Initialize(config);
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Services.AddSingleton<IBlogConfig>(config);
        builder.Services.AddSingleton<ICommandMediator>(store);
        builder.Services.AddSingleton(Mock.Of<IQueryMediator>());
        builder.Services.AddSingleton(Mock.Of<IEventMediator>());
        builder.Services.AddSingleton(Mock.Of<IBlogImageStorage>());
        builder.Services.AddSingleton(new EmailCapabilityStatusEvaluator(new EmailServiceOptionsValidator(), new EmailOutboxWorkerOptionsValidator())
            .Evaluate(new EmailServiceOptions(), new EmailOutboxWorkerOptions()));
        builder.Services.AddInMemoryCacheAside();
        builder.Services.AddSingleton<PostCacheInvalidator>();
        builder.Services.AddControllers().AddApplicationPart(typeof(SettingsController).Assembly)
            .ConfigureApplicationPartManager(manager => manager.FeatureProviders.Add(new SettingsOnlyFeatureProvider()));
        builder.Services.AddAuthorization(options => options.AddPolicy(BlogAuthSchemas.AdministratorPolicy,
            policy => policy.RequireAssertion(_ => true)));
        var app = builder.Build();
        app.UseAuthorization();
        app.MapControllers();
        await app.StartAsync(TestContext.Current.CancellationToken);
        return app;
    }

    private sealed class SettingsOnlyFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
    {
        public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
        {
            foreach (var controller in feature.Controllers.Where(type => type.AsType() != typeof(SettingsController)).ToArray())
                feature.Controllers.Remove(controller);
        }
    }
}
