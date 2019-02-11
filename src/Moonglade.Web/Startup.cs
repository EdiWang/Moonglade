using System;
using System.IO;
using System.Text;
using Edi.Blog.Pingback;
using Edi.Captcha;
using Edi.Net.AesEncryption;
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
using Moonglade.AzureApplicationInsights;
using Moonglade.Configuration;
using Moonglade.Core;
using Moonglade.Data;
using Moonglade.ImageStorage;
using Moonglade.ImageStorage.AzureBlob;
using Moonglade.ImageStorage.FileSystem;
using Moonglade.Model;
using Moonglade.Model.Settings;
using Moonglade.Web.Authentication.AzureAd;
using Moonglade.Web.Filters;

namespace Moonglade.Web
{
    public class Startup
    {
        private readonly ILogger<Startup> _logger;

        private readonly string _appInsightApiKey;
        private readonly string _appInsightEndpoint;
        private readonly string _appInsightAppId;
        private readonly string _azStorageConnectionString;
        private readonly string _azStorageContainerName;
        private readonly string _aesKey;
        private readonly string _aesIv;

        public Startup(IConfiguration configuration, ILogger<Startup> logger)
        {
            Configuration = configuration;
            _logger = logger;

            var appInsightsConfig = Configuration.GetSection("ApplicationInsights");
            _appInsightAppId = appInsightsConfig["AppId"];
            _appInsightApiKey = appInsightsConfig["APIKey"];
            _appInsightEndpoint = appInsightsConfig["APIEndpoint"];

            var azureStorageSettings = Configuration.GetSection("AzureStorageSettings");
            _azStorageConnectionString = azureStorageSettings["ConnectionString"];
            _azStorageContainerName = azureStorageSettings["ContainerName"];

            var encryptionSettings = Configuration.GetSection("Encryption");
            _aesKey = encryptionSettings["Key"];
            _aesIv = encryptionSettings["IV"];
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDistributedMemoryCache();
            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(20);
                options.Cookie.HttpOnly = true;
            });

            services.Configure<AppSettings>(Configuration.GetSection(nameof(AppSettings)));

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
            services.TryAddSingleton<IActionContextAccessor, ActionContextAccessor>();
            services.AddSingleton(s => new AzureStorageInfo(_azStorageConnectionString, _azStorageContainerName));
            services.AddSingleton<IAsyncImageStorageProvider, FileSystemImageProvider>();
            services.AddSingleton<IAsyncImageStorageProvider, AzureStorageImageProvider>();
            services.AddSingleton<BlogConfig>();
            services.AddSingleton(m => new MetricsReader(_appInsightEndpoint, _appInsightAppId, _appInsightApiKey));
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
            services.AddTransient<SyndicationFeedService>();
            services.AddTransient<TagService>();
            services.AddTransient(enc => new AesEncryptionService(new KeyInfo(_aesKey, _aesIv)));

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

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            var baseDir = env.ContentRootPath;
            AppDomain.CurrentDomain.SetData(Constants.AppBaseDirectory, baseDir);
            AppDomain.CurrentDomain.SetData(Constants.DataDirectory, Path.Combine(baseDir, "App_Data"));

            TryAddUrlRewrite(app, baseDir);

            var feedDirectoryPath = $"{AppDomain.CurrentDomain.GetData(Constants.DataDirectory)}\\feed";
            if (!Directory.Exists(feedDirectoryPath))
            {
                Directory.CreateDirectory(feedDirectoryPath);
            }
            CleanDataCache();

            app.UseSecurityHeaders(new HeaderPolicyCollection()
                .AddCustomHeader("X-UA-Compatible", "IE=edge")
            );

            if (env.IsDevelopment())
            {
                _logger.LogWarning("Application is running under DEBUG mode.");
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseStatusCodePagesWithReExecute("/error", "?statusCode={0}");
                app.UseHttpsRedirection();
                app.UseHsts();
            }

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            app.UseStaticFiles();
            app.UseSession();
            app.UseAuthentication();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Post}/{action=Index}/{id?}");
            });
        }

        private void TryAddUrlRewrite(IApplicationBuilder app, string baseDir)
        {
            try
            {
                using (var sr = File.OpenText(Path.Combine(baseDir, "urlrewrite.xml")))
                {
                    var options = new RewriteOptions().AddIISUrlRewrite(sr);
                    app.UseRewriter(options);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "URL Rewrite is non-fatal error, continue running the application.");
            }
        }

        private void CleanDataCache()
        {
            var openSearchDataFile = $"{AppDomain.CurrentDomain.GetData(Constants.DataDirectory)}\\opensearch.xml";
            var opmlDataFile = $"{AppDomain.CurrentDomain.GetData(Constants.DataDirectory)}\\opml.xml";

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
    }
}
