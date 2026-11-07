using Microsoft.AspNetCore.Builder;
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
        { InitStartUpResult.FailedCancellation, "Application initialization was canceled. RESTART Moonglade to retry." },
        { InitStartUpResult.UnexpectedError, "Unexpected application initialization error, please check error log, then RESTART Moonglade manually." }
    };

    /// <summary>
    /// Runs initialization before the host starts and stops startup on failure.
    /// </summary>
    /// <param name="app">The web application.</param>
    public static async Task InitStartUp(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var initializer = scope.ServiceProvider.GetRequiredService<StartUpInitializer>();
        var result = await initializer.InitStartUpAsync();

        if (result != InitStartUpResult.Success)
        {
            var message = ErrorMessages.TryGetValue(result, out var msg)
                ? msg
                : $"Application initialization failed ({result}), please check error log, then RESTART Moonglade manually.";

            app.Logger.LogCritical("{ErrorMessage}", message);

            throw new InvalidOperationException(message);
        }
    }
}
