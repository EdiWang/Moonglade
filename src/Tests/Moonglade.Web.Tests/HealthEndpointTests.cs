using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moonglade.Web.Extensions;
using System.Net;
using System.Text.Json;

namespace Moonglade.Web.Tests;

public class HealthEndpointTests
{
    [Fact]
    public async Task Health_WhenReadinessCheckFails_ReturnsLivenessOnly()
    {
        using var app = await CreateTestApp();
        using var client = app.GetTestClient();

        var response = await client.GetAsync("/health", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var document = JsonDocument.Parse(body);
        Assert.Equal("healthy", document.RootElement.GetProperty("status").GetString());
        Assert.Equal(["self"], GetCheckNames(document));
    }

    [Fact]
    public async Task HealthReady_WhenReadinessCheckFails_ReturnsDatabaseReadiness()
    {
        using var app = await CreateTestApp();
        using var client = app.GetTestClient();

        var response = await client.GetAsync("/health/ready", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var document = JsonDocument.Parse(body);
        Assert.Equal("unhealthy", document.RootElement.GetProperty("status").GetString());
        Assert.Equal(["database"], GetCheckNames(document));
    }

    private static async Task<WebApplication> CreateTestApp()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Development"
        });

        builder.WebHost.UseTestServer();
        builder.Services.AddRouting();
        builder.Services.AddHealthChecks()
            .AddCheck(
                "self",
                () => HealthCheckResult.Healthy("Application is running"),
                tags: [MoongladeHealthCheckOptions.LivenessTag])
            .AddCheck(
                "database",
                () => HealthCheckResult.Unhealthy("Database unavailable"),
                tags: [MoongladeHealthCheckOptions.ReadinessTag]);

        var app = builder.Build();
        app.MapHealthChecks("/health", MoongladeHealthCheckOptions.CreateLivenessOptions());
        app.MapHealthChecks("/health/ready", MoongladeHealthCheckOptions.CreateReadinessOptions());

        await app.StartAsync(TestContext.Current.CancellationToken);
        return app;
    }

    private static string[] GetCheckNames(JsonDocument document) =>
    [
        .. document.RootElement
            .GetProperty("checks")
            .EnumerateObject()
            .Select(check => check.Name)
    ];
}
