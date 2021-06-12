#region Usings

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using AspNetCoreRateLimit;
using Edi.Captcha;
using Microsoft.ApplicationInsights.DependencyCollector;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.Implementation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;
using Moonglade.Auditing;
using Moonglade.Auth;
using Moonglade.Caching;
using Moonglade.Comments;
using Moonglade.Configuration;
using Moonglade.Configuration.Settings;
using Moonglade.Core;
using Moonglade.Data;
using Moonglade.DataPorting;
using Moonglade.FriendLink;
using Moonglade.ImageStorage;
using Moonglade.Menus;
using Moonglade.Notification.Client;
using Moonglade.Page;
using Moonglade.Pingback;
using Moonglade.Syndication;
using Moonglade.Web.Configuration;
using Moonglade.Web.Filters;
using Moonglade.Web.Middleware;
using WilderMinds.MetaWeblog;

#endregion

namespace Moonglade.Web
{
    public class Startup
    {
        private ILogger<Startup> _logger;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;
        private readonly IList<CultureInfo> _supportedCultures;

        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            _configuration = configuration;
            _environment = env;

            // Workaround stupid ASP.NET "by design" issue
            // https://github.com/aspnet/Configuration/issues/451
            _supportedCultures = _configuration.GetSection("SupportedCultures").Get<string[]>()
                                               .Select(p => new CultureInfo(p))
                                               .ToList();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            // ASP.NET Setup
            services.AddOptions();
            services.AddHttpContextAccessor();
            services.AddRateLimit(_configuration.GetSection("IpRateLimiting"));
            services.AddFeatureManagement();
            services.AddAzureAppConfiguration();
            services.AddApplicationInsightsTelemetry()
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

            services.AddControllers(options => options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()))
                    .ConfigureApiBehaviorOptions(ConfigureApiBehavior.BlogApiBehavior);
            services.AddRazorPages()
                    .AddViewLocalization()
                    .AddDataAnnotationsLocalization()
                    .AddRazorPagesOptions(options =>
                    {
                        options.Conventions.AuthorizeFolder("/Admin");
                        options.Conventions.AuthorizeFolder("/Settings");
                    });

            // Fix Chinese character being encoded in HTML output
            services.AddSingleton(HtmlEncoder.Create(
                new[]
                {
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
                }
            ));

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

            services.Configure<RouteOptions>(options =>
            {
                options.LowercaseUrls = true;
                options.LowercaseQueryStrings = true;
                options.AppendTrailingSlash = false;
            });

            services.AddSwaggerGen();

            // Blog Services
            services.AddCoreBloggingServices()
                    .AddBlogPage()
                    .AddPingback()
                    .AddSyndication()
                    .AddNotificationClient()
                    .AddReleaseCheckerClient();
            services.AddScoped<IMenuService, MenuService>()
                    .AddScoped<IFriendLinkService, FriendLinkService>()
                    .AddScoped<IBlogAudit, BlogAudit>()
                    .AddScoped<IFoafWriter, FoafWriter>()
                    .AddScoped<IExportManager, ExportManager>();
            services.AddScoped<ValidateCaptcha>();
            services.AddMetaWeblog<MetaWeblogService>();
            services.AddBlogCache();
            services.Configure<List<ManifestIcon>>(_configuration.GetSection("ManifestIcons"));
            services.AddBlogConfig(_configuration);
            services.AddScoped<ITimeZoneResolver>(
                c => new BlogTimeZoneResolver(c.GetService<IBlogConfig>()?.GeneralSettings.TimeZoneUtcOffset));
            services.AddBlogAuthenticaton(_configuration);
            services.AddComments(_configuration);
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
            IBlogConfig blogConfig,
            TelemetryConfiguration configuration)
        {
            _logger = logger;

            if (_environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
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
                app.UseMiddleware<RSDMiddleware>();
                app.UseMetaWeblog("/metaweblog");
            }

            app.UseMiddleware<SiteMapMiddleware>();
            app.UseMiddleware<PoweredByMiddleware>();
            app.UseMiddleware<DNTMiddleware>();

            if (_configuration.GetValue<bool>("PreferAzureAppConfiguration"))
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
                app.UseStatusCodePages(ConfigureStatusCodePages.Handler);
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
            app.UseEndpoints(ConfigureEndpoints.BlogEndpoints);
        }
    }
}