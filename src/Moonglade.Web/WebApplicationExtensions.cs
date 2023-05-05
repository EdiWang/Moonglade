using Microsoft.EntityFrameworkCore;
using Moonglade.Data.MySql;
using Moonglade.Data.PostgreSql;
using Moonglade.Data.SqlServer;

namespace Moonglade.Web;

public static class WebApplicationExtensions
{
    public static async Task<StartupInitResult> InitStartUp(this WebApplication app, string dbType)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var env = services.GetRequiredService<IWebHostEnvironment>();

        BlogDbContext context = dbType.ToLowerInvariant() switch
        {
            "mysql" => services.GetRequiredService<MySqlBlogDbContext>(),
            "sqlserver" => services.GetRequiredService<SqlServerBlogDbContext>(),
            "postgresql" => services.GetRequiredService<PostgreSqlBlogDbContext>(),
            _ => throw new ArgumentOutOfRangeException(nameof(dbType))
        };

        try
        {
            await context.Database.EnsureCreatedAsync();
        }
        catch (Exception e)
        {
            app.Logger.LogCritical(e, e.Message);
            return StartupInitResult.DatabaseConnectionFail;
        }

        bool isNew = !await context.BlogConfiguration.AnyAsync();
        if (isNew)
        {
            try
            {
                app.Logger.LogInformation("Seeding database...");

                await context.ClearAllData();
                await Seed.SeedAsync(context, app.Logger);

                app.Logger.LogInformation("Database seeding successfully.");

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
        var keysToAdd = bc.LoadFromConfig(config);

        var toAdd = keysToAdd as int[] ?? keysToAdd.ToArray();
        if (toAdd.Any())
        {
            foreach (var key in toAdd)
            {
                switch (key)
                {
                    case 1:
                        await mediator.Send(new AddDefaultConfigurationCommand(key, nameof(ContentSettings), ContentSettings.DefaultValue.ToJson()));
                        break;
                    case 2:
                        await mediator.Send(new AddDefaultConfigurationCommand(key, nameof(NotificationSettings), NotificationSettings.DefaultValue.ToJson()));
                        break;
                    case 3:
                        await mediator.Send(new AddDefaultConfigurationCommand(key, nameof(FeedSettings), FeedSettings.DefaultValue.ToJson()));
                        break;
                    case 4:
                        await mediator.Send(new AddDefaultConfigurationCommand(key, nameof(GeneralSettings), GeneralSettings.DefaultValue.ToJson()));
                        break;
                    case 5:
                        await mediator.Send(new AddDefaultConfigurationCommand(key, nameof(ImageSettings), ImageSettings.DefaultValue.ToJson()));
                        break;
                    case 6:
                        await mediator.Send(new AddDefaultConfigurationCommand(key, nameof(AdvancedSettings), AdvancedSettings.DefaultValue.ToJson()));
                        break;
                    case 7:
                        await mediator.Send(new AddDefaultConfigurationCommand(key, nameof(CustomStyleSheetSettings), CustomStyleSheetSettings.DefaultValue.ToJson()));
                        break;
                    case 10:
                        await mediator.Send(new AddDefaultConfigurationCommand(key, nameof(CustomMenuSettings), CustomMenuSettings.DefaultValue.ToJson()));
                        break;
                }
            }
        }

        try
        {
            var iconData = await mediator.Send(new GetAssetQuery(AssetId.SiteIconBase64));
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