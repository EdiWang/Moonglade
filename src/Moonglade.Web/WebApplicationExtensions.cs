using Edi.ChinaDetector;
using Microsoft.EntityFrameworkCore;
using Moonglade.Data.MySql;
using Moonglade.Data.PostgreSql;
using Moonglade.Data.SqlServer;

namespace Moonglade.Web;

public static class WebApplicationExtensions
{
    public static async Task InitStartUp(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var env = services.GetRequiredService<IWebHostEnvironment>();

        string dbType = app.Configuration.GetConnectionString("DatabaseType")!;
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

            app.MapGet("/", () => Results.Problem(
                detail: "Database connection test failed, please check your connection string and firewall settings, then RESTART Moonglade manually.",
                statusCode: 500
            ));
            await app.RunAsync();
        }

        bool isNew = !await context.BlogConfiguration.AnyAsync();
        if (isNew)
        {
            try
            {
                await SeedDatabase(app, context);
            }
            catch (Exception e)
            {
                app.Logger.LogCritical(e, e.Message);

                app.MapGet("/", () => Results.Problem(
                    detail: "Database setup failed, please check error log, then RESTART Moonglade manually.",
                    statusCode: 500
                ));
                await app.RunAsync();
            }
        }

        var mediator = services.GetRequiredService<IMediator>();

        try
        {
            await InitBlogConfig(app, mediator);
        }
        catch (Exception e)
        {
            app.Logger.LogCritical(e, e.Message);
            app.MapGet("/", () => Results.Problem(
                detail: "Error initializing blog configuration, please check error log, then RESTART Moonglade manually.",
                statusCode: 500
            ));
            await app.RunAsync();
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
    }

    private static async Task SeedDatabase(WebApplication app, BlogDbContext context)
    {
        app.Logger.LogInformation("Seeding database...");

        await context.ClearAllData();
        await Seed.SeedAsync(context, app.Logger);

        app.Logger.LogInformation("Database seeding successfully.");
    }

    private static async Task InitBlogConfig(WebApplication app, IMediator mediator)
    {
        // load configurations into singleton
        var config = await mediator.Send(new GetAllConfigurationsQuery());
        var bc = app.Services.GetRequiredService<IBlogConfig>();
        var keysToAdd = bc.LoadFromConfig(config);

        var toAdd = keysToAdd as int[] ?? keysToAdd.ToArray();
        if (toAdd.Length != 0)
        {
            foreach (var key in toAdd)
            {
                switch (key)
                {
                    case 1:
                        await mediator.Send(new AddDefaultConfigurationCommand(key, nameof(ContentSettings),
                            ContentSettings.DefaultValue.ToJson()));
                        break;
                    case 2:
                        await mediator.Send(new AddDefaultConfigurationCommand(key, nameof(NotificationSettings),
                            NotificationSettings.DefaultValue.ToJson()));
                        break;
                    case 3:
                        await mediator.Send(new AddDefaultConfigurationCommand(key, nameof(FeedSettings),
                            FeedSettings.DefaultValue.ToJson()));
                        break;
                    case 4:
                        await mediator.Send(new AddDefaultConfigurationCommand(key, nameof(GeneralSettings),
                            GeneralSettings.DefaultValue.ToJson()));
                        break;
                    case 5:
                        await mediator.Send(new AddDefaultConfigurationCommand(key, nameof(ImageSettings),
                            ImageSettings.DefaultValue.ToJson()));
                        break;
                    case 6:
                        await mediator.Send(new AddDefaultConfigurationCommand(key, nameof(AdvancedSettings),
                            AdvancedSettings.DefaultValue.ToJson()));
                        break;
                    case 7:
                        await mediator.Send(new AddDefaultConfigurationCommand(key, nameof(CustomStyleSheetSettings),
                            CustomStyleSheetSettings.DefaultValue.ToJson()));
                        break;
                    case 10:
                        await mediator.Send(new AddDefaultConfigurationCommand(key, nameof(CustomMenuSettings),
                            CustomMenuSettings.DefaultValue.ToJson()));
                        break;
                }
            }
        }
    }

    public static async Task DetectChina(this WebApplication app)
    {
        // Read config `DetectChina` to decide how to deal with China
        // Refer: https://go.edi.wang/aka/os251
        var detectChina = app.Configuration["DetectChina"];
        if (!string.IsNullOrWhiteSpace(detectChina))
        {
            var service = new OfflineChinaDetectService();
            var result = await service.Detect(DetectionMethod.TimeZone | DetectionMethod.Culture | DetectionMethod.Behavior);
            if (result.Rank >= 1)
            {
                DealWithChina(app, detectChina);
            }
        }
    }

    private static void DealWithChina(WebApplication app, string detectChina)
    {
        switch (detectChina.ToLower())
        {
            case "block":
                app.Logger.LogError("Positive China detection, application stopped.");

                app.MapGet("/", () => Results.Text(
                    "Due to legal and regulation concerns, we regret to inform you that deploying Moonglade on servers located in Mainland China is currently not possible",
                    statusCode: 251
                ));
                app.Run();

                break;
            case "allow":
            default:
                app.Logger.LogInformation("Current server is suspected to be located in Mainland China, Moonglade will still run on full functionality.");

                break;
        }
    }
}