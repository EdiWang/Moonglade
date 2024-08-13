using Edi.ChinaDetector;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Moonglade.Data.MySql;
using Moonglade.Data.PostgreSql;
using Moonglade.Data.SqlServer;
using System.Net;

namespace Moonglade.Web;

public static class WebApplicationExtensions
{
    public static async Task InitStartUp(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;

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

        try
        {
            var blogConfigInitializer = services.GetRequiredService<IBlogConfigInitializer>();
            await blogConfigInitializer.Init();
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

        if (!isNew)
        {
            try
            {
                var migrationManager = services.GetRequiredService<IMigrationManager>();
                await migrationManager.TryMigration(context);
            }
            catch (Exception e)
            {
                app.Logger.LogCritical(e, e.Message);
                app.MapGet("/", () => Results.Problem(
                    detail: "Error migrating database, please check error log, then RESTART Moonglade manually.",
                    statusCode: 500
                ));
                await app.RunAsync();
            }
        }

        var siteIconInitializer = services.GetRequiredService<ISiteIconInitializer>();
        await siteIconInitializer.GenerateSiteIcons();
    }

    public static void UseSmartXFFHeader(this WebApplication app)
    {
        var fho = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        };

        // ASP.NET Core always use the last value in XFF header, which is AFD's IP address
        // Need to set as `X-Azure-ClientIP` as workaround
        // https://learn.microsoft.com/en-us/azure/frontdoor/front-door-http-headers-protocol
        var headerName = app.Configuration["ForwardedHeaders:HeaderName"];
        if (!string.IsNullOrWhiteSpace(headerName))
        {
            // RFC 7230
            if (headerName.Length > 40 || !Helper.IsValidHeaderName(headerName))
            {
                app.Logger.LogWarning($"XFF header name '{headerName}' is invalid, it will not be applied");
            }
            else
            {
                fho.ForwardedForHeaderName = headerName;
            }
        }

        var knownProxies = app.Configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>();
        if (knownProxies is { Length: > 0 })
        {
            // Fix docker deployments on Azure App Service blows up with Azure AD authentication
            // https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/proxy-load-balancer?view=aspnetcore-6.0
            // "Outside of using IIS Integration when hosting out-of-process, Forwarded Headers Middleware isn't enabled by default."
            if (Helper.IsRunningInDocker())
            {
                // Fix #712
                // Adding KnownProxies will make Azure App Service boom boom with Azure AD redirect URL
                // Result in `https` incorrectly written into `http` and make `/signin-oidc` url invalid.
                app.Logger.LogWarning("Running in Docker, skip adding 'KnownProxies'.");
            }
            else
            {
                fho.ForwardLimit = null;
                fho.KnownProxies.Clear();

                foreach (var ip in knownProxies)
                {
                    fho.KnownProxies.Add(IPAddress.Parse(ip));
                }

                app.Logger.LogInformation("Added known proxies ({0}): {1}",
                    knownProxies.Length,
                    System.Text.Json.JsonSerializer.Serialize(knownProxies));
            }
        }
        else
        {
            // Fix deployment on AFD would not get the correct client IP address because it doesn't trust network other than localhost by default
            // Add this can make ASP.NET Core read forward headers from any network with a potential security issue
            // Attackers can hide their IP by sending a fake header
            // This is OK because Moonglade is just a blog, nothing to hack, let it be
            fho.KnownNetworks.Add(new(IPAddress.Any, 0));
            fho.KnownNetworks.Add(new(IPAddress.IPv6Any, 0));
        }

        app.UseForwardedHeaders(fho);
    }

    private static async Task SeedDatabase(WebApplication app, BlogDbContext context)
    {
        app.Logger.LogInformation("Seeding database...");

        await context.ClearAllData();
        await Seed.SeedAsync(context, app.Logger);

        app.Logger.LogInformation("Database seeding successfully.");
    }

    public static async Task DetectChina(this WebApplication app)
    {
        // Learn more at https://go.edi.wang/aka/os251
        var service = new OfflineChinaDetectService();
        var result = await service.Detect(DetectionMethod.TimeZone | DetectionMethod.Culture | DetectionMethod.Behavior);
        if (result.Rank >= 1)
        {
            DealWithChina(app);
        }
    }

    private static void DealWithChina(WebApplication app)
    {
        void Prevent()
        {
            app.Logger.LogError("Positive China detection, application stopped.");

            app.MapGet("/", () => Results.Text(
                "Due to legal and regulation concerns, we regret to inform you that deploying Moonglade on servers located in China (including Hong Kong) is currently not possible",
                statusCode: 251
            ));
            app.Run();
        }

        if (app.Environment.IsDevelopment())
        {
            app.Logger.LogWarning("Current deployment is suspected to be located in China, Moonglade will still run on full functionality in development environment.");
        }
        else
        {
            Prevent();
        }
    }
}