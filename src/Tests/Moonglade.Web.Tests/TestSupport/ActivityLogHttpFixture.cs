using Edi.CacheAside.InMemory;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Auth;
using Moonglade.Configuration;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Web.Controllers;
using Moonglade.Web.Filters;
using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json.Serialization;

namespace Moonglade.Web.Tests.TestSupport;

public sealed class ActivityLogHttpFixture(WebApplication app, ActivityLogTestStore store) : IAsyncDisposable
{
    public WebApplication App { get; } = app;
    public ActivityLogTestStore Store { get; } = store;

    public static async Task<ActivityLogHttpFixture> CreateAsync(int historyCount = 3, CancellationToken ct = default, bool includeSettings = false)
    {
        var store = await ActivityLogTestStore.CreateAsync(historyCount, ct);
        if (includeSettings)
        {
            using var scope = store.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();
            foreach (var property in typeof(BlogSettingsSnapshot).GetProperties())
                db.BlogConfiguration.Add(new BlogConfigurationEntity
                {
                    CfgKey = property.Name,
                    CfgValue = property.GetValue(store.Config.Capture()).ToJson(),
                    LastModifiedTimeUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                });
            await db.SaveChangesAsync(ct);
        }
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "src", "Moonglade.Web", "Moonglade.Web.csproj")))
            directory = directory.Parent;
        var root = Path.Combine(directory?.FullName ?? throw new DirectoryNotFoundException("Moonglade repository root not found."),
            "src", "Moonglade.Web");
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ApplicationName = typeof(Program).Assembly.GetName().Name,
            ContentRootPath = root,
            WebRootPath = Path.Combine(root, "wwwroot"),
            EnvironmentName = "Testing"
        });
        builder.WebHost.UseTestServer();
        builder.Configuration.Sources.Clear();
        builder.Logging.ClearProviders();
        store.ConfigureServices(builder.Services);
        builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
        builder.Services.AddInMemoryCacheAside();
        builder.Services.AddSingleton<ThemeCacheInvalidator>();
        builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
        builder.Services.AddBlogAuthentication(new ConfigurationBuilder().Build());
        builder.Services.AddAntiforgery(options =>
        {
            options.Cookie.Name = "X-CSRF-TOKEN-MOONGLADE";
            options.FormFieldName = "CSRF-TOKEN-MOONGLADE-FORM";
            options.HeaderName = "XSRF-TOKEN";
        });
        builder.Services.AddAuthentication("B1Test")
            .AddScheme<AuthenticationSchemeOptions, SyntheticAuthenticationHandler>("B1Test", _ => { });
        builder.Services.AddControllers(options =>
            {
                options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
                options.Filters.Add<BlogSettingsFilter>();
            })
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
            .ConfigureApplicationPartManager(manager => manager.FeatureProviders.Add(new ActivityOnlyFeatureProvider(includeSettings)));
        builder.Services.AddRazorPages(options => options.Conventions.AuthorizeFolder("/Admin", BlogAuthSchemas.AdministratorPolicy));
        var app = builder.Build();
        app.UseRequestLocalization(new RequestLocalizationOptions()
            .SetDefaultCulture("en-US")
            .AddSupportedCultures("en-US", "zh-Hans", "zh-Hant", "de-DE", "ja-JP", "fr-FR")
            .AddSupportedUICultures("en-US", "zh-Hans", "zh-Hant", "de-DE", "ja-JP", "fr-FR"));
        app.Use((context, next) =>
        {
            context.Connection.RemoteIpAddress = IPAddress.Parse("192.0.2.25");
            return next(context);
        });
        app.UseStaticFiles();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        app.MapRazorPages();
        await app.StartAsync(ct);
        return new ActivityLogHttpFixture(app, store);
    }

    public async ValueTask DisposeAsync()
    {
        await App.DisposeAsync();
        await Store.DisposeAsync();
    }

    private sealed class ActivityOnlyFeatureProvider(bool includeSettings) : IApplicationFeatureProvider<ControllerFeature>
    {
        public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
        {
            foreach (var controller in feature.Controllers.Where(type => type.AsType() != typeof(ActivityLogController)
                         && (!includeSettings || type.AsType() != typeof(SettingsController))).ToArray())
                feature.Controllers.Remove(controller);
        }
    }

    private sealed class SyntheticAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue("X-B1-User", out var name))
                return Task.FromResult(AuthenticateResult.NoResult());
            List<Claim> claims = [new(ClaimTypes.Name, name.ToString())];
            if (name == "admin") claims.Add(new Claim(ClaimTypes.Role, "Administrator"));
            var identity = new ClaimsIdentity(claims, Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
        }
    }
}
