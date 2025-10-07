using Edi.AspNetCore.Utils;
using Edi.Captcha;
using Edi.PasswordGenerator;
using LiteBus.Commands;
using LiteBus.Events;
using LiteBus.Extensions.Microsoft.DependencyInjection;
using LiteBus.Queries;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moonglade.Data.MySql;
using Moonglade.Data.PostgreSql;
using Moonglade.Data.SqlServer;
using Moonglade.Email.Client;
using Moonglade.IndexNow.Client;
using Moonglade.Moderation;
using Moonglade.Setup;
using Moonglade.Syndication;
using Moonglade.Web.BackgroundServices;
using Moonglade.Web.Handlers;
using Moonglade.Web.HealthChecks;
using Moonglade.Webmention;
using SixLabors.Fonts;
using System.Globalization;
using System.Net;
using System.Text.Json.Serialization;
using Encoder = Moonglade.Web.Configuration.Encoder;

namespace Moonglade.Web;

public class Program
{
    public static async Task Main(string[] args)
    {
        LoadAssemblies();
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        var cultures = GetSupportedCultures();
        var builder = WebApplication.CreateBuilder(args);
        builder.WriteParameterTable();

        ConfigureLogging(builder);
        ConfigureServices(builder.Services, builder.Configuration, cultures);

        var app = builder.Build();

        await app.InitStartUp();
        ConfigureMiddleware(app, cultures);

        await app.RunAsync();
    }

    private static void LoadAssemblies()
    {
        var assemblies = new[]
        {
            "Moonglade.Webmention",
            "Moonglade.Auth",
            "Moonglade.Comments",
            "Moonglade.Features",
            "Moonglade.Email.Client",
            "Moonglade.IndexNow.Client",
            "Moonglade.Syndication",
            "Moonglade.Theme",
            "Moonglade.Data",
            "Moonglade.Configuration"
        };

        foreach (var assembly in assemblies)
        {
            AppDomain.CurrentDomain.Load(assembly);
        }
    }

    private static List<CultureInfo> GetSupportedCultures()
    {
        var cultureCodes = new[] { "en-US", "zh-Hans", "zh-Hant", "de-DE", "ja-JP" };
        return [.. cultureCodes.Select(code => new CultureInfo(code))];
    }

    private static void ConfigureLogging(WebApplicationBuilder builder)
    {
        if (EnvironmentHelper.IsRunningOnAzureAppService())
        {
            builder.Logging.AddAzureWebAppDiagnostics();
        }
    }

    private static void ConfigureServices(IServiceCollection services, IConfiguration configuration, List<CultureInfo> cultures)
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        assemblies = [.. assemblies.Where(x => x.FullName!.StartsWith("Moonglade"))];

        services.AddLiteBus(liteBus =>
        {
            liteBus.AddCommandModule(module =>
            {
                foreach (var assembly in assemblies)
                {
                    module.RegisterFromAssembly(assembly);
                }
            });

            liteBus.AddQueryModule(module =>
            {
                foreach (var assembly in assemblies)
                {
                    module.RegisterFromAssembly(assembly);
                }
            });

            liteBus.AddEventModule(module =>
            {
                foreach (var assembly in assemblies)
                {
                    module.RegisterFromAssembly(assembly);
                }
            });
        });

