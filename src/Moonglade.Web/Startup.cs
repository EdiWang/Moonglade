#region Usings

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
using Moonglade.DataPorting;
using Moonglade.Model;
using Moonglade.Model.Settings;
using Moonglade.Syndication;
using Moonglade.Web.Authentication;
using Moonglade.Web.Extensions;
using Moonglade.Web.Middleware;
using Moonglade.Web.SiteIconGenerator;

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

            // Workaround stupid ASP.NET Core "by design" issue
            // https://github.com/aspnet/Configuration/issues/451
            _supportedCultures = _appSettings.GetSection("SupportedCultures")
                .GetChildren()
                .Select(p => new CultureInfo(p.Value))
                .ToList();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddBlogConfiguration(_appSettings);
            services.AddBlogCache();

            services.AddRateLimit(_configuration.GetSection("IpRateLimiting"));

            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(20);
                options.Cookie.HttpOnly = true;
            });

            services.AddApplicationInsightsTelemetry();
            services.AddBlogAuthenticaton(_configuration);

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
                const string csrfCookieName = "CSRF-TOKEN-MOONGLADE";
                options.Cookie.Name = $"X-{csrfCookieName}";
                options.FormFieldName = $"{csrfCookieName}-FORM";
            });

            services.AddPingback();
            services.AddImageStorage(_configuration, _environment);
            services.TryAddSingleton<IActionContextAccessor, ActionContextAccessor>();
            services.AddScoped<IBlogAudit, BlogAudit>();
            services.AddScoped<ISiteIconGenerator, FileSystemSiteIconGenerator>();
            services.AddScoped<IExportManager, ExportManager>();
            services.AddScoped<IFileSystemOpmlWriter, FileSystemOpmlWriter>();
            services.AddSessionBasedCaptcha();
            services.AddBlogServices();
            services.AddBlogNotification(_logger);

            services.AddDataStorage(_configuration.GetConnectionString(Constants.DbConnectionName));
        }

        public void Configure(
            IApplicationBuilder app,
            ILogger<Startup> logger,
            IHostApplicationLifetime appLifetime,
            TelemetryConfiguration configuration)
        {
            _logger = logger;

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

            app.UseRobotsTxt();

            app.UseRewriter(new RewriteOptions()
                .AddRedirect("(.*)/$", "$1")
                .AddRedirect("(index|default).(aspx?|htm|s?html|php|pl|jsp|cfm)", "/"));

            app.UseMiddleware<PoweredByMiddleware>();
            app.UseMiddleware<DNTMiddleware>();
            app.UseMiddleware<FirstRunMiddleware>();
            app.UseMiddleware<BlogGraphAPIGuardMiddleware>();

            if (_environment.IsDevelopment())
            {
                app.UseRouteDebugger();
                _logger.LogWarning($"Running in environment: {_environment.EnvironmentName}. Detailed error page enabled.");
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHttpsRedirection();
                app.UseHsts();
                app.UseExceptionHandler("/error");
                app.UseStatusCodePagesWithReExecute("/error", "?statusCode={0}");
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

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/ping", async context =>
                {
                    context.Response.Headers.Add("X-Moonglade-Version", Utils.AppVersion);
                    var obj = new
                    {
                        MoongladeVersion = Utils.AppVersion,
                        DotNetVersion = Environment.Version.ToString(),
                        EnvironmentTags = Utils.GetEnvironmentTags()
                    };

                    var json = System.Text.Json.JsonSerializer.Serialize(obj);
                    await context.Response.WriteAsync(json, Encoding.UTF8);
                });
                endpoints.MapControllerRoute(
                    "default",
                    "{controller=Home}/{action=Index}/{id?}");
                endpoints.MapRazorPages();
            });
        }
    }
}