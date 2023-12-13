﻿using AspNetCoreRateLimit;
using Edi.Captcha;
using Edi.ChinaDetector;
using Edi.PasswordGenerator;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.Implementation;
using Microsoft.AspNetCore.HttpOverrides;
using Moonglade.Comments.Moderator;
using Moonglade.Data.MySql;
using Moonglade.Data.PostgreSql;
using Moonglade.Data.SqlServer;
using Moonglade.Email.Client;
using Moonglade.Pingback;
using Moonglade.Syndication;
using SixLabors.Fonts;
using Spectre.Console;
using System.Globalization;
using System.Net;
using System.Text.Json.Serialization;
using WilderMinds.MetaWeblog;
using Encoder = Moonglade.Web.Configuration.Encoder;
using IPNetwork = Microsoft.AspNetCore.HttpOverrides.IPNetwork;

Console.OutputEncoding = Encoding.UTF8;
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

var builder = WebApplication.CreateBuilder(args);

string dbType = builder.Configuration.GetConnectionString("DatabaseType");
string connStr = builder.Configuration.GetConnectionString("MoongladeDatabase");

var cultures = new[] { "en-US", "zh-Hans" }.Select(p => new CultureInfo(p)).ToList();

builder.WriteParameterTable();
AnsiConsole.MarkupLine("[link=https://github.com/EdiWang/Moonglade]GitHub: EdiWang/Moonglade[/]");

ConfigureConfiguration();
ConfigureServices(builder.Services);

var app = builder.Build();

await DetectChina();
await FirstRun();

ConfigureMiddleware();

app.Run();

void ConfigureConfiguration()
{
    builder.Logging.AddAzureWebAppDiagnostics();
    builder.Configuration.AddJsonFile("manifesticons.json", false, true);
}

void ConfigureServices(IServiceCollection services)
{
    AppDomain.CurrentDomain.Load("Moonglade.Core");
    AppDomain.CurrentDomain.Load("Moonglade.FriendLink");
    AppDomain.CurrentDomain.Load("Moonglade.Theme");
    AppDomain.CurrentDomain.Load("Moonglade.Configuration");
    AppDomain.CurrentDomain.Load("Moonglade.Data");

    services.AddMediatR(config => config.RegisterServicesFromAssemblies(AppDomain.CurrentDomain.GetAssemblies()));
    services.AddOptions()
            .AddHttpContextAccessor()
            .AddRateLimit(builder.Configuration.GetSection("IpRateLimiting"));
    services.AddApplicationInsightsTelemetry();

    services.AddSession(options =>
    {
        options.IdleTimeout = TimeSpan.FromMinutes(20);
        options.Cookie.HttpOnly = true;
    }).AddSessionBasedCaptcha(options => options.FontStyle = FontStyle.Bold);

    services.AddLocalization(options => options.ResourcesPath = "Resources");
    services.AddControllers(options => options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()))
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
            .ConfigureApiBehaviorOptions(ConfigureApiBehavior.BlogApiBehavior);
    services.AddRazorPages()
            .AddDataAnnotationsLocalization(options => options.DataAnnotationLocalizerProvider = (_, factory) => factory.Create(typeof(Program)))
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
    });

    services.Configure<RequestLocalizationOptions>(options =>
    {
        options.DefaultRequestCulture = new("en-US");
        options.SupportedCultures = cultures;
        options.SupportedUICultures = cultures;
    });

    services.Configure<RouteOptions>(options =>
    {
        options.LowercaseUrls = true;
        options.LowercaseQueryStrings = true;
        options.AppendTrailingSlash = false;
    });

    services.AddTransient<IPasswordGenerator, DefaultPasswordGenerator>();

    services.AddHealthChecks();
    services.AddPingback()
            .AddSyndication()
            .AddNotification()
            .AddInMemoryCacheAside()
            .AddMetaWeblog<Moonglade.Web.MetaWeblogService>()
            .AddScoped<ValidateCaptcha>()
            .AddScoped<ITimeZoneResolver, BlogTimeZoneResolver>()
            .AddBlogConfig()
            .AddBlogAuthenticaton(builder.Configuration)
            .AddContentModerator(builder.Configuration)
            .AddImageStorage(builder.Configuration, options => options.ContentRootPath = builder.Environment.ContentRootPath)
            .Configure<List<ManifestIcon>>(builder.Configuration.GetSection("ManifestIcons"));

    switch (dbType!.ToLower())
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

async Task DetectChina()
{
    // Read config `Experimental:DetectChina` to decide how to deal with China
    // Refer: https://go.edi.wang/aka/os251
    var detectChina = builder.Configuration["Experimental:DetectChina"];
    if (!string.IsNullOrWhiteSpace(detectChina))
    {
        var service = new OfflineChinaDetectService();
        var result = await service.Detect(DetectionMethod.TimeZone | DetectionMethod.Culture);
        if (result.Rank >= 1)
        {
            DealWithChina(detectChina);
        }
        else
        {
            app.Logger.LogInformation("Offline China detection result negative, trying online detection");

            // Try online detection
            var onlineService = new OnlineChinaDetectService(new());
            var onlineResult = await onlineService.Detect(DetectionMethod.IPAddress | DetectionMethod.GFWTest);
            if (onlineResult.Rank >= 1)
            {
                DealWithChina(detectChina);
            }
        }
    }
}

