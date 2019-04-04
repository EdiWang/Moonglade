using System;
using System.IO;
using System.Text;
using Edi.Blog.Pingback;
using Edi.Captcha;
using Edi.Net.AesEncryption;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.Implementation;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Moonglade.Configuration;
using Moonglade.Core;
using Moonglade.Data;
using Moonglade.Data.Infrastructure;
using Moonglade.ImageStorage;
using Moonglade.ImageStorage.AzureBlob;
using Moonglade.ImageStorage.FileSystem;
using Moonglade.Model;
using Moonglade.Model.Settings;
using Moonglade.Web.Authentication.AzureAd;
using Moonglade.Web.Filters;
using Moonglade.Web.Middleware;
using Moonglade.Web.Middleware.RobotsTxt;

namespace Moonglade.Web
{
    public class Startup
    {
        private readonly ILogger<Startup> _logger;
        private readonly IConfigurationSection _appSettingsSection;

        public IConfiguration Configuration { get; }

        public IHostingEnvironment Environment { get; }

        public Startup(IConfiguration configuration, ILogger<Startup> logger, IHostingEnvironment env)
        {
            Configuration = configuration;
            Environment = env;
            _logger = logger;

            _appSettingsSection = Configuration.GetSection(nameof(AppSettings));
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDistributedMemoryCache();
            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(20);
                options.Cookie.HttpOnly = true;
            });

            services.Configure<AppSettings>(_appSettingsSection);
            services.Configure<RobotsTxtOptions>(Configuration.GetSection("RobotsTxt"));

