using Edi.CacheAside.InMemory;
using LiteBus.Commands.Abstractions;
using LiteBus.Queries.Abstractions;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Auth;
using Moonglade.Configuration;
using Moonglade.Web.Configuration;
using Moonglade.Web.Controllers;
using Moonglade.Web.Extensions;
using Moonglade.Web.Services;
using Moq;
using System.Globalization;
using System.Net;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace Moonglade.Web.Tests.TestSupport;

internal sealed class LocalAccountHttpFixture : IAsyncDisposable
{
    private readonly Dictionary<string, string> _cookies = [];
    public ConfigurationCommandRecorder Store { get; }
    public WebApplication App { get; }
    public HttpClient Client { get; private set; } = null!;
    public string PathBase { get; }
    public IPAddress RemoteIpAddress { get; set; } = IPAddress.Parse("192.0.2.90");
    public string UserAgent { get; set; } = "B2-test-client";
    public int BusinessExecutions { get; private set; }

    private LocalAccountHttpFixture(WebApplication app, ConfigurationCommandRecorder store, string pathBase)
    {
        App = app;
        Store = store;
        PathBase = pathBase;
    }

    public static async Task<LocalAccountHttpFixture> CreateAsync(
        bool required = false, string pathBase = "", string environment = "Production",
        int permits = 100, bool legacyOidcConfiguration = false, HttpMessageHandler? oidcBackchannel = null,
        TimeProvider? timeProvider = null)
    {
        var ct = TestContext.Current.CancellationToken;
        var store = new ConfigurationCommandRecorder();
        var config = new BlogConfig(BlogConfig.Parse(new Dictionary<string, string>()).Snapshot);
        store.Initialize(config);
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "src", "Moonglade.Web")))
            directory = directory.Parent;
        var root = Path.Combine(directory?.FullName ?? throw new DirectoryNotFoundException(), "src", "Moonglade.Web");
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ApplicationName = typeof(Program).Assembly.GetName().Name,
            ContentRootPath = root,
            WebRootPath = Path.Combine(root, "wwwroot"),
            EnvironmentName = environment
        });
        builder.Configuration.Sources.Clear();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Authentication:Totp:Required"] = required.ToString()
        });
        if (legacyOidcConfiguration)
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Provider"] = "OpenIdConnect",
                ["Authentication:OpenIdConnect:Authority"] = "https://identity.example.test",
                ["Authentication:OpenIdConnect:ClientId"] = "synthetic-client",
                ["Authentication:OpenIdConnect:AllowedSubjects:0"] = "exact-subject"
            });
        builder.Logging.ClearProviders();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton(store.Db);
        builder.Services.AddSingleton<IBlogConfig>(config);
        builder.Services.AddSingleton<ICommandMediator>(store);
        builder.Services.AddSingleton(store.EventMediator);
        builder.Services.AddSingleton(Mock.Of<IQueryMediator>());
        builder.Services.AddInMemoryCacheAside();
        builder.Services.AddSingleton<ThemeCacheInvalidator>();
        builder.Services.AddSingleton<PostCacheInvalidator>();
        builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
        builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
        builder.Services.AddBlogAuthentication(builder.Configuration);
        if (timeProvider is not null)
            builder.Services.Configure<CookieAuthenticationOptions>(BlogAuthSchemas.LocalAccountRememberedDevice,
                options => options.TimeProvider = timeProvider);
        if (oidcBackchannel is not null)
            builder.Services.AddHttpClient("OwnerOidc").ConfigurePrimaryHttpMessageHandler(() => oidcBackchannel);
        builder.Services.AddProblemDetails();
        builder.Services.AddResponseCompression();
        builder.Services.AddHttpsRedirection(options => options.HttpsPort = 443);
        builder.Services.Configure<LocalAccountRateLimitOptions>(options => options.PermitLimit = permits);
        builder.Services.AddSingleton<LocalAccountRateLimitPolicy>();
        builder.Services.AddRateLimiter(options =>
            options.AddPolicy<string, LocalAccountRateLimitPolicy>(LocalAccountRateLimitPolicy.PolicyName));
        builder.Services.AddAntiforgery(options =>
        {
            options.Cookie.Name = "X-CSRF-TOKEN-MOONGLADE";
            options.FormFieldName = "CSRF-TOKEN-MOONGLADE-FORM";
            options.HeaderName = "XSRF-TOKEN";
        });
        builder.Services.AddControllers(options => options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()))
            .ConfigureApplicationPartManager(manager => manager.FeatureProviders.Add(new AccountOnlyFeatureProvider()));
        builder.Services.AddRazorPages(options =>
            options.Conventions.AuthorizeFolder("/Admin", BlogAuthSchemas.AdministratorPolicy));
        var app = builder.Build();
        var fixture = new LocalAccountHttpFixture(app, store, pathBase);
        if (pathBase.Length != 0) app.UsePathBase(pathBase);
        app.Use((context, next) =>
        {
            context.Connection.RemoteIpAddress = fixture.RemoteIpAddress;
            context.Response.OnStarting(() =>
            {
                context.Response.Headers.ContentLanguage = CultureInfo.CurrentUICulture.Name;
                return Task.CompletedTask;
            });
            return next(context);
        });
        app.UseMoongladeRequestPipeline([new("en-US"), new("de-DE"), new("zh-Hans")]);
        app.MapStaticAssets().ShortCircuit();
        app.MapControllers();
        app.MapRazorPages();
        foreach (var route in new[] { "/", "/post/example", "/page/example", "/admin/probe" })
        {
            var endpoint = app.MapMethods(route, ["GET", "HEAD"], () =>
            {
                fixture.BusinessExecutions++;
                return Results.Text("business-reached");
            }).WithMetadata(new PageActionDescriptor()).WithOrder(-100);
            if (route == "/admin/probe") endpoint.RequireAuthorization(BlogAuthSchemas.AdministratorPolicy);
        }
        foreach (var route in new[]
                 { "/api/probe", "/feed/rss", "/feed/atom", "/category/sample/rss", "/webmention", "/foaf.xml", "/robots.txt",
                     "/sitemap.xml", "/opensearch", "/verification.txt", "/auth/setup-account-extra", "/health/extra" })
            app.MapMethods(route, ["GET", "HEAD", "POST"], () =>
            {
                fixture.BusinessExecutions++;
                return Results.Text("business-reached");
            });
        app.MapGet("/health", () => Results.Text("live"));
        app.MapGet("/health/ready", () => Results.Text("ready"));
        app.MapGet("/admin/security-token", (HttpContext context, IAntiforgery antiforgery) =>
                Results.Text(antiforgery.GetAndStoreTokens(context).RequestToken!))
            .RequireAuthorization(BlogAuthSchemas.AdministratorPolicy);
        await app.StartAsync(ct);
        fixture.Client = app.GetTestClient();
        fixture.Client.BaseAddress = new Uri("https://localhost");
        return fixture;
    }

    public async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path,
        HttpContent? content = null, string? antiforgery = null, string? language = null, bool https = true)
    {
        using var request = new HttpRequestMessage(method, new Uri($"{(https ? "https" : "http")}://localhost{PathBase}{path}"))
        { Content = content };
        request.Headers.UserAgent.ParseAdd(UserAgent);
        if (_cookies.Count != 0) request.Headers.Add("Cookie", string.Join("; ", _cookies.Select(pair => $"{pair.Key}={pair.Value}")));
        if (antiforgery is not null) request.Headers.Add("XSRF-TOKEN", antiforgery);
        if (language is not null) request.Headers.AcceptLanguage.ParseAdd(language);
        var response = await Client.SendAsync(request, TestContext.Current.CancellationToken);
        if (response.Headers.TryGetValues("Set-Cookie", out var headers))
            foreach (var header in headers)
            {
                var pair = header.Split(';', 2)[0].Split('=', 2);
                _cookies[pair[0]] = pair[1];
            }
        return response;
    }

    public Task<string> SetupTokenAsync() => FormTokenAsync("/auth/setup-account");

    public async Task<string> FormTokenAsync(string path)
    {
        using var response = await SendAsync(HttpMethod.Get, path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var match = Regex.Match(body, "name=\"CSRF-TOKEN-MOONGLADE-FORM\"[^>]*value=\"([^\"]+)\"");
        Assert.True(match.Success, "The compiled form must supply the antiforgery token used for the HTTP request.");
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }

    public void SetTicket(ClaimsPrincipal principal, string scheme, DateTimeOffset? expires = null)
    {
        var options = App.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>().Get(scheme);
        var ticket = new AuthenticationTicket(principal, new AuthenticationProperties
        {
            IssuedUtc = DateTimeOffset.UtcNow.AddMinutes(-1),
            ExpiresUtc = expires ?? DateTimeOffset.UtcNow.AddMinutes(5)
        }, scheme);
        _cookies[options.Cookie.Name!] = options.TicketDataFormat.Protect(ticket);
    }

    public AuthenticationTicket ReadTicket(string scheme)
    {
        var options = App.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>().Get(scheme);
        return options.TicketDataFormat.Unprotect(_cookies[options.Cookie.Name!])!;
    }

    public void CorruptTicket(string scheme)
    {
        var options = App.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>().Get(scheme);
        _cookies[options.Cookie.Name!] = "tampered";
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await App.DisposeAsync();
        Store.Dispose();
    }

    private sealed class AccountOnlyFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
    {
        public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
        {
            foreach (var controller in feature.Controllers.Where(type => type.AsType() != typeof(AccountController) &&
                type.AsType() != typeof(OwnerAvatarController) && type.AsType() != typeof(AuthController)).ToArray())
                feature.Controllers.Remove(controller);
        }
    }

}
