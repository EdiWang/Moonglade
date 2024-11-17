using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Moonglade.Setup;

public static class WebApplicationExtensions
{
    public static async Task InitStartUp(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;

        var initializer = services.GetRequiredService<IStartUpInitializer>();
        var result = await initializer.InitStartUp();

        var errorMessages = new Dictionary<InitStartUpResult, string>
        {
            { InitStartUpResult.FailedCreateDatabase, "Database connection test failed, please check your connection string and firewall settings, then RESTART Moonglade manually." },
            { InitStartUpResult.FailedSeedingDatabase, "Error seeding database, please check error log, then RESTART Moonglade manually." },
            { InitStartUpResult.FailedInitBlogConfig, "Error initializing blog configuration, please check error log, then RESTART Moonglade manually." },
            { InitStartUpResult.FailedDatabaseMigration, "Error migrating database, please check error log, then RESTART Moonglade manually." }
        };

        if (errorMessages.TryGetValue(result, out var message))
        {
            await FailFast(message);
        }

        async Task FailFast(string detail)
        {
            app.MapGet("/", () => Results.Problem(
                detail: detail,
                statusCode: 500
            ));
            await app.RunAsync();
        }
    }
}