void DealWithChina(string detectChina)
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

async Task FirstRun()
{
    try
    {
        var startUpResut = await app.InitStartUp(dbType);

        // Change `switch-case` to `if-else` for workaround https://github.com/dotnet/aspnetcore/issues/51285
        if (startUpResut == StartupInitResult.DatabaseConnectionFail)
        {
            app.MapGet("/", () => Results.Problem(
                detail: "Database connection test failed, please check your connection string and firewall settings, then RESTART Moonglade manually.",
                statusCode: 500
                ));
            app.Run();
        }
        else if (startUpResut == StartupInitResult.DatabaseSetupFail)
        {
            app.MapGet("/", () => Results.Problem(
                detail: "Database setup failed, please check error log, then RESTART Moonglade manually.",
                statusCode: 500
            ));
            app.Run();
        }
    }
    catch (Exception e)
    {
        app.MapGet("/", _ => throw new("Start up failed: " + e.Message));
        app.Run();
    }
}

void ConfigureMiddleware()
{
    bool useFWHeaders = builder.Configuration.GetSection("ForwardedHeaders:Enabled").Get<bool>();

    if (useFWHeaders)
    {
        var fho = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        };

        // ASP.NET Core always use the last value in XFF header, which is AFD's IP address
        // Need to set as `X-Azure-ClientIP` as workaround
        // https://learn.microsoft.com/en-us/azure/frontdoor/front-door-http-headers-protocol
        var headerName = builder.Configuration["ForwardedHeaders:HeaderName"];
        if (!string.IsNullOrWhiteSpace(headerName))
        {
            fho.ForwardedForHeaderName = headerName;
        }

        var knownProxies = builder.Configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>();
        if (knownProxies is { Length: > 0 })
        {
            // Fix docker deployments on Azure App Service blows up with Azure AD authentication
            // https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/proxy-load-balancer?view=aspnetcore-6.0
            // "Outside of using IIS Integration when hosting out-of-process, Forwarded Headers Middleware isn't enabled by default."
            if (Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true")
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
                    System.Text.Json.JsonSerializer.Serialize(knownProxies).EscapeMarkup());
            }
        }
        else
        {
            // Fix deployment on AFD would not get the correct client IP address because it doesn't trust network other than localhost by default
            // Add this can make ASP.NET Core read forward headers from any network with a potential security issue
            // Attackers can hide their IP by sending a fake header
            // This is OK because Moonglade is just a blog, nothing to hack, let it be
            fho.KnownNetworks.Add(new IPNetwork(IPAddress.Any, 0));
            fho.KnownNetworks.Add(new IPNetwork(IPAddress.IPv6Any, 0));
        }

        app.UseForwardedHeaders(fho);
    }

    if (!app.Environment.IsProduction())
    {
        app.Logger.LogWarning($"Running in environment: {app.Environment.EnvironmentName}. Application Insights disabled.");

        var tc = app.Services.GetRequiredService<TelemetryConfiguration>();
        tc.DisableTelemetry = true;
        TelemetryDebugWriter.IsTracingDisabled = true;
    }

    app.UseCustomCss(options => options.MaxContentLength = 10240);
    app.UseManifest(options => options.ThemeColor = "#333333");
    app.UseRobotsTxt();

    app.UseOpenSearch(options =>
    {
        options.RequestPath = "/opensearch";
        options.IconFileType = "image/png";
        options.IconFilePath = "/favicon-16x16.png";
    });

    var bc = app.Services.GetRequiredService<IBlogConfig>();

    if (bc.AdvancedSettings.EnableFoaf)
    {
        app.UseMiddleware<FoafMiddleware>();
    }

    if (bc.AdvancedSettings.EnableMetaWeblog)
    {
        app.UseMiddleware<RSDMiddleware>().UseMetaWeblog("/metaweblog");
    }

    app.UseMiddleware<SiteMapMiddleware>();
    app.UseMiddleware<PoweredByMiddleware>();
    app.UseMiddleware<DNTMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }
    else
    {
        app.UseStatusCodePages(ConfigureStatusCodePages.Handler).UseExceptionHandler("/error");
        // app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseRequestLocalization(new RequestLocalizationOptions
    {
        DefaultRequestCulture = new("en-US"),
        SupportedCultures = cultures,
        SupportedUICultures = cultures
    });

    app.UseStaticFiles();
    app.UseSession().UseCaptchaImage(options =>
    {
        options.RequestPath = "/captcha-image";
        options.ImageHeight = 36;
        options.ImageWidth = 100;
    });

    app.UseIpRateLimiting();
    app.UseRouting();
    app.UseAuthentication().UseAuthorization();

    app.UseEndpoints(ConfigureEndpoints.BlogEndpoints);
}