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
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moonglade.Auth;
using Moonglade.Configuration;
using Moonglade.Configuration.Settings;
using Moonglade.Utils;
using Moonglade.Web.Configuration;
using Moonglade.Web.Middleware;
using Moonglade.Web.Models;

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

            // Workaround stupid ASP.NET "by design" issue
            // https://github.com/aspnet/Configuration/issues/451
            _supportedCultures = _appSettings.GetSection("SupportedCultures")
                .GetChildren()
                .Select(p => new CultureInfo(p.Value))
                .ToList();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            // ASP.NET Setup
            services.AddRateLimit(_configuration.GetSection("IpRateLimiting"));
            services.AddApplicationInsightsTelemetry();
            services.AddAzureAppConfiguration();

            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(20);
                options.Cookie.HttpOnly = true;
            });
            services.AddSessionBasedCaptcha();
            services.TryAddSingleton<IActionContextAccessor, ActionContextAccessor>();
            services.AddLocalization(options => options.ResourcesPath = "Resources");
            services.AddMvc(options => options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()))
                    .AddViewLocalization()
                    .AddDataAnnotationsLocalization();

            services.Configure<RequestLocalizationOptions>(options =>
            {
                options.DefaultRequestCulture = new("en-US");
                options.SupportedCultures = _supportedCultures;
                options.SupportedUICultures = _supportedCultures;
            });

            services.AddAntiforgery(options =>
            {
                const string csrfName = "CSRF-TOKEN-MOONGLADE";
                options.Cookie.Name = $"X-{csrfName}";
                options.FormFieldName = $"{csrfName}-FORM";
                options.HeaderName = "XSRF-TOKEN";
            });

            // Blog Services
            services.AddBlogServices();
            services.Configure<List<BlogTheme>>(_configuration.GetSection("Themes"));
            services.Configure<List<ManifestIcon>>(_configuration.GetSection("ManifestIcons"));
            services.Configure<List<TagNormalization>>(_configuration.GetSection("TagNormalization"));
            services.AddBlogConfiguration(_appSettings);
            services.AddBlogAuthenticaton(_configuration);
            services.AddComments(_configuration);
            services.AddNotification(_logger);
            services.AddDataStorage(_configuration.GetConnectionString("MoongladeDatabase"));
            services.AddImageStorage(_configuration, options =>
            {
                options.ContentRootPath = _environment.ContentRootPath;
            });
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

            appLifetime.ApplicationStopping.Register(() =>
            {
                _logger.LogInformation("Moonglade is stopping...");
            });

            app.UseRobotsTxt();

            app.UseMiddleware<PoweredByMiddleware>();
            app.UseMiddleware<DNTMiddleware>();
            app.UseMiddleware<FirstRunMiddleware>();

            if (_configuration.GetValue<bool>("AppSettings:PreferAzureAppConfiguration"))
            {
                app.UseAzureAppConfiguration();
            }

            if (_environment.IsDevelopment())
            {
                app.UseRouteDebugger();
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseStatusCodePages();
                app.UseExceptionHandler("/error");
                app.UseHttpsRedirection();
                app.UseHsts();
            }

            app.UseRequestLocalization(new RequestLocalizationOptions
            {
                DefaultRequestCulture = new("en-US"),
                SupportedCultures = _supportedCultures,
                SupportedUICultures = _supportedCultures
            });

            app.UseDefaultImage(options =>
            {
                options.AllowedExtensions = _configuration.GetSection("ImageStorage:AllowedExtensions")
                    .GetChildren()
                    .Select(x => x.Value);
                options.DefaultImagePath = _configuration["ImageStorage:DefaultImagePath"];
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
                    context.Response.Headers.Add("X-Moonglade-Version", Helper.AppVersion);
                    var obj = new
                    {
                        MoongladeVersion = Helper.AppVersion,
                        DotNetVersion = Environment.Version.ToString(),
                        EnvironmentTags = Helper.GetEnvironmentTags()
                    };

                    await context.Response.WriteAsync(obj.ToJson(), Encoding.UTF8);
                });
                endpoints.MapControllerRoute(
                    "default",
                    "{controller=Home}/{action=Index}/{id?}");
                endpoints.MapRazorPages();
            });
        }
    }
}