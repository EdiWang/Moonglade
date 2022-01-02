using Moonglade.Data.Setup;

namespace Moonglade.Web;

public static class WebApplicationExtensions
{
    public static async Task InitStartUp(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var env = services.GetRequiredService<IWebHostEnvironment>();

        //var dbConnection = services.GetRequiredService<IDbConnection>();
        var setupRunner = services.GetRequiredService<ISetupRunner>();

        try
        {
            if (!setupRunner.TestDatabaseConnection()) return;
        }
        catch (Exception e)
        {
            app.Logger.LogCritical(e, e.Message);
            return;
        }

        if (setupRunner.IsFirstRun())
        {
            try
            {
                app.Logger.LogInformation("Initializing first run configuration...");
                setupRunner.InitFirstRun();
                app.Logger.LogInformation("Database setup successfully.");
            }
            catch (Exception e)
            {
                app.Logger.LogCritical(e, e.Message);
            }
        }

        try
        {
            var mediator = services.GetRequiredService<IMediator>();
            var iconData = await mediator.Send(new GetAssetDataQuery(AssetId.SiteIconBase64));
            MemoryStreamIconGenerator.GenerateIcons(iconData, env.WebRootPath, app.Logger);
        }
        catch (Exception e)
        {
            // Non critical error, just log, do not block application start
            app.Logger.LogError(e, e.Message);
        }
    }
}