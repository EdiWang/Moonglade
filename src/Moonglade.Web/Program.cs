using Edi.Captcha;
using Edi.PasswordGenerator;

using Microsoft.AspNetCore.Rewrite;

using Moonglade.Comments.Moderator;
using Moonglade.Data.MySql;
using Moonglade.Data.PostgreSql;
using Moonglade.Data.SqlServer;
using Moonglade.Email.Client;
using Moonglade.Mention.Common;
using Moonglade.Pingback;
using Moonglade.Setup;
using Moonglade.Syndication;
using Moonglade.Web.Handlers;
using Moonglade.Webmention;

using SixLabors.Fonts;

using System.Globalization;
using System.Text.Json.Serialization;
using Moonglade.IndexNow.Client;
using Encoder = Moonglade.Web.Configuration.Encoder;

AppDomain.CurrentDomain.Load("Moonglade.Setup");
AppDomain.CurrentDomain.Load("Moonglade.Core");
AppDomain.CurrentDomain.Load("Moonglade.FriendLink");
AppDomain.CurrentDomain.Load("Moonglade.Theme");
AppDomain.CurrentDomain.Load("Moonglade.Configuration");
AppDomain.CurrentDomain.Load("Moonglade.Data");
AppDomain.CurrentDomain.Load("Moonglade.Webmention");

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

var cultures = new[] { "en-US", "zh-Hans", "zh-Hant", "de-DE" }.Select(p => new CultureInfo(p)).ToList();

var builder = WebApplication.CreateBuilder(args);
builder.WriteParameterTable();

if (Helper.IsRunningOnAzureAppService())
{
    // https://learn.microsoft.com/en-us/aspnet/core/fundamentals/logging/?view=aspnetcore-8.0#azure-app-service
    builder.Logging.AddAzureWebAppDiagnostics();
}

var services = builder.Services;

services.AddMediatR(config => config.RegisterServicesFromAssemblies(AppDomain.CurrentDomain.GetAssemblies()));
services.AddOptions()
        .AddHttpContextAccessor();

services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(20);
    options.Cookie.HttpOnly = true;
}).AddSessionBasedCaptcha(options =>
{
    List<string> magics = [
        Encoding.UTF8.GetString(BitConverter.GetBytes('✔'.GetHashCode())
                .Zip(BitConverter.GetBytes(0x242F2E32)).Select(x => (byte)(x.First + x.Second)).ToArray()),
            Helper.GetMagic(0x6B441,11,15),
            Helper.GetMagic(0x1499E, 10, 14)
    ];

    if (bool.Parse(builder.Configuration["BlockPRCFuryCode"]!))
    {
        magics.AddRange([
                Helper.GetMagic(0x7DB14,21,25),
                Helper.GetMagic(0x78E10,13,17),
                Helper.GetMagic(0x17808,34,38),
                Helper.GetMagic(0x1B5ED,4,8),
                Helper.GetMagic(0x9CFB,25,29),
                "NMSL", "CNMB", "MDZZ", "TNND"
            ]);
    }

    options.FontStyle = FontStyle.Bold;
    options.BlockedCodes = magics.ToArray();
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
        .AddImageStorage(builder.Configuration, options => options.ContentRootPath = builder.Environment.ContentRootPath);

services.AddEmailClient();
services.AddIndexNowClient();
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

services.AddTransient<ISiteIconInitializer, SiteIconInitializer>();
services.AddScoped<IMigrationManager, MigrationManager>();
services.AddScoped<IBlogConfigInitializer, BlogConfigInitializer>();
services.AddScoped<IStartUpInitializer, StartUpInitializer>();

var app = builder.Build();

if (!app.Environment.IsDevelopment() && await Helper.IsRunningInChina())
{
    app.Logger.LogCritical("Positive China detection, application stopped.");
    await app.StopAsync();

    return 251;
}

await app.InitStartUp();

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

app.MapGet("/robots.txt", RobotsTxtMapHandler.Handler);
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

app.Run();

return 0;
