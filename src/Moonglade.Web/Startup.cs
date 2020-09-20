#region Usings

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using AspNetCoreRateLimit;
using Edi.Captcha;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.Implementation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moonglade.Auditing;
using Moonglade.Core;
using Moonglade.Core.Caching;
using Moonglade.Core.Notification;
using Moonglade.DataPorting;
using Moonglade.Model;
using Moonglade.Model.Settings;
using Moonglade.Syndication;
using Moonglade.Web.Authentication;
using Moonglade.Web.Extensions;
using Moonglade.Web.Filters;
using Moonglade.Web.Middleware;
using Moonglade.Web.SiteIconGenerator;
using Polly;

#endregion

namespace Moonglade.Web
{
    public class Startup
    {
        private ILogger<Startup> _logger;
        private readonly IConfigurationSection _appSettings;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;
        private readonly IList<CultureInfo> _supportedCultures;

        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            _configuration = configuration;
            _environment = env;
            _appSettings = _configuration.GetSection(nameof(AppSettings));
            _supportedCultures = new[] { "en-US", "zh-CN" }.Select(p => new CultureInfo(p)).ToList();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddBlogConfiguration(_appSettings);
            services.AddMemoryCache();
            services.AddRateLimit(_configuration.GetSection("IpRateLimiting"));

            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(20);
                options.Cookie.HttpOnly = true;
            });

            services.AddApplicationInsightsTelemetry();
            services.AddMoongladeAuthenticaton(_configuration);

            services.AddLocalization(options => options.ResourcesPath = "Resources");
            services.AddMvc(options =>
                            options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()))
                    .AddViewLocalization()
                    .AddDataAnnotationsLocalization();

            services.Configure<RequestLocalizationOptions>(options =>
            {
                options.DefaultRequestCulture = new RequestCulture("en-US");
                options.SupportedCultures = _supportedCultures;
                options.SupportedUICultures = _supportedCultures;
            });

            services.AddAntiforgery(options =>
            {
                const string cookieBaseName = "CSRF-TOKEN-MOONGLADE";
                options.Cookie.Name = $"X-{cookieBaseName}";
                options.FormFieldName = $"{cookieBaseName}-FORM";
            });

            services.AddPingback();
            services.AddImageStorage(_configuration, _environment);
            services.TryAddSingleton<IActionContextAccessor, ActionContextAccessor>();
            services.AddScoped<IBlogAudit, BlogAudit>();
            services.AddScoped<DeleteSubscriptionCache>();
            services.AddScoped<ISiteIconGenerator, FileSystemSiteIconGenerator>();

            services.AddScoped<IExportManager, ExportManager>();
            services.AddScoped<IFileSystemOpmlWriter, FileSystemOpmlWriter>();
            services.AddSingleton<IBlogCache, BlogCache>();
            services.AddSessionBasedCaptcha();

            var asm = Assembly.GetAssembly(typeof(BlogService));
            if (null != asm)
            {
                var types = asm.GetTypes().Where(t => t.IsClass && t.IsPublic && t.Name.EndsWith("Service"));
                foreach (var t in types)
                {
                    services.AddScoped(t, t);
                }
            }

            services.AddHttpClient<IMoongladeNotificationClient, NotificationClient>()
                    .AddTransientHttpErrorPolicy(builder =>
                            builder.WaitAndRetryAsync(3, retryCount =>
                            TimeSpan.FromSeconds(Math.Pow(2, retryCount)),
                                (result, span, retryCount, context) =>
                                {
                                    _logger?.LogWarning($"Request failed with {result.Result.StatusCode}. Waiting {span} before next retry. Retry attempt {retryCount}/3.");
                                }));

            services.AddDataStorage(_configuration.GetConnectionString(Constants.DbConnectionName));
        }

        public void Configure(
            IApplicationBuilder app,
            ILogger<Startup> logger,
            IHostApplicationLifetime appLifetime,
            TelemetryConfiguration configuration)
        {
            _logger = logger;
            var enforceHttps = bool.Parse(_appSettings["EnforceHttps"]);
            var allowExtScripts = bool.Parse(_appSettings[nameof(AppSettings.AllowExternalScripts)]);

            // Support Chinese contents
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            if (!_environment.IsProduction())
            {
                _logger.LogWarning($"Running in environment: {_environment.EnvironmentName}. Application Insights disabled.");

                configuration.DisableTelemetry = true;
                TelemetryDebugWriter.IsTracingDisabled = true;
            }

            appLifetime.ApplicationStarted.Register(() =>
            {
                _logger.LogInformation("Moonglade started.");
            });
            appLifetime.ApplicationStopping.Register(() =>
            {
                _logger.LogInformation("Moonglade is stopping...");
            });
            appLifetime.ApplicationStopped.Register(() =>
            {
                _logger.LogInformation("Moonglade stopped.");
            });

            app.UseSecurityHeaders(new HeaderPolicyCollection()
                .AddFrameOptionsSameOrigin()
                .AddXssProtectionEnabled()
                .AddContentTypeOptionsNoSniff()
                .AddContentSecurityPolicy(csp =>
                {
                    if (enforceHttps)
                    {
                        csp.AddUpgradeInsecureRequests();
                    }
                    csp.AddFormAction().Self();

                    if (!allowExtScripts)
                    {
                        csp.AddScriptSrc()
                            .Self()
                            .UnsafeInline()
                            .UnsafeEval()
                            // Whitelist Azure Application Insights
                            .From("https://*.vo.msecnd.net")
                            .From("https://*.services.visualstudio.com");
                    }
                })
                .AddFeaturePolicy(builder =>
                {
                    builder.AddCamera().None();
                    builder.AddMicrophone().None();
                    builder.AddPayment().None();
                    builder.AddUsb().None();
                    builder.AddAccelerometer().None();
                })
                .RemoveServerHeader()
            );

            app.UseRobotsTxt();

            TryUseUrlRewrite(app);
            app.UseMiddleware<PoweredByMiddleware>();
            app.UseMiddleware<DNTMiddleware>();
            app.UseMiddleware<FirstRunMiddleware>();

            if (_environment.IsDevelopment())
            {
                app.UseRouteDebugger();
                _logger.LogWarning($"Running in environment: {_environment.EnvironmentName}. Detailed error page enabled.");
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/error");
                app.UseStatusCodePagesWithReExecute("/error", "?statusCode={0}");
            }

            if (enforceHttps)
            {
                _logger.LogInformation("HTTPS is enforced.");
                app.UseHttpsRedirection();
                app.UseHsts();
            }

            app.UseRequestLocalization(new RequestLocalizationOptions
            {
                DefaultRequestCulture = new RequestCulture("en-US"),
                SupportedCultures = _supportedCultures,
                SupportedUICultures = _supportedCultures
            });

            app.UseStaticFiles();
            app.UseSession();

            app.UseIpRateLimiting();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.Use(async (context, next) =>
            {
                if (context.Request.Path.StartsWithSegments("/api")
                    && !bool.Parse(_appSettings[nameof(AppSettings.EnableWebApi)]))
                {
                    context.Response.StatusCode = StatusCodes.Status501NotImplemented;
                    await context.Response.WriteAsync("API is disabled", Encoding.UTF8);
                }
                else
                {
                    await next.Invoke();
                }
            });

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/ping", async context =>
                {
                    context.Response.Headers.Add("X-Moonglade-Version", Utils.AppVersion);
                    await context.Response.WriteAsync(
                        $"Moonglade Version: {Utils.AppVersion}, .NET Core {Environment.Version}", Encoding.UTF8);
                });
                endpoints.MapControllerRoute(
                    "default",
                    "{controller=Home}/{action=Index}/{id?}");
                endpoints.MapRazorPages();
            });
        }

        private void TryUseUrlRewrite(IApplicationBuilder app)
        {
            try
            {
                var options = new RewriteOptions()
                    .AddRedirect("(.*)/$", "$1")
                    .AddRedirect("(index|default).(aspx?|htm|s?html|php|pl|jsp|cfm)", "/");

                app.UseRewriter(options);
            }
            catch (Exception e)
            {
                _logger.LogError(e, nameof(TryUseUrlRewrite));
            }
        }
    }
}