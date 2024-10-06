using System.Globalization;
using System.Text.Json.Serialization;
using Edi.Captcha;
using Edi.PasswordGenerator;
using Microsoft.AspNetCore.Rewrite;
using Moonglade.Comments.Moderator;
using Moonglade.Data.MySql;
using Moonglade.Data.PostgreSql;
using Moonglade.Data.SqlServer;
using Moonglade.Email.Client;
using Moonglade.IndexNow.Client;
using Moonglade.Mention.Common;
using Moonglade.Pingback;
using Moonglade.Setup;
using Moonglade.Syndication;
using Moonglade.Web.Handlers;
using Moonglade.Webmention;
using SixLabors.Fonts;
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
        ConfigureServices(builder.Services, builder.Configuration, builder.Environment, cultures);

        var app = builder.Build();
        if (!app.Environment.IsDevelopment() && await Helper.IsRunningInChina())
        {
            app.Logger.LogCritical("Positive China detection, application stopped.");
            await app.StopAsync();
        }

        await app.InitStartUp();
        ConfigureMiddleware(app, cultures);

        app.Run();
    }

    private static void LoadAssemblies()
    {
        var assemblies = new[]
        {
            "Moonglade.Auth",
            "Moonglade.Comments",
            "Moonglade.Core",
            "Moonglade.Email.Client",
            "Moonglade.FriendLink",
            "Moonglade.Syndication",
            "Moonglade.Theme",
            "Moonglade.Data",
            "Moonglade.Webmention",
            "Moonglade.Pingback",
            "Moonglade.Mention.Common",
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
        return cultureCodes.Select(code => new CultureInfo(code)).ToList();
    }

    private static void ConfigureLogging(WebApplicationBuilder builder)
    {
        if (Helper.IsRunningOnAzureAppService())
        {
            builder.Logging.AddAzureWebAppDiagnostics();
        }
    }

    private static void ConfigureServices(IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment, List<CultureInfo> cultures)
    {
        services.AddMediatR(config => config.RegisterServicesFromAssemblies(AppDomain.CurrentDomain.GetAssemblies()));
        services.AddOptions().AddHttpContextAccessor();
        ConfigureSession(services);
        ConfigureCaptcha(services, configuration);
        ConfigureLocalization(services);
        ConfigureControllers(services);
        ConfigureRazorPages(services);
        services.AddSingleton(Encoder.MoongladeHtmlEncoder);
        ConfigureAntiforgery(services);
        ConfigureRequestLocalization(services, cultures);
        ConfigureRouteOptions(services);
        services.AddTransient<IPasswordGenerator, DefaultPasswordGenerator>();
        services.AddHealthChecks();
        ConfigureMoongladeServices(services, configuration, environment);
        ConfigureDatabase(services, configuration);
        ConfigureInitializers(services);
    }

    private static void ConfigureSession(IServiceCollection services)
    {
        services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromMinutes(20);
            options.Cookie.HttpOnly = true;
        });
    }

    private static void ConfigureCaptcha(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSessionBasedCaptcha(options =>
        {
            var magics = new List<string>
            {
                Encoding.UTF8.GetString(BitConverter.GetBytes('✔'.GetHashCode())
                    .Zip(BitConverter.GetBytes(0x242F2E32)).Select(x => (byte)(x.First + x.Second)).ToArray()),
                Helper.GetMagic(0x6B441, 11, 15),
                Helper.GetMagic(0x1499E, 10, 14)
            };

            if (bool.Parse(configuration["BlockPRCFuryCode"]!))
            {
                magics.AddRange(new[]
                {
                    Helper.GetMagic(0x7DB14, 21, 25),
                    Helper.GetMagic(0x78E10, 13, 17),
                    Helper.GetMagic(0x17808, 34, 38),
                    Helper.GetMagic(0x1B5ED, 4, 8),
                    Helper.GetMagic(0x9CFB, 25, 29),
                    "NMSL", "CNMB", "MDZZ", "TNND"
                });
            }

            options.FontStyle = FontStyle.Bold;
            options.BlockedCodes = magics.ToArray();
        });
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

    private static void ConfigureMoongladeServices(IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        services.AddMentionCommon()
            .AddPingback()
            .AddWebmention()
            .AddSyndication()
            .AddInMemoryCacheAside()
            .AddScoped<ValidateCaptcha>()
            .AddScoped<ITimeZoneResolver, BlogTimeZoneResolver>()
            .AddBlogConfig()
            .AddBlogAuthenticaton(configuration)
            .AddImageStorage(configuration, options => options.ContentRootPath = environment.ContentRootPath);

        services.AddEmailClient();
        services.AddIndexNowClient(configuration.GetSection("IndexNow"));
        services.AddContentModerator(configuration);
        services.AddSingleton<CannonService>();
    }

    private static void ConfigureDatabase(IServiceCollection services, IConfiguration configuration)
    {
        var dbType = configuration.GetConnectionString("DatabaseType")!.ToLower();
        var connStr = configuration.GetConnectionString("MoongladeDatabase");

        switch (dbType)
        {
            case "mysql":
                services.AddMySqlStorage(connStr!);
                break;
            case "postgresql":
                services.AddPostgreSqlStorage(connStr!);
                break;
            case "sqlserver":
            default:
                services.AddSqlServerStorage(connStr!);
                break;
        }
    }

    private static void ConfigureInitializers(IServiceCollection services)
    {
        services.AddTransient<ISiteIconInitializer, SiteIconInitializer>();
        services.AddScoped<IMigrationManager, MigrationManager>();
        services.AddScoped<IBlogConfigInitializer, BlogConfigInitializer>();
        services.AddScoped<IStartUpInitializer, StartUpInitializer>();
    }

    private static void ConfigureMiddleware(WebApplication app, List<CultureInfo> cultures)
    {
        bool useXFFHeaders = app.Configuration.GetSection("ForwardedHeaders:Enabled").Get<bool>();
        if (useXFFHeaders) app.UseSmartXFFHeader();

        app.UseCustomCss(options => options.MaxContentLength = 10240);
        app.UseOpenSearch(options =>
        {
            options.RequestPath = "/opensearch";
            options.IconFileType = "image/png";
            options.IconFilePath = "/favicon-16x16.png";
        });

        app.UseMiddleware<PoweredByMiddleware>();
        app.UseMiddleware<DNTMiddleware>();

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

        var options = new RewriteOptions().AddRedirect(@"(.*)/$", @"\$1", 301);
        app.UseRewriter(options);
        app.UseStaticFiles();
        app.UseSession().UseCaptchaImage(p =>
        {
            p.RequestPath = "/captcha-image";
            p.ImageHeight = 36;
            p.ImageWidth = 100;
        });

        app.UseRouting();
        app.UseAuthentication().UseAuthorization();

        app.MapHealthChecks("/ping", new()
        {
            ResponseWriter = ConfigureEndpoints.WriteResponse
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
