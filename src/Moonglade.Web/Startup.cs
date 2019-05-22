using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Edi.Blog.Pingback;
using Edi.Captcha;
using Edi.Net.AesEncryption;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.Implementation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Moonglade.Configuration;
using Moonglade.Configuration.Abstraction;
using Moonglade.Core;
using Moonglade.Data;
using Moonglade.Data.Infrastructure;
using Moonglade.ImageStorage;
using Moonglade.ImageStorage.AzureBlob;
using Moonglade.ImageStorage.FileSystem;
using Moonglade.Model;
using Moonglade.Model.Settings;
using Moonglade.Notification;
using Moonglade.Setup;
using Moonglade.Web.Authentication;
using Moonglade.Web.Filters;
using Moonglade.Web.Middleware.PoweredBy;
using Moonglade.Web.Middleware.RobotsTxt;
using Newtonsoft.Json;

namespace Moonglade.Web
{
    public class Startup
    {
        private IServiceCollection _services;

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

            var authentication = new AuthenticationSettings();
            Configuration.Bind(nameof(Authentication), authentication);
            services.AddMoongladeAuthenticaton(authentication);

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2)
                    .AddJsonOptions(
                        options =>
                            options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                    );

            services.AddAntiforgery(options =>
            {
                const string cookieBaseName = "CSRF-TOKEN-MOONGLADE";
                options.Cookie.Name = $"X-{cookieBaseName}";
                options.FormFieldName = $"{cookieBaseName}-FORM";
            });
            services.AddMemoryCache();
            services.AddHttpContextAccessor();

            AddImageStorage(services);

            services.AddScoped(typeof(IRepository<>), typeof(DbContextRepository<>));
            services.TryAddSingleton<IActionContextAccessor, ActionContextAccessor>();
            services.AddSingleton<IBlogConfig, BlogConfig>();
            services.AddScoped<DeleteSubscriptionCache>();
            services.AddTransient<ISessionBasedCaptcha, BasicLetterCaptcha>();
            services.AddTransient<IMoongladeNotification, EmailNotification>();
            services.AddTransient<IPingbackSender, PingbackSender>();
            services.AddTransient<IPingbackReceiver, PingbackReceiver>();

            var asm = Assembly.GetAssembly(typeof(MoongladeService));
            if (null != asm)
            {
                var types = asm.GetTypes().Where(t => t.IsClass && t.IsPublic && t.Name.EndsWith("Service"));
                foreach (var t in types)
                {
                    services.AddTransient(t, t);
                }
            }

            var encryption = new Encryption();
            Configuration.Bind(nameof(Encryption), encryption);
            services.AddTransient<IAesEncryptionService>(enc => new AesEncryptionService(new KeyInfo(encryption.Key, encryption.IV)));

            services.AddDbContext<MoongladeDbContext>(options =>
                    options.UseLazyLoadingProxies()
                           .UseSqlServer(Configuration.GetConnectionString(Constants.DbConnectionName), sqlOptions =>
                               {
                                   sqlOptions.EnableRetryOnFailure(
                                       maxRetryCount: 3,
                                       maxRetryDelay: TimeSpan.FromSeconds(30),
                                       errorNumbersToAdd: null);
                               }));

