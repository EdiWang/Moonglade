using System.Collections.Concurrent;
using System.Data;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using MediatR;
using Moonglade.Configuration;
using Moonglade.Data.Setup;
using Moonglade.Utils;

namespace Moonglade.Web;

public class Program
{
    public static ConcurrentDictionary<string, byte[]> SiteIconDictionary { get; set; } = new();

    public static async Task Main(string[] args)
    {
        var info = $"App:\tMoonglade {Helper.AppVersion}\n" +
                   $"Path:\t{Environment.CurrentDirectory} \n" +
                   $"System:\t{Helper.TryGetFullOSVersion()} \n" +
                   $"Host:\t{Environment.MachineName} \n" +
                   $"User:\t{Environment.UserName}";
        Trace.WriteLine(info);
        Console.WriteLine(info);

        // Support Chinese contents
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        var host = CreateHostBuilder(args).Build();

        using (var scope = host.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            var env = services.GetRequiredService<IWebHostEnvironment>();
            var loggerFactory = services.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<Program>();

            var dataDir = CreateDataDirectory();
            AppDomain.CurrentDomain.SetData("DataDirectory", dataDir);
            logger.LogInformation($"Using data directory '{dataDir}'");

            var dbConnection = services.GetRequiredService<IDbConnection>();
            TryInitFirstRun(dbConnection, logger);

            var mediator = services.GetRequiredService<IMediator>();
            await TryInitSiteIcons(mediator, env.WebRootPath, logger);
        }

        await host.RunAsync();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.CaptureStartupErrors(true)
                    .ConfigureKestrel(c => c.AddServerHeader = false)
                    .UseStartup<Startup>()
                    .ConfigureAppConfiguration((hostingContext, config) =>
                    {
                        config.AddJsonFile("manifesticons.json", false, true)
                            .AddJsonFile("tagnormalization.json", false, true);

                        var settings = config.Build();
                        if (settings.GetValue<bool>("PreferAzureAppConfiguration"))
                        {
                            config.AddAzureAppConfiguration(options =>
                            {
                                options.Connect(settings["ConnectionStrings:AzureAppConfig"])
                                    .ConfigureRefresh(refresh =>
                                    {
                                        refresh.Register("Moonglade:Settings:Sentinel", refreshAll: true)
                                            .SetCacheExpiration(TimeSpan.FromSeconds(10));
                                    })
                                    .UseFeatureFlags(o => o.Label = "Moonglade");
                            });
                        }
                    })
                    .ConfigureLogging(logging =>
                    {
                        logging.AddAzureWebAppDiagnostics();
                    });
            });

    private static async Task TryInitSiteIcons(ISender sender, string webRootPath, ILogger logger)
    {
        try
        {
            logger.LogInformation("Generating site icons");

            var iconData = await sender.Send(new GetAssetDataQuery(AssetId.SiteIconBase64));
            MemoryStreamIconGenerator.GenerateIcons(iconData, webRootPath, logger);

            logger.LogInformation($"Generated {SiteIconDictionary.Count} icon(s). \n{JsonSerializer.Serialize(SiteIconDictionary.Select(p => p.Key).OrderBy(x => x))}");
        }
        catch (Exception e)
        {
            // Non critical error, just log, do not block application start
            logger.LogError(e, e.Message);
        }
    }

    private static string CreateDataDirectory()
    {
        // Use Temp folder as best practice
        // Do NOT create or modify anything under application directory
        // e.g. Azure Deployment using WEBSITE_RUN_FROM_PACKAGE will make website root directory read only.
        var tPath = Path.GetTempPath();
        var appDataPath = Path.Join(tPath, "moonglade", "App_Data");
        if (Directory.Exists(appDataPath))
        {
            Directory.Delete(appDataPath, true);
        }
        Directory.CreateDirectory(appDataPath);

        return appDataPath;
    }

    private static void TryInitFirstRun(IDbConnection dbConnection, ILogger logger)
    {
        var setupHelper = new SetupRunner(dbConnection);

        if (!setupHelper.TestDatabaseConnection(ex =>
            {
                Trace.WriteLine(ex);
                Console.WriteLine(ex);
            })) return;

        if (!setupHelper.IsFirstRun()) return;

        try
        {
            logger.LogInformation("Initializing first run configuration...");
            setupHelper.InitFirstRun();
            logger.LogInformation("Database setup successfully.");
        }
        catch (Exception e)
        {
            logger.LogCritical(e, e.Message);
        }
    }
}