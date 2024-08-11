using Edi.Captcha;
using Edi.PasswordGenerator;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Rewrite;
using Moonglade.Comments.Moderator;
using Moonglade.Data.MySql;
using Moonglade.Data.PostgreSql;
using Moonglade.Data.SqlServer;
using Moonglade.Email.Client;
using Moonglade.Mention.Common;
using Moonglade.Pingback;
using Moonglade.Syndication;
using Moonglade.Webmention;
using SixLabors.Fonts;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
using Encoder = Moonglade.Web.Configuration.Encoder;

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
var cultures = new[] { "en-US", "zh-Hans", "zh-Hant" }.Select(p => new CultureInfo(p)).ToList();

var builder = WebApplication.CreateBuilder(args);
builder.WriteParameterTable();

if (Helper.IsRunningOnAzureAppService())
{
    // https://learn.microsoft.com/en-us/aspnet/core/fundamentals/logging/?view=aspnetcore-8.0#azure-app-service
    builder.Logging.AddAzureWebAppDiagnostics();
}

builder.Configuration.AddJsonFile("manifesticons.json", false, true);

ConfigureServices(builder.Services);

var app = builder.Build();

await app.DetectChina();
await app.InitStartUp();

ConfigureMiddleware();

app.Run();

void ConfigureServices(IServiceCollection services)
{
    AppDomain.CurrentDomain.Load("Moonglade.Core");
    AppDomain.CurrentDomain.Load("Moonglade.FriendLink");
    AppDomain.CurrentDomain.Load("Moonglade.Theme");
    AppDomain.CurrentDomain.Load("Moonglade.Configuration");
    AppDomain.CurrentDomain.Load("Moonglade.Data");
    AppDomain.CurrentDomain.Load("Moonglade.Webmention");

    services.AddMediatR(config => config.RegisterServicesFromAssemblies(AppDomain.CurrentDomain.GetAssemblies()));
    services.AddOptions()
            .AddHttpContextAccessor();

    var magic0 = Encoding.UTF8.GetString(BitConverter.GetBytes('✔'.GetHashCode())
                        .Zip(BitConverter.GetBytes(0x242F2E32)).Select(x => (byte)(x.First + x.Second)).ToArray());
    var magic1 = Convert.ToBase64String(SHA256.Create().ComputeHash(BitConverter.GetBytes(0x6B441)))[11..15];
    var magic2 = Convert.ToBase64String(SHA256.Create().ComputeHash(BitConverter.GetBytes(0x7DB14)))[21..25];
    var magic3 = Convert.ToBase64String(SHA256.Create().ComputeHash(BitConverter.GetBytes(0x78E10)))[13..17];
    var magic4 = Convert.ToBase64String(SHA256.Create().ComputeHash(BitConverter.GetBytes(0x873B3)))[27..32];

    services.AddSession(options =>
    {
        options.IdleTimeout = TimeSpan.FromMinutes(20);
        options.Cookie.HttpOnly = true;
    }).AddSessionBasedCaptcha(options =>
    {
        options.FontStyle = FontStyle.Bold;
        options.BlockedCodes = [magic0, magic1, magic2, magic3, magic4];
    });

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
        options.AppendTrailingSlash = false;
    });

    services.AddTransient<IPasswordGenerator, DefaultPasswordGenerator>();

    services.AddHealthChecks();

    services.AddMentionCommon()
            .AddPingback()
            .AddWebmention();

    services.AddSyndication()
            .AddInMemoryCacheAside()
            .AddScoped<ValidateCaptcha>()
            .AddScoped<ITimeZoneResolver, BlogTimeZoneResolver>()
            .AddBlogConfig()
            .AddBlogAuthenticaton(builder.Configuration)
            .AddImageStorage(builder.Configuration, options => options.ContentRootPath = builder.Environment.ContentRootPath)
            .Configure<List<ManifestIcon>>(builder.Configuration.GetSection("ManifestIcons"));

    services.AddEmailClient();
    services.AddContentModerator(builder.Configuration);

    services.AddSingleton<CannonService>();

    string dbType = builder.Configuration.GetConnectionString("DatabaseType");
    string connStr = builder.Configuration.GetConnectionString("MoongladeDatabase");
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

void ConfigureMiddleware()
{
    bool useXFFHeaders = app.Configuration.GetSection("ForwardedHeaders:Enabled").Get<bool>();
    if (useXFFHeaders) app.UseSmartXFFHeader();

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

    app.UseWhen(
        _ => bc.AdvancedSettings.EnableFoaf,
        appBuilder => appBuilder.UseMiddleware<FoafMiddleware>()
    );

    app.UseWhen(
        ctx => bc.AdvancedSettings.EnableSiteMap && ctx.Request.Path == "/sitemap.xml",
        appBuilder => appBuilder.UseMiddleware<SiteMapMiddleware>()
    );

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

    var options = new RewriteOptions().AddRedirect("(.*)/$", "$1", 301);
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
}