            _services = services;
        }

        // ReSharper disable once UnusedMember.Global
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
            app.UseMiddleware<PoweredByMiddleware>();

            if (env.IsDevelopment())
            {
                _logger.LogWarning("Application is running under DEBUG mode. Application Insights disabled.");

                TelemetryConfiguration.Active.DisableTelemetry = true;
                TelemetryDebugWriter.IsTracingDisabled = true;
                ListAllRegisteredServices(app);

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

            // robots.txt
            app.UseRobotsTxt();
            //app.UseRobotsTxt(builder =>
            //builder.AddSection(section =>
            //        section.SetComment("Allow Googlebot")
            //               .SetUserAgent("Googlebot")
            //               .Allow("/"))
            ////.AddSitemap("https://example.com/sitemap.xml")
            //);

            app.UseAuthentication();

            var conn = Configuration.GetConnectionString(Constants.DbConnectionName);
            var setupHelper = new SetupHelper(conn);

            if (!setupHelper.TestDatabaseConnection(exception =>
            {
                _logger.LogCritical(exception, $"Error {nameof(SetupHelper.TestDatabaseConnection)}, connection string: {conn}");
            }))
            {
                app.Run(async context =>
                {
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    await context.Response.WriteAsync("Database connection failed. Please see error log, fix it and RESTART this application.");
                });
            }
            else
            {
                if (setupHelper.IsFirstRun())
                {
                    try
                    {
                        SetupHelper.SetInitialEncryptionKey(Environment, _logger);
                        setupHelper.SetupDatabase();
                        setupHelper.ResetDefaultConfiguration();
                        setupHelper.InitSampleData();
                    }
                    catch (Exception e)
                    {
                        _logger.LogCritical(e, e.Message);
                        app.Run(async context =>
                        {
                            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                            await context.Response.WriteAsync("Error initializing first run, please check error log.");
                        });
                    }
                }

                app.UseMvc(routes =>
                {
                    routes.MapRoute(
                        name: "default",
                        template: "{controller=Post}/{action=Index}/{id?}");
                });
            }
        }

        #region Private Helpers


        private void AddImageStorage(IServiceCollection services)
        {
            var imageStorage = new ImageStorage.ImageStorage();
            Configuration.Bind(nameof(ImageStorage), imageStorage);

            if (null == imageStorage.Provider)
            {
                throw new ArgumentNullException("Provider", "Provider can not be null.");
            }

            var imageStorageProvider = imageStorage.Provider.ToLower();
            if (string.IsNullOrWhiteSpace(imageStorageProvider))
            {
                throw new ArgumentNullException("Provider", "Provider can not be empty.");
            }

            switch (imageStorageProvider)
            {
                case "azurestorage":
                    var conn = imageStorage.AzureStorageSettings.ConnectionString;
                    var container = imageStorage.AzureStorageSettings.ContainerName;

                    services.AddSingleton(s => new AzureStorageInfo(conn, container));
                    services.AddSingleton<IAsyncImageStorageProvider, AzureStorageImageProvider>();
                    break;
                case "filesystem":
                    var path = imageStorage.FileSystemSettings.Path;
                    try
                    {
                        var fullPath = Utils.ResolveImageStoragePath(Environment.ContentRootPath, path);

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
        }

        private void PrepareRuntimePathDependencies(IApplicationBuilder app, IHostingEnvironment env)
        {
            void DeleteDataFile(string path)
            {
                try
                {
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

            void CleanDataCache()
            {
                var openSearchDataFile = $@"{AppDomain.CurrentDomain.GetData(Constants.DataDirectory)}\{Constants.OpenSearchFileName}";
                var opmlDataFile = $@"{AppDomain.CurrentDomain.GetData(Constants.DataDirectory)}\{Constants.OpmlFileName}";

                DeleteDataFile(openSearchDataFile);
                DeleteDataFile(opmlDataFile);
            }

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
                        var options = new RewriteOptions()
                            .AddRedirect("(.*)/$", "$1")
                            .AddIISUrlRewrite(sr);
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

        private void ListAllRegisteredServices(IApplicationBuilder app)
        {
            app.Map("/debug/allservices", builder => builder.Run(async context =>
            {
                var sb = new StringBuilder();
                sb.Append("<html>" +
                          "<head>" +
                          "<title>All Registered Services</title>" +
                          "<link href=\"/css/mooglade-css-bundle.min.css?\" rel=\"stylesheet\" />\r" +
                          "</head>" +
                          "<body><div class=\"container-fluid\" style=\"font-family: Consolas\">" +
                          "<table class=\"table table-bordered table-hover table-sm table-responsive\">" +
                          "<thead>");
                sb.Append("<tr><th>Lifetime</th><th>Instance</th></tr>");
                sb.Append("</thead><tbody>");
                foreach (var svc in _services.Where(svc => svc.ImplementationType != null).OrderBy(svc => svc.ImplementationType.FullName))
                {
                    sb.Append("<tr>");
                    sb.Append($"<td>{svc.Lifetime}</td>");
                    sb.Append($"<td>{svc.ImplementationType.FullName}</td>");
                    sb.Append("</tr>");
                }
                sb.Append("</tbody></table></div></body></html>");
                context.Response.ContentType = "text/html";
                await context.Response.WriteAsync(sb.ToString());
            }));
        }
        #endregion
    }
}