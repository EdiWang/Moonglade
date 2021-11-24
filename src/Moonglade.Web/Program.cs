using AspNetCoreRateLimit;
using Edi.Captcha;
using MediatR;
using Microsoft.ApplicationInsights.DependencyCollector;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.Implementation;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.FeatureManagement;
using Moonglade.Auth;
using Moonglade.Caching;
using Moonglade.Comments;
using Moonglade.Configuration;
using Moonglade.Core;
using Moonglade.Data;
using Moonglade.Data.Setup;
using Moonglade.ImageStorage;
using Moonglade.Notification.Client;
using Moonglade.Pingback;
using Moonglade.Syndication;
using Moonglade.Utils;
using Moonglade.Web;
using Moonglade.Web.Configuration;
using Moonglade.Web.Filters;
using Moonglade.Web.Middleware;
using SixLabors.Fonts;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using WilderMinds.MetaWeblog;
using JsonSerializer = System.Text.Json.JsonSerializer;

var info = $"App:\tMoonglade {Helper.AppVersion}\n" +
           $"Path:\t{Environment.CurrentDirectory} \n" +
           $"System:\t{Helper.TryGetFullOSVersion()} \n" +
           $"Host:\t{Environment.MachineName} \n" +
           $"User:\t{Environment.UserName}";
Trace.WriteLine(info);
Console.WriteLine(info);

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddAzureWebAppDiagnostics();
builder.Host.ConfigureAppConfiguration((hostingContext, config) =>
{
    config.AddJsonFile("manifesticons.json", false, true);

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
});

#region DI

// Workaround stupid ASP.NET "by design" issue
// https://github.com/aspnet/Configuration/issues/451
var cultures = builder.Configuration.GetSection("Cultures").Get<string[]>()
            .Select(p => new CultureInfo(p))
            .ToList();

AppDomain.CurrentDomain.Load("Moonglade.FriendLink");
AppDomain.CurrentDomain.Load("Moonglade.Menus");
AppDomain.CurrentDomain.Load("Moonglade.Theme");
builder.Services.AddMediatR(AppDomain.CurrentDomain.GetAssemblies());

// ASP.NET Setup

// Fix docker deployments on Azure App Service blows up with Azure AD authentication
// https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/proxy-load-balancer?view=aspnetcore-6.0
// "Outside of using IIS Integration when hosting out-of-process, Forwarded Headers Middleware isn't enabled by default."
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

builder.Services.AddOptions()
    .AddHttpContextAccessor()
    .AddRateLimit(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddFeatureManagement();
builder.Services.AddAzureAppConfiguration()
    .AddApplicationInsightsTelemetry()
    .ConfigureTelemetryModule<DependencyTrackingTelemetryModule>((module, _) =>
    {
        module.EnableSqlCommandTextInstrumentation = true;
    });

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(20);
    options.Cookie.HttpOnly = true;
}).AddSessionBasedCaptcha(options =>
{
    options.FontStyle = FontStyle.Bold;
});

builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.AddSwaggerGen();
builder.Services.AddControllers(options => options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()))
    .ConfigureApiBehaviorOptions(ConfigureApiBehavior.BlogApiBehavior);
builder.Services.AddRazorPages()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization(options =>
    {
        options.DataAnnotationLocalizerProvider = (_, factory) => factory.Create(typeof(SharedResource));
    })
    .AddRazorPagesOptions(options =>
    {
        options.Conventions.AuthorizeFolder("/Admin");
        options.Conventions.AuthorizeFolder("/Settings");
    });

// Fix Chinese character being encoded in HTML output
builder.Services.AddSingleton(HtmlEncoder.Create(
    UnicodeRanges.BasicLatin,
    UnicodeRanges.CjkCompatibility,
    UnicodeRanges.CjkCompatibilityForms,
    UnicodeRanges.CjkCompatibilityIdeographs,
    UnicodeRanges.CjkRadicalsSupplement,
    UnicodeRanges.CjkStrokes,
    UnicodeRanges.CjkUnifiedIdeographs,
    UnicodeRanges.CjkUnifiedIdeographsExtensionA,
    UnicodeRanges.CjkSymbolsandPunctuation,
    UnicodeRanges.EnclosedCjkLettersandMonths,
    UnicodeRanges.MiscellaneousSymbols,
    UnicodeRanges.HalfwidthandFullwidthForms
));

