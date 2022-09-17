using AspNetCoreRateLimit;
using Edi.Captcha;
using Microsoft.ApplicationInsights.DependencyCollector;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.Implementation;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.FeatureManagement;
using Moonglade.Data.MySql;
using Moonglade.Data.SqlServer;
using Moonglade.Notification.Client;
using Moonglade.Pingback;
using Moonglade.Syndication;
using SixLabors.Fonts;
using System.Globalization;
using System.Text.Json.Serialization;
using WilderMinds.MetaWeblog;
using Encoder = Moonglade.Web.Configuration.Encoder;

var info = $"App:\tMoonglade {Helper.AppVersion}\n" +
           $"Path:\t{Environment.CurrentDirectory} \n" +
           $"System:\t{Helper.TryGetFullOSVersion()} \n" +
           $"Host:\t{Environment.MachineName} \n" +
           $"User:\t{Environment.UserName}";
Console.WriteLine(info);

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

var builder = WebApplication.CreateBuilder(args);

string dbType = builder.Configuration.GetConnectionString("DatabaseType");
string connStr = builder.Configuration.GetConnectionString("MoongladeDatabase");

var cultures = new[] { "en-US", "zh-Hans" }.Select(p => new CultureInfo(p)).ToList();

ConfigureConfiguration(builder.Configuration);
ConfigureServices(builder.Services);

var app = builder.Build();

await FirstRun();

ConfigureMiddleware(app);

app.Run();

void ConfigureConfiguration(IConfiguration configuration)
{
    builder.Logging.AddAzureWebAppDiagnostics();
    builder.Host.ConfigureAppConfiguration(config =>
    {
        config.AddJsonFile("manifesticons.json", false, true);
        var appConfigConn = configuration["ConnectionStrings:AzureAppConfig"];

        if (!string.IsNullOrWhiteSpace(appConfigConn))
        {
            config.AddAzureAppConfiguration(options =>
            {
                options.Connect(appConfigConn)
                    .ConfigureRefresh(refresh =>
                    {
                        refresh.Register("Moonglade:Settings:Sentinel", refreshAll: true)
                            .SetCacheExpiration(TimeSpan.FromSeconds(10));
                    })
                    .UseFeatureFlags(o => o.Label = "Moonglade");
            });
        }
    });
}

void ConfigureServices(IServiceCollection services)
{
    AppDomain.CurrentDomain.Load("Moonglade.FriendLink");
    AppDomain.CurrentDomain.Load("Moonglade.Menus");
    AppDomain.CurrentDomain.Load("Moonglade.Theme");
    AppDomain.CurrentDomain.Load("Moonglade.Configuration");
    AppDomain.CurrentDomain.Load("Moonglade.Data");

    services.AddMediatR(AppDomain.CurrentDomain.GetAssemblies());

    // Fix docker deployments on Azure App Service blows up with Azure AD authentication
    // https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/proxy-load-balancer?view=aspnetcore-6.0
    // "Outside of using IIS Integration when hosting out-of-process, Forwarded Headers Middleware isn't enabled by default."
    services.Configure<ForwardedHeadersOptions>(options => options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto);

    services.AddOptions()
            .AddHttpContextAccessor()
            .AddRateLimit(builder.Configuration.GetSection("IpRateLimiting"))
            .AddFeatureManagement();
    services.AddAzureAppConfiguration()
            .AddApplicationInsightsTelemetry()
            .ConfigureTelemetryModule<DependencyTrackingTelemetryModule>((module, _) => module.EnableSqlCommandTextInstrumentation = true);

    services.AddSession(options =>
    {
        options.IdleTimeout = TimeSpan.FromMinutes(20);
        options.Cookie.HttpOnly = true;
    }).AddSessionBasedCaptcha(options => options.FontStyle = FontStyle.Bold);

    services.AddLocalization(options => options.ResourcesPath = "Resources");
    services.AddSwaggerGen();
    services.AddControllers(options => options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()))
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
            .ConfigureApiBehaviorOptions(ConfigureApiBehavior.BlogApiBehavior);
    services.AddRazorPages()
            .AddDataAnnotationsLocalization(options => options.DataAnnotationLocalizerProvider = (_, factory) => factory.Create(typeof(SharedResource)))
            .AddRazorPagesOptions(options =>
            {
                options.Conventions.AddPageRoute("/Admin/Post", "admin");
                options.Conventions.AuthorizeFolder("/Admin");
                options.Conventions.AuthorizeFolder("/Settings");
            });

    // Fix Chinese character being encoded in HTML output
    services.AddSingleton(Encoder.MoongladeHtmlEncoder);

    services.AddAntiforgery(options =>
    {
        const string csrfName = "CSRF-TOKEN-MOONGLADE";
        options.Cookie.Name = $"X-{csrfName}";
        options.FormFieldName = $"{csrfName}-FORM";
        options.HeaderName = "XSRF-TOKEN";
    }).Configure<RequestLocalizationOptions>(options =>
    {
        options.DefaultRequestCulture = new("en-US");
        options.SupportedCultures = cultures;
        options.SupportedUICultures = cultures;
    }).Configure<RouteOptions>(options =>
    {
        options.LowercaseUrls = true;
        options.LowercaseQueryStrings = true;
        options.AppendTrailingSlash = false;
    });

    services.AddHealthChecks();
    services.AddPingback()
            .AddSyndication()
            .AddNotification()
            .AddReleaseCheckerClient()
            .AddBlogCache()
            .AddMetaWeblog<Moonglade.Web.MetaWeblogService>()
            .AddScoped<ValidateCaptcha>()
            .AddScoped<ITimeZoneResolver, BlogTimeZoneResolver>()
            .AddBlogConfig(builder.Configuration)
            .AddBlogAuthenticaton(builder.Configuration)
            .AddComments(builder.Configuration)
            .AddImageStorage(builder.Configuration, options => options.ContentRootPath = builder.Environment.ContentRootPath)
            .Configure<List<ManifestIcon>>(builder.Configuration.GetSection("ManifestIcons"));

    switch (dbType.ToLower())
    {
        case "mysql":
            services.AddMySqlStorage(connStr);
            break;
        case "sqlserver":
        default:
            services.AddSqlServerStorage(connStr);
            break;
    }
}

