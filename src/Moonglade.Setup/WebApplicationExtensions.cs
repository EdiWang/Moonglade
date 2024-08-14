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

        switch (result)
        {
            case InitStartUpResult.FailedCreateDatabase:
                await FailFast("Database connection test failed, please check your connection string and firewall settings, then RESTART Moonglade manually.");
                break;
            case InitStartUpResult.FailedSeedingDatabase:
                await FailFast("Error seeding database, please check error log, then RESTART Moonglade manually.");
                break;
            case InitStartUpResult.FailedInitBlogConfig:
                await FailFast("Error initializing blog configuration, please check error log, then RESTART Moonglade manually.");
                break;
            case InitStartUpResult.FailedDatabaseMigration:
                await FailFast("Error migrating database, please check error log, then RESTART Moonglade manually.");
                break;
            case InitStartUpResult.Success:
                break;
        }

        return;

        async Task FailFast(string messsage)
        {
            app.MapGet("/", () => Results.Problem(
                detail: messsage,
                statusCode: 500
            ));
            await app.RunAsync();
        }
    }
}