builder.Services.AddAntiforgery(options =>
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

builder.Services.AddHealthChecks();
builder.Services.AddTransient<RequestBodyLoggingMiddleware>();
builder.Services.AddTransient<ResponseBodyLoggingMiddleware>();

// Blog Services
builder.Services.AddPingback()
    .AddSyndication()
    .AddNotificationClient()
    .AddReleaseCheckerClient()
    .AddBlogCache()
    .AddMetaWeblog<Moonglade.Web.MetaWeblogService>()
    .AddScoped<ValidateCaptcha>()
    .AddScoped<ITimeZoneResolver, BlogTimeZoneResolver>()
    .AddBlogConfig(builder.Configuration)
    .AddBlogAuthenticaton(builder.Configuration)
    .AddComments(builder.Configuration)
    .AddDataStorage(builder.Configuration.GetConnectionString("MoongladeDatabase"))
    .AddImageStorage(builder.Configuration, options =>
    {
        options.ContentRootPath = builder.Environment.ContentRootPath;
    })
    .Configure<List<ManifestIcon>>(builder.Configuration.GetSection("ManifestIcons"));

#endregion

var app = builder.Build();

#region First Run

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var env = services.GetRequiredService<IWebHostEnvironment>();

    var dbConnection = services.GetRequiredService<IDbConnection>();
    var setupHelper = new SetupRunner(dbConnection);
    if (!setupHelper.TestDatabaseConnection(ex =>
        {
            Trace.WriteLine(ex);
            Console.WriteLine(ex);
        })) return;

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

    var mediator = services.GetRequiredService<IMediator>();

    try
    {
        app.Logger.LogInformation("Generating site icons");

        var iconData = await mediator.Send(new GetAssetDataQuery(AssetId.SiteIconBase64));
        MemoryStreamIconGenerator.GenerateIcons(iconData, env.WebRootPath, app.Logger);

        app.Logger.LogInformation($"Generated {MemoryStreamIconGenerator.SiteIconDictionary.Count} icon(s). \n{JsonSerializer.Serialize(MemoryStreamIconGenerator.SiteIconDictionary.Select(p => p.Key).OrderBy(x => x))}");
    }
    catch (Exception e)
    {
        // Non critical error, just log, do not block application start
        app.Logger.LogError(e, e.Message);
    }
}

#endregion

#region Middleware

app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger()
        .UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Moonglade API V1");
        });
}

if (!app.Environment.IsProduction())
{
    app.Logger.LogWarning($"Running in environment: {app.Environment.EnvironmentName}. Application Insights disabled.");

    var tc = app.Services.GetRequiredService<TelemetryConfiguration>();
    tc.DisableTelemetry = true;
    TelemetryDebugWriter.IsTracingDisabled = true;
}

app.Lifetime.ApplicationStopping.Register(() =>
{
    app.Logger.LogInformation("Moonglade is stopping...");
});

app.UseCustomCss(options => options.MaxContentLength = 10240);
app.UseManifest(options => options.ThemeColor = "#333333");
app.UseRobotsTxt();

app.UseOpenSearch(options =>
{
    options.RequestPath = "/opensearch";
    options.IconFileType = "image/png";
    options.IconFilePath = "/favicon-16x16.png";
});

app.UseMiddlewareForFeature<FoafMiddleware>(nameof(FeatureFlags.Foaf));

var bc = app.Services.GetRequiredService<IBlogConfig>();
if (bc.AdvancedSettings.EnableMetaWeblog)
{
    app.UseMiddleware<RSDMiddleware>()
        .UseMetaWeblog("/metaweblog");
}

app.UseMiddleware<SiteMapMiddleware>()
    .UseMiddleware<PoweredByMiddleware>()
    .UseMiddleware<DNTMiddleware>();

if (app.Configuration.GetValue<bool>("PreferAzureAppConfiguration"))
{
    app.UseAzureAppConfiguration();
}

if (app.Environment.IsDevelopment())
{
    app.UseRouteDebugger()
        .UseDeveloperExceptionPage();
}
else
{
    app.UseStatusCodePages(ConfigureStatusCodePages.Handler)
        .UseExceptionHandler("/error");
    app.UseHttpsRedirection()
        .UseHsts();
}

app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new("en-US"),
    SupportedCultures = cultures,
    SupportedUICultures = cultures
});

app.UseDefaultImage(options =>
{
    options.AllowedExtensions = app.Configuration.GetSection("ImageStorage:AllowedExtensions").Get<string[]>();
    options.DefaultImagePath = app.Configuration["ImageStorage:DefaultImagePath"];
});

var rewriteOptions = new RewriteOptions().AddRedirect("^admin$", "admin/post");
app.UseRewriter(rewriteOptions);

app.UseStaticFiles();
app.UseSession().UseCaptchaImage(options =>
{
    options.RequestPath = "/captcha-image";
    options.ImageHeight = 36;
    options.ImageWidth = 100;
});

app.UseIpRateLimiting();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<RequestBodyLoggingMiddleware>();
app.UseMiddleware<ResponseBodyLoggingMiddleware>();

app.UseEndpoints(ConfigureEndpoints.BlogEndpoints);

#endregion

app.Run();