using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Moonglade.Setup;

/// <summary>
/// Extension methods for initializing Moonglade on application startup.
/// </summary>
public static class WebApplicationExtensions
{
    private static readonly Dictionary<InitStartUpResult, string> ErrorMessages = new()
    {
        { InitStartUpResult.FailedCreateDatabase, "Database connection test failed, please check your connection string and firewall settings, then RESTART Moonglade manually." },
        { InitStartUpResult.FailedSeedingDatabase, "Error seeding database, please check error log, then RESTART Moonglade manually." },
        { InitStartUpResult.FailedInitBlogConfig, "Error initializing blog configuration, please check error log, then RESTART Moonglade manually." },
        { InitStartUpResult.FailedDatabaseMigration, "Error migrating database, please check error log, then RESTART Moonglade manually." }
    };

    /// <summary>
    /// Runs Moonglade startup initialization and handles failures gracefully.
    /// </summary>
    /// <param name="app">The web application.</param>
    public static async Task InitStartUp(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var initializer = scope.ServiceProvider.GetRequiredService<IStartUpInitializer>();
        var result = await initializer.InitStartUpAsync();

        if (result != InitStartUpResult.Success)
        {
            var message = ErrorMessages.TryGetValue(result, out var msg)
                ? msg
                : $"Unknown startup failure: {result}";

            app.Logger.LogCritical("{ErrorMessage}", message);

            await FailFast(app, message);
        }
    }

    private static async Task FailFast(WebApplication app, string detail)
    {
        // Remove all endpoints, only expose the error
        app.Map("/", () => Results.Problem(
            detail: detail,
            statusCode: 500
        ));

        await app.RunAsync();
    }
}
