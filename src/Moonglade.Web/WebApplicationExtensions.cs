using Moonglade.Data.MySql;
using Moonglade.Data.Setup;
using Moonglade.Data.SqlServer;

namespace Moonglade.Web;

public static class WebApplicationExtensions
{
    public static async Task<StartupInitResult> InitStartUp(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var env = services.GetRequiredService<IWebHostEnvironment>();

        var setupRunner = services.GetRequiredService<ISetupRunner>();

        BlogDbContext context;
        switch (app.Configuration.GetConnectionString("DatabaseType").ToLower())
        {
            case "mysql":
                context = services.GetRequiredService<BlogMySqlDbContext>();
                break;
            case "sqlserver":
            default:
                context = services.GetRequiredService<BlogSqlServerDbContext>();
                break;
        }

        bool canConnect = await context.Database.CanConnectAsync();
        if (!canConnect) return StartupInitResult.DatabaseConnectionFail;

        if (setupRunner.IsFirstRun())
        {
            try
            {
                app.Logger.LogInformation("Initializing first run configuration...");

                await context.Database.EnsureCreatedAsync();
                await context.ClearAllData();

                await Seed.SeedAsync(context, app.Logger);

                app.Logger.LogInformation("Database setup successfully.");
            }
            catch (Exception e)
            {
                app.Logger.LogCritical(e, e.Message);
                return StartupInitResult.DatabaseSetupFail;
            }
        }

        var mediator = services.GetRequiredService<IMediator>();

        // load configurations into singleton
        var config = await mediator.Send(new GetAllConfigurationsQuery());
        var bc = app.Services.GetRequiredService<IBlogConfig>();
        bc.LoadFromConfig(config);

        try
        {
            var iconData = await mediator.Send(new GetAssetDataQuery(AssetId.SiteIconBase64));
            MemoryStreamIconGenerator.GenerateIcons(iconData, env.WebRootPath, app.Logger);
        }
        catch (Exception e)
        {
            // Non critical error, just log, do not block application start
            app.Logger.LogError(e, e.Message);
        }

        return StartupInitResult.None;
    }
}

public enum StartupInitResult
{
    None = 0,
    DatabaseConnectionFail = 1,
    DatabaseSetupFail = 2
}