async Task FirstRun()
{
    try
    {
        var startUpResut = await app.InitStartUp(dbType);
        switch (startUpResut)
        {
            case StartupInitResult.DatabaseConnectionFail:
                app.MapGet("/", () => Results.Problem(
                    detail: "Database connection test failed, please check your connection string and firewall settings, then RESTART Moonglade manually.",
                    statusCode: 500
                    ));
                app.Run();
                return;
            case StartupInitResult.DatabaseSetupFail:
                app.MapGet("/", () => Results.Problem(
                    detail: "Database setup failed, please check error log, then RESTART Moonglade manually.",
                    statusCode: 500
                ));
                app.Run();
                return;
        }
    }
    catch (Exception e)
    {
        app.MapGet("/", _ => throw new("Start up failed: " + e.Message));
        app.Run();
    }
}

void ConfigureMiddleware(IApplicationBuilder appBuilder)
{
    appBuilder.UseForwardedHeaders();

    if (!app.Environment.IsProduction())
    {
        app.Logger.LogWarning($"Running in environment: {app.Environment.EnvironmentName}. Application Insights disabled.");

        var tc = app.Services.GetRequiredService<TelemetryConfiguration>();
        tc.DisableTelemetry = true;
        TelemetryDebugWriter.IsTracingDisabled = true;
    }

    appBuilder.UseCustomCss(options => options.MaxContentLength = 10240);
    appBuilder.UseManifest(options => options.ThemeColor = "#333333");
    appBuilder.UseRobotsTxt();

    appBuilder.UseOpenSearch(options =>
    {
        options.RequestPath = "/opensearch";
        options.IconFileType = "image/png";
        options.IconFilePath = "/favicon-16x16.png";
    });

    appBuilder.UseMiddlewareForFeature<FoafMiddleware>(nameof(FeatureFlags.Foaf));

    var bc = app.Services.GetRequiredService<IBlogConfig>();
    if (bc.AdvancedSettings.EnableMetaWeblog)
    {
        appBuilder.UseMiddleware<RSDMiddleware>().UseMetaWeblog("/metaweblog");
    }

    appBuilder.UseMiddleware<SiteMapMiddleware>()
              .UseMiddleware<PoweredByMiddleware>()
              .UseMiddleware<DNTMiddleware>();

    if (app.Configuration.GetValue<bool>("PreferAzureAppConfiguration"))
    {
        appBuilder.UseAzureAppConfiguration();
    }

    if (app.Environment.IsDevelopment())
    {
        appBuilder.UseSwagger().UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Moonglade API V1"));
        appBuilder.UseRouteDebugger().UseDeveloperExceptionPage();
    }
    else
    {
        appBuilder.UseStatusCodePages(ConfigureStatusCodePages.Handler).UseExceptionHandler("/error");
    }

    appBuilder.UseHttpsRedirection().UseHsts();
    appBuilder.UseRequestLocalization(new RequestLocalizationOptions
    {
        DefaultRequestCulture = new("en-US"),
        SupportedCultures = cultures,
        SupportedUICultures = cultures
    });

    appBuilder.UseDefaultImage(options =>
    {
        options.AllowedExtensions = app.Configuration.GetSection("ImageStorage:AllowedExtensions").Get<string[]>();
        options.DefaultImagePath = app.Configuration["ImageStorage:DefaultImagePath"];
    });

    appBuilder.UseStaticFiles();
    appBuilder.UseSession().UseCaptchaImage(options =>
    {
        options.RequestPath = "/captcha-image";
        options.ImageHeight = 36;
        options.ImageWidth = 100;
    });

    appBuilder.UseIpRateLimiting();
    appBuilder.UseRouting();
    appBuilder.UseAuthentication().UseAuthorization();

    appBuilder.UseEndpoints(ConfigureEndpoints.BlogEndpoints);
}