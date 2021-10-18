#region Usings

using AspNetCoreRateLimit;
using Edi.Captcha;
using MediatR;
using Microsoft.ApplicationInsights.DependencyCollector;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.Implementation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.FeatureManagement;
using Moonglade.Auth;
using Moonglade.Caching;
using Moonglade.Comments;
using Moonglade.Configuration;
using Moonglade.Configuration.Settings;
using Moonglade.Core;
using Moonglade.Data;
using Moonglade.ImageStorage;
using Moonglade.Notification.Client;
using Moonglade.Pingback;
using Moonglade.Syndication;
using Moonglade.Web.Configuration;
using Moonglade.Web.Filters;
using Moonglade.Web.Middleware;
using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using WilderMinds.MetaWeblog;

#endregion

namespace Moonglade.Web;

public class Startup
{
    private ILogger<Startup> _logger;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    private readonly IList<CultureInfo> _cultures;

    public Startup(IConfiguration configuration, IWebHostEnvironment env)
    {
        _configuration = configuration;
        _environment = env;

        // Workaround stupid ASP.NET "by design" issue
        // https://github.com/aspnet/Configuration/issues/451
        _cultures = _configuration.GetSection("Cultures").Get<string[]>()
            .Select(p => new CultureInfo(p))
            .ToList();
    }

    public void ConfigureServices(IServiceCollection services)
    {
        AppDomain.CurrentDomain.Load("Moonglade.FriendLink");
        AppDomain.CurrentDomain.Load("Moonglade.Menus");
        AppDomain.CurrentDomain.Load("Moonglade.Theme");
        services.AddMediatR(AppDomain.CurrentDomain.GetAssemblies());

        // ASP.NET Setup
        services.AddOptions()
            .AddHttpContextAccessor()
            .AddRateLimit(_configuration.GetSection("IpRateLimiting"));
        services.AddFeatureManagement();
        services.AddAzureAppConfiguration()
            .AddApplicationInsightsTelemetry()
            .ConfigureTelemetryModule<DependencyTrackingTelemetryModule>((module, _) =>
            {
                module.EnableSqlCommandTextInstrumentation = true;
            });

        services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromMinutes(20);
            options.Cookie.HttpOnly = true;
        }).AddSessionBasedCaptcha();

        services.AddLocalization(options => options.ResourcesPath = "Resources");
        services.AddSwaggerGen();
        services.AddControllers(options => options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()))
            .ConfigureApiBehaviorOptions(ConfigureApiBehavior.BlogApiBehavior);
        services.AddRazorPages()
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
        services.AddSingleton(HtmlEncoder.Create(
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

        services.AddAntiforgery(options =>
        {
            const string csrfName = "CSRF-TOKEN-MOONGLADE";
            options.Cookie.Name = $"X-{csrfName}";
            options.FormFieldName = $"{csrfName}-FORM";
            options.HeaderName = "XSRF-TOKEN";
        }).Configure<RequestLocalizationOptions>(options =>
        {
            options.DefaultRequestCulture = new("en-US");
            options.SupportedCultures = _cultures;
            options.SupportedUICultures = _cultures;
        }).Configure<RouteOptions>(options =>
        {
            options.LowercaseUrls = true;
            options.LowercaseQueryStrings = true;
            options.AppendTrailingSlash = false;
        });

        services.AddHealthChecks();
        services.AddTransient<RequestBodyLoggingMiddleware>();
        services.AddTransient<ResponseBodyLoggingMiddleware>();

        // Blog Services
        services.AddPingback()
            .AddSyndication()
            .AddNotificationClient()
            .AddReleaseCheckerClient()
            .AddBlogCache()
            .AddMetaWeblog<MetaWeblogService>()
            .AddScoped<ValidateCaptcha>()
            .AddScoped<ITimeZoneResolver>(c => new BlogTimeZoneResolver(c.GetService<IBlogConfig>()?.GeneralSettings.TimeZoneUtcOffset))
            .AddBlogConfig(_configuration)
            .AddBlogAuthenticaton(_configuration)
            .AddComments(_configuration)
            .AddDataStorage(_configuration.GetConnectionString("MoongladeDatabase"))
            .AddImageStorage(_configuration, options =>
            {
                options.ContentRootPath = _environment.ContentRootPath;
            })
            .Configure<List<ManifestIcon>>(_configuration.GetSection("ManifestIcons"));
    }

    public void Configure(
        IApplicationBuilder app,
        ILogger<Startup> logger,
        IHostApplicationLifetime appLifetime,
        IBlogConfig blogConfig,
        TelemetryConfiguration configuration)
    {
        _logger = logger;

        if (_environment.IsDevelopment())
        {
            app.UseSwagger()
                .UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Moonglade API V1");
                });
        }

        if (!_environment.IsProduction())
        {
            _logger.LogWarning($"Running in environment: {_environment.EnvironmentName}. Application Insights disabled.");

            configuration.DisableTelemetry = true;
            TelemetryDebugWriter.IsTracingDisabled = true;
        }

        appLifetime.ApplicationStopping.Register(() =>
        {
            _logger.LogInformation("Moonglade is stopping...");
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

        if (blogConfig.AdvancedSettings.EnableMetaWeblog)
        {
            app.UseMiddleware<RSDMiddleware>()
                .UseMetaWeblog("/metaweblog");
        }

        app.UseMiddleware<SiteMapMiddleware>()
            .UseMiddleware<PoweredByMiddleware>()
            .UseMiddleware<DNTMiddleware>();

        if (_configuration.GetValue<bool>("PreferAzureAppConfiguration"))
        {
            app.UseAzureAppConfiguration();
        }

        if (_environment.IsDevelopment())
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
            SupportedCultures = _cultures,
            SupportedUICultures = _cultures
        });

        app.UseDefaultImage(options =>
        {
            options.AllowedExtensions = _configuration.GetSection("ImageStorage:AllowedExtensions").Get<string[]>();
            options.DefaultImagePath = _configuration["ImageStorage:DefaultImagePath"];
        });

        var rewriteOptions = new RewriteOptions().AddRedirect("^admin$", "admin/post");
        app.UseRewriter(rewriteOptions);

        app.UseStaticFiles();
        app.UseSession().UseCaptchaImage(options =>
        {
            options.RequestPath = "/captcha-image";
            options.ImageHeight = _configuration.GetValue<int>("Captcha:ImageHeight");
            options.ImageWidth = _configuration.GetValue<int>("Captcha:ImageWidth");
        });

        app.UseIpRateLimiting();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();

        app.UseMiddleware<RequestBodyLoggingMiddleware>();
        app.UseMiddleware<ResponseBodyLoggingMiddleware>();

        app.UseEndpoints(ConfigureEndpoints.BlogEndpoints);
    }
}