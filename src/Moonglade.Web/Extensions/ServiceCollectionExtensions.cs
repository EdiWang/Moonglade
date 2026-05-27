using Edi.AspNetCore.Utils.Filters;
using Edi.Captcha;
using Edi.PasswordGenerator;
using LiteBus.Commands;
using LiteBus.Events;
using LiteBus.Extensions.Microsoft.DependencyInjection;
using LiteBus.Queries;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moonglade.BackgroundServices;
using Moonglade.Data.PostgreSql;
using Moonglade.Data.SqlServer;
using Moonglade.Email.Client;
using Moonglade.IndexNow.Client;
using Moonglade.Moderation;
using Moonglade.Setup;
using Moonglade.Syndication;
using Moonglade.Webmention;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Encodings.Web;
using System.Text.Json.Serialization;
using System.Text.Unicode;

namespace Moonglade.Web.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMoongladeWebServices(
        this IServiceCollection services,
        IConfiguration configuration,
        List<CultureInfo> cultures)
    {
        services.AddMoongladeLiteBus();
        services.AddHttpClient();
        services.AddOptions().AddHttpContextAccessor();
        services.AddMoongladeCaptcha(configuration);
        services.AddMoongladeLocalization();
        services.AddMoongladeControllers();
        services.AddMoongladeRazorPages();
        services.AddSingleton(HtmlEncoder.Create(UnicodeRanges.All));
        services.AddMoongladeAntiforgery();
        services.AddMoongladeRequestLocalization(cultures);
        services.AddMoongladeRouteOptions();
        services.AddTransient<IPasswordGenerator, DefaultPasswordGenerator>();
        services.AddMoongladeHealthChecks();
        services.AddMoongladeProblemDetails();
        services.AddMoongladeCoreServices(configuration);
        services.AddMoongladeDatabase(configuration);
        services.AddMoongladeInitializers();

        return services;
    }

    private static IServiceCollection AddMoongladeLiteBus(this IServiceCollection services)
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

        return services;
    }

    private static IServiceCollection AddMoongladeCaptcha(this IServiceCollection services, IConfiguration configuration)
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
            options.FontStyle = CaptchaFontStyle.Regular;
            options.BlockedCodes = [.. magics];
            options.DrawLines = true;
        });

        services.AddScoped<ValidateCaptcha>();
        return services;
    }

    private static IServiceCollection AddMoongladeLocalization(this IServiceCollection services)
    {
        services.AddLocalization(options => options.ResourcesPath = "Resources");
        return services;
    }

    private static IServiceCollection AddMoongladeControllers(this IServiceCollection services)
    {
        services.AddControllers(options =>
            {
                options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
                options.Filters.Add<ProblemDetailsResultFilter>();
            })
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        return services;
    }

    private static IServiceCollection AddMoongladeRazorPages(this IServiceCollection services)
    {
        services.AddRazorPages()
            .AddDataAnnotationsLocalization(options =>
                options.DataAnnotationLocalizerProvider = (_, factory) => factory.Create(typeof(Program)))
            .AddRazorPagesOptions(options =>
            {
                options.Conventions.AddPageRoute("/Admin/Post", "admin");
                options.Conventions.AuthorizeFolder("/Admin");
            })
            .AddViewOptions(options =>
            {
                // Fix '__Invariant' form input rendering issue
                options.HtmlHelperOptions.FormInputRenderMode = FormInputRenderMode.AlwaysUseCurrentCulture;
            });

        return services;
    }

    private static IServiceCollection AddMoongladeAntiforgery(this IServiceCollection services)
    {
        services.AddAntiforgery(options =>
        {
            const string csrfName = "CSRF-TOKEN-MOONGLADE";
            options.Cookie.Name = $"X-{csrfName}";
            options.FormFieldName = $"{csrfName}-FORM";
            options.HeaderName = "XSRF-TOKEN";
        });

        return services;
    }

    private static IServiceCollection AddMoongladeRequestLocalization(this IServiceCollection services, List<CultureInfo> cultures)
    {
        services.Configure<RequestLocalizationOptions>(options =>
        {
            options.DefaultRequestCulture = new("en-US");
            options.SupportedCultures = cultures;
            options.SupportedUICultures = cultures;
        });

        return services;
    }

    private static IServiceCollection AddMoongladeRouteOptions(this IServiceCollection services)
    {
        services.Configure<RouteOptions>(options =>
        {
            options.LowercaseUrls = true;
            options.AppendTrailingSlash = false;
        });

        return services;
    }

    private static IServiceCollection AddMoongladeHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy("Application is running"));

        return services;
    }

    private static IServiceCollection AddMoongladeProblemDetails(this IServiceCollection services)
    {
        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                context.ProblemDetails.Instance = context.HttpContext.Request.Path;
                context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
            };
        });

        return services;
    }

    private static IServiceCollection AddMoongladeCoreServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddWebmention();

        services.AddSyndication()
                .AddInMemoryCacheAside()
                .AddBlogConfig()
                .AddBlogAuthenticaton(configuration)
                .AddImageStorage(configuration);

        services.AddEmailClient();
        services.AddIndexNowClient(configuration.GetSection("IndexNow"));
        services.AddContentModerator(configuration);

        services.AddSingleton<ScheduledPublishWakeUp>();
        services.AddHostedService<ScheduledPublishService>();

        services.AddSingleton<CannonService>();
        services.AddHostedService(sp => sp.GetRequiredService<CannonService>());

        services.AddSingleton<UpdateCheckerState>();
        services.AddHttpClient<IGitHubReleaseClient, GitHubReleaseClient>(client =>
        {
            client.BaseAddress = new Uri("https://api.github.com");
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Moonglade", VersionHelper.AppVersionBasic));
        });
        services.AddHostedService<UpdateCheckService>();

        return services;
    }

    private static IServiceCollection AddMoongladeDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connStr = configuration.GetConnectionString("MoongladeDatabase");
        var dbType = configuration.GetConnectionString("DatabaseProvider")
            ?? throw new InvalidOperationException("ConnectionStrings:DatabaseProvider is not configured.");

        if (string.Equals(dbType, "postgresql", StringComparison.OrdinalIgnoreCase))
        {
            services.AddPostgreSqlStorage(connStr!);
        }
        else if (string.Equals(dbType, "sqlserver", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSqlServerStorage(connStr!);
        }
        else
        {
            throw new NotSupportedException($"Unknown database type '{dbType}', please check connection string.");
        }

        return services;
    }

    private static IServiceCollection AddMoongladeInitializers(this IServiceCollection services)
    {
        services.AddTransient<ISiteIconBuilder, SiteIconBuilder>();
        services.AddScoped<IMigrationManager, MigrationManager>();
        services.AddScoped<IConfigInitializer, ConfigInitializer>();
        services.AddScoped<IStartUpInitializer, StartUpInitializer>();

        return services;
    }
}