        services.AddHttpClient();
        services.AddOptions().AddHttpContextAccessor();
        ConfigureCaptcha(services, configuration);
        ConfigureLocalization(services);
        ConfigureControllers(services);
        ConfigureRazorPages(services);
        services.AddSingleton(Encoder.MoongladeHtmlEncoder);
        ConfigureAntiforgery(services);
        ConfigureRequestLocalization(services, cultures);
        ConfigureRouteOptions(services);
        services.AddTransient<IPasswordGenerator, DefaultPasswordGenerator>();
        services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy("Application is running"))
            .AddDbContextCheck<BlogDbContext>("database", tags: ["db", "ready"])
            .AddCheck<DatabaseConnectivityHealthCheck>("database_connectivity", tags: ["db"]);
        ConfigureMoongladeServices(services, configuration);
        ConfigureDatabase(services, configuration);
        ConfigureInitializers(services);
    }

    private static void ConfigureCaptcha(IServiceCollection services, IConfiguration configuration)
    {
        var magics = new List<string>
            {
                Encoding.UTF8.GetString([.. BitConverter.GetBytes('✔'.GetHashCode())
                    .Zip(BitConverter.GetBytes(0x242F2E32)).Select(x => (byte)(x.First + x.Second))]),
                Helper.GetMagic(0x6B441, 11, 15)
            };

        var captchaKey = configuration["CaptchaSettings:SharedKey"];
        var expirationMinutes = configuration.GetValue<int>("CaptchaSettings:TokenExpirationMinutes", 5);

        services.AddSharedKeyStatelessCaptcha(options =>
        {
            options.SharedKey = captchaKey;
            options.TokenExpiration = TimeSpan.FromMinutes(expirationMinutes);
            options.FontStyle = FontStyle.Bold;
            options.BlockedCodes = [.. magics];
            options.DrawLines = true;
        });

        services.AddScoped<ValidateCaptcha>();
    }

    private static void ConfigureLocalization(IServiceCollection services)
    {
        services.AddLocalization(options => options.ResourcesPath = "Resources");
    }

    private static void ConfigureControllers(IServiceCollection services)
    {
        services.AddControllers(options => options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()))
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
            .ConfigureApiBehaviorOptions(ConfigureApiBehavior.BlogApiBehavior);
    }

    private static void ConfigureRazorPages(IServiceCollection services)
    {
        services.AddRazorPages()
            .AddDataAnnotationsLocalization(options =>
                options.DataAnnotationLocalizerProvider = (_, factory) => factory.Create(typeof(Program)))
            .AddRazorPagesOptions(options =>
            {
                options.Conventions.AddPageRoute("/Admin/Post", "admin");
                options.Conventions.AuthorizeFolder("/Admin");
                options.Conventions.AuthorizeFolder("/Settings");
            })
            .AddViewOptions(options =>
            {
                // Fix '__Invariant' form input rendering issue
                options.HtmlHelperOptions.FormInputRenderMode = FormInputRenderMode.AlwaysUseCurrentCulture;
            });
    }

    private static void ConfigureAntiforgery(IServiceCollection services)
    {
        services.AddAntiforgery(options =>
        {
            const string csrfName = "CSRF-TOKEN-MOONGLADE";
            options.Cookie.Name = $"X-{csrfName}";
            options.FormFieldName = $"{csrfName}-FORM";
            options.HeaderName = "XSRF-TOKEN";
        });
    }

    private static void ConfigureRequestLocalization(IServiceCollection services, List<CultureInfo> cultures)
    {
        services.Configure<RequestLocalizationOptions>(options =>
        {
            options.DefaultRequestCulture = new("en-US");
            options.SupportedCultures = cultures;
            options.SupportedUICultures = cultures;
        });
    }

    private static void ConfigureRouteOptions(IServiceCollection services)
    {
        services.Configure<RouteOptions>(options =>
        {
            options.LowercaseUrls = true;
            options.AppendTrailingSlash = false;
        });
    }

    private static void ConfigureMoongladeServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddWebmention();

        services.AddSyndication()
                .AddInMemoryCacheAside()
                .AddBlogConfig()
                .AddAnalytics(configuration)
                .AddBlogAuthenticaton(configuration)
                .AddImageStorage(configuration);

        services.AddEmailClient();
        services.AddIndexNowClient(configuration.GetSection("IndexNow"));
        services.AddContentModerator(configuration);

        if (configuration.GetValue<bool>("PostScheduler:Enabled"))
        {
            services.AddSingleton<ScheduledPublishWakeUp>();
            services.AddHostedService<ScheduledPublishService>();
        }

        services.AddSingleton<CannonService>();
    }

    private static void ConfigureDatabase(IServiceCollection services, IConfiguration configuration)
    {
        var connStr = configuration.GetConnectionString("MoongladeDatabase");
        var dbType = configuration.GetConnectionString("DatabaseProvider");

        switch (dbType.ToLower())
        {
            case "mysql":
                services.AddMySqlStorage(connStr!);
                break;
            case "postgresql":
                services.AddPostgreSqlStorage(connStr!);
                break;
            case "sqlserver":
                services.AddSqlServerStorage(connStr!);
                break;
            default:
                throw new NotSupportedException("Unknown database type, please check connection string.");
        }
    }

    private static void ConfigureInitializers(IServiceCollection services)
    {
        services.AddTransient<ISiteIconBuilder, SiteIconBuilder>();
        services.AddScoped<IMigrationManager, MigrationManager>();
        services.AddScoped<IConfigInitializer, ConfigInitializer>();
        services.AddScoped<IStartUpInitializer, StartUpInitializer>();
    }

    private static void ConfigureMiddleware(WebApplication app, List<CultureInfo> cultures)
    {
        bool useXFFHeaders = app.Configuration.GetValue<bool>("ForwardedHeaders:Enabled");
        if (useXFFHeaders) app.UseSmartXFFHeader();

        app.UseCustomCss(options => options.MaxContentLength = 10240);
        app.UseOpenSearch(options =>
        {
            options.RequestPath = "/opensearch";
            options.IconFileType = "image/png";
            options.IconFilePath = "/favicon-16x16.png";
        });

        bool usePrefersColorSchemeHeader = app.Configuration.GetValue<bool>("PrefersColorSchemeHeader:Enabled");
        if (usePrefersColorSchemeHeader) app.UseMiddleware<PrefersColorSchemeMiddleware>();

        app.UseMiddleware<PoweredByMiddleware>();

        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseStatusCodePages(ConfigureStatusCodePages.Handler).UseExceptionHandler("/error");
        }

        app.UseHttpsRedirection();
        app.UseRequestLocalization(new RequestLocalizationOptions
        {
            DefaultRequestCulture = new("en-US"),
            SupportedCultures = cultures,
            SupportedUICultures = cultures
        });

        var options = new RewriteOptions().AddRedirect(@"(.*)/$", @"$1", (int)HttpStatusCode.MovedPermanently);
        app.UseRewriter(options);
        app.UseStaticFiles();
        //app.UseCaptchaImage(p =>
        //{
        //    p.RequestPath = "/captcha-image";
        //    p.ImageHeight = 36;
        //    p.ImageWidth = 100;
        //});

        app.UseRouting();
        app.UseAuthentication().UseAuthorization();

        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = PingEndpoint.WriteResponse,
            AllowCachingResponses = false
        });

        app.MapControllers();
        app.MapRazorPages();
        app.MapGet("/robots.txt", RobotsTxtMapHandler.Handler);

        if (!string.IsNullOrWhiteSpace(app.Configuration["IndexNow:ApiKey"]))
        {
            app.MapGet($"/{app.Configuration["IndexNow:ApiKey"]}.txt", IndexNowMapHandler.Handler);
        }

        app.MapGet("/manifest.webmanifest", WebManifestMapHandler.Handler);
        var bc = app.Services.GetRequiredService<IBlogConfig>();
        if (bc.AdvancedSettings.EnableFoaf)
        {
            app.MapGet("/foaf.xml", FoafMapHandler.Handler);
        }

        if (bc.AdvancedSettings.EnableSiteMap)
        {
            app.MapGet("/sitemap.xml", SiteMapMapHandler.Handler);
        }
    }
}