            services.AddAuthentication(sharedOptions =>
                    {
                        sharedOptions.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                        sharedOptions.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
                    })
                    .AddAzureAD(options => Configuration.Bind("AzureAd", options)).AddCookie();

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2)
                    .AddJsonOptions(
                        options =>
                            options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore
                    );

            services.AddAntiforgery(options =>
            {
                const string cookieBaseName = "CSRF-TOKEN-MOONGLADE";
                options.Cookie.Name = $"X-{cookieBaseName}";
                options.FormFieldName = $"{cookieBaseName}-FORM";
            });
            services.AddMemoryCache();
            services.AddHttpContextAccessor();

            var imageStorageSection = Configuration.GetSection("ImageStorage");
            var imageStorageProvider = imageStorageSection["Provider"];
            switch (imageStorageProvider)
            {
                case nameof(AzureStorageImageProvider):
                    var conn = imageStorageSection["AzureStorageSettings:ConnectionString"];
                    var container = imageStorageSection["AzureStorageSettings:ContainerName"];

                    services.AddSingleton(s => new AzureStorageInfo(conn, container));
                    services.AddSingleton<IAsyncImageStorageProvider, AzureStorageImageProvider>();
                    break;
                case nameof(FileSystemImageProvider):
                    var path = imageStorageSection["FileSystemSettings:Path"];
                    try
                    {
                        var fullPath = ResolveImageStoragePath(path);

                        _logger.LogInformation($"Setting {nameof(FileSystemImageProvider)} to use Path: {fullPath}");
                        services.AddSingleton(s => new FileSystemImageProviderInfo(path));
                        services.AddSingleton<IAsyncImageStorageProvider, FileSystemImageProvider>();
                    }
                    catch (Exception e)
                    {
                        _logger.LogCritical(e, $"Error setting path for {nameof(FileSystemImageProvider)}, raw path: {path}");
                        throw;
                    }
                    break;
                default:
                    var msg = $"Provider {imageStorageProvider} is not supported.";
                    _logger.LogCritical(msg);
                    throw new NotSupportedException(msg);
            }

            services.AddScoped(typeof(IAsyncRepository<>), typeof(DbContextRepository<>));

            services.TryAddSingleton<IActionContextAccessor, ActionContextAccessor>();
            services.AddSingleton<BlogConfig>();
            services.AddScoped<DeleteSubscriptionCache>();
            services.AddTransient<ISessionBasedCaptcha, BasicLetterCaptcha>();
            services.AddTransient<BlogConfigurationService>();
            services.AddTransient<CategoryService>();
            services.AddTransient<CommentService>();
            services.AddTransient<EmailService>();
            services.AddTransient<FriendLinkService>();
            services.AddTransient<PostService>();
            services.AddTransient<PingbackSender>();
            services.AddTransient<PingbackReceiver>();
            services.AddTransient<PingbackService>();
            services.AddTransient<SyndicationService>();
            services.AddTransient<TagService>();

            var encryptionSettings = Configuration.GetSection("Encryption");
            var aesKey = encryptionSettings["Key"];
            var aesIv = encryptionSettings["IV"];
            services.AddTransient(enc => new AesEncryptionService(new KeyInfo(aesKey, aesIv)));

            services.AddDbContext<MoongladeDbContext>(options =>
                    options.UseLazyLoadingProxies()
                           .UseSqlServer(Configuration.GetConnectionString(Constants.DbConnectionName), sqlServerOptionsAction:
                               sqlOptions =>
                               {
                                   sqlOptions.EnableRetryOnFailure(
                                       maxRetryCount: 3,
                                       maxRetryDelay: TimeSpan.FromSeconds(30),
                                       errorNumbersToAdd: null);
                               }));
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IApplicationLifetime appLifetime)
        {
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

            PrepareRuntimePathDependencies(app, env);

            app.UseSecurityHeaders(new HeaderPolicyCollection()
                .AddFrameOptionsSameOrigin()
                .AddXssProtectionEnabled()
                .AddContentTypeOptionsNoSniff()
            );

            if (env.IsDevelopment())
            {
                _logger.LogWarning("Application is running under DEBUG mode. Application Insights disabled.");

                TelemetryConfiguration.Active.DisableTelemetry = true;
                TelemetryDebugWriter.IsTracingDisabled = true;

                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/error");
                app.UseStatusCodePagesWithReExecute("/error", "?statusCode={0}");
            }

            var enforceHttps = bool.Parse(_appSettingsSection["EnforceHttps"]);
            if (enforceHttps)
            {
                _logger.LogInformation("HTTPS is enforced.");
                app.UseHttpsRedirection();
                app.UseHsts();
            }

            // Support Chinese contents
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            app.UseStaticFiles();
            app.UseSession();
            app.UseAuthentication();

            // robots.txt
            app.UseRobotsTxt();
            //app.UseRobotsTxt(builder =>
            //builder.AddSection(section =>
            //        section.SetComment("Allow Googlebot")
            //               .SetUserAgent("Googlebot")
            //               .Allow("/"))
            ////.AddSitemap("https://example.com/sitemap.xml")
            //);

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Post}/{action=Index}/{id?}");
            });
        }

        #region Private Helpers

        private void PrepareRuntimePathDependencies(IApplicationBuilder app, IHostingEnvironment env)
        {
            var baseDir = env.ContentRootPath;
            TryAddUrlRewrite(app, baseDir);
            AppDomain.CurrentDomain.SetData(Constants.AppBaseDirectory, baseDir);

            // Use Temp folder as best practice
            // Do NOT create or modify anything under application directory
            // e.g. Azure Deployment using WEBSITE_RUN_FROM_PACKAGE will make website root directory read only.
            string tPath = Path.GetTempPath();
            _logger.LogInformation($"Server environment Temp path: {tPath}");
            string moongladeAppDataPath = Path.Combine(tPath, @"moonglade\App_Data");
            if (Directory.Exists(moongladeAppDataPath))
            {
                Directory.Delete(moongladeAppDataPath, true);
            }

            Directory.CreateDirectory(moongladeAppDataPath);
            AppDomain.CurrentDomain.SetData(Constants.DataDirectory, moongladeAppDataPath);
            _logger.LogInformation($"Created Application Data path: {moongladeAppDataPath}");

            var feedDirectoryPath = $@"{AppDomain.CurrentDomain.GetData(Constants.DataDirectory)}\feed";
            if (!Directory.Exists(feedDirectoryPath))
            {
                Directory.CreateDirectory(feedDirectoryPath);
                _logger.LogInformation($"Created feed path: {feedDirectoryPath}");
            }

            CleanDataCache();
        }

        private void TryAddUrlRewrite(IApplicationBuilder app, string baseDir)
        {
            try
            {
                var urlRewriteConfigPath = Path.Combine(baseDir, "urlrewrite.xml");
                if (File.Exists(urlRewriteConfigPath))
                {
                    using (var sr = File.OpenText(urlRewriteConfigPath))
                    {
                        var options = new RewriteOptions().AddIISUrlRewrite(sr);
                        app.UseRewriter(options);
                    }
                }
                else
                {
                    _logger.LogWarning($"Can not find {urlRewriteConfigPath}, skip adding url rewrite.");
                }
            }
            catch (Exception e)
            {
                // URL Rewrite is non-fatal error, continue running the application.
                _logger.LogError(e, nameof(TryAddUrlRewrite));
            }
        }

        private void CleanDataCache()
        {
            var openSearchDataFile = $@"{AppDomain.CurrentDomain.GetData(Constants.DataDirectory)}\{Constants.OpenSearchFileName}";
            var opmlDataFile = $@"{AppDomain.CurrentDomain.GetData(Constants.DataDirectory)}\{Constants.OpmlFileName}";

            DeleteDataFile(openSearchDataFile);
            DeleteDataFile(opmlDataFile);
        }

        private void DeleteDataFile(string path)
        {
            try
            {
                _logger.LogInformation($"Deleting {path}");
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error Deleting file {path}");
            }
        }

        private string ResolveImageStoragePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            var basedirStr = "${basedir}"; // Do not use "." because there could be "." in path.
            if (path.IndexOf(basedirStr, StringComparison.Ordinal) > 0)
            {
                _logger.LogError($"Invalid Path settings for {nameof(FileSystemImageProvider)}, settings value: {path}, {basedirStr} can only be at the beginning.");
                throw new NotSupportedException($"{basedirStr} can only be at the beginning.");
            }
            if (path.IndexOf(basedirStr, StringComparison.Ordinal) == 0)
            {
                // Use relative path
                // Warning: Write data under application directory may blow up on Azure App Services when WEBSITE_RUN_FROM_PACKAGE = 1, which set the directory read-only.
                path = path.Replace(basedirStr, Environment.ContentRootPath);
            }

            // IsPathFullyQualified can't check if path is valid, e.g.:
            // Path: C:\Documents<>|foo
            //   Rooted: True
            //   Fully qualified: True
            //   Full path: C:\Documents<>|foo
            var invalidChars = Path.GetInvalidPathChars();
            if (path.IndexOfAny(invalidChars) >= 0)
            {
                throw new InvalidOperationException("Path can not contain invalid chars.");
            }
            if (!Path.IsPathFullyQualified(path))
            {
                throw new InvalidOperationException("Path is not fully qualified.");
            }

            var fullPath = Path.GetFullPath(path);
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
            }
            return fullPath;
        }

        #endregion
    }
}
