using Moonglade.Data.Setup;
using System.Data;

namespace Moonglade.Web;

public static class WebApplicationExtensions
{
    public static async Task InitStartUp(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var env = services.GetRequiredService<IWebHostEnvironment>();
        var mediator = services.GetRequiredService<IMediator>();

        var dbConnection = services.GetRequiredService<IDbConnection>();
        var setupHelper = new SetupRunner(dbConnection);
        try
        {
            if (!setupHelper.TestDatabaseConnection()) return;
        }
        catch (Exception e)
        {
            app.Logger.LogCritical(e, e.Message);
            return;
        }

        if (setupHelper.IsFirstRun())
        {
            try
            {
                app.Logger.LogInformation("Initializing first run configuration...");
                setupHelper.InitFirstRun();
                app.Logger.LogInformation("Database setup successfully.");
            }
            catch (Exception e)
            {
                app.Logger.LogCritical(e, e.Message);
            }
        }
        else
        {
            // Migration v11.9-v11.10
            // TODO: Remove this code in v11.11
            var blogConfig = services.GetRequiredService<IBlogConfig>();

            // Sidebar
            var sidebarPitch = await mediator.Send(new GetHtmlPitchQuery(PitchKey.Sidebar));
            if (string.IsNullOrWhiteSpace(sidebarPitch.Value))
            {
                var oldValue1 = blogConfig.GeneralSettings.SideBarCustomizedHtmlPitch;
                if (!string.IsNullOrWhiteSpace(oldValue1))
                {
                    await mediator.Send(new SetHtmlPitchCommand(PitchKey.Sidebar, oldValue1));
                }
            }

            // Footer
            var footerPitch = await mediator.Send(new GetHtmlPitchQuery(PitchKey.Footer));
            if (string.IsNullOrWhiteSpace(footerPitch.Value))
            {
                var oldValue2 = blogConfig.GeneralSettings.FooterCustomizedHtmlPitch;
                if (!string.IsNullOrWhiteSpace(oldValue2))
                {
                    await mediator.Send(new SetHtmlPitchCommand(PitchKey.Footer, oldValue2));
                }
            }

            // Callout
            var calloutPitch = await mediator.Send(new GetHtmlPitchQuery(PitchKey.Callout));
            if (string.IsNullOrWhiteSpace(calloutPitch.Value))
            {
                var oldValue3 = blogConfig.ContentSettings.CalloutSectionHtmlPitch;
                if (!string.IsNullOrWhiteSpace(oldValue3))
                {
                    await mediator.Send(new SetHtmlPitchCommand(PitchKey.Callout, oldValue3));
                }
            }
        }

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
    }
}