using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Edi.Blog.Pingback;
using Edi.Captcha;
using Edi.Net.AesEncryption;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.Implementation;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Moonglade.Configuration;
using Moonglade.Core;
using Moonglade.Data;
using Moonglade.Data.Entities;
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
using Enumerable = System.Linq.Enumerable;

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

            var authenticationSection = Configuration.GetSection("Authentication");
            var authenticationProvider = authenticationSection["Provider"];
            switch (authenticationProvider)
            {
                case "AzureAd":
                    services.AddAuthentication(sharedOptions =>
                        {
                            sharedOptions.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                            sharedOptions.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
                        })
                        .AddAzureAD(options => Configuration.Bind("Authentication:AzureAd", options)).AddCookie();
                    _logger.LogInformation("Authentication is configured using Azure Active Directory.");
                    break;
                case "Local":
                    services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                            .AddCookie(options =>
                            {
                                // TODO
                                // When some day I'm in a good mood, I will consider writing local authentication provider
                                // for now, just stick with Azure, have a cloud day guys!
                            });

                    _logger.LogInformation("Authentication is configured using Local Account.");
                    throw new NotImplementedException();
                default:
                    var msg = $"Provider {authenticationProvider} is not supported.";
                    _logger.LogCritical(msg);
                    throw new NotSupportedException(msg);
            }

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

            AddImageStorage(services);

            services.AddScoped(typeof(IRepository<>), typeof(DbContextRepository<>));
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

            TryInitializeData(app.ApplicationServices);
        }

        #region Private Helpers

        // DO NOT use async! DO NOT use async! DO NOT use async! Important thing say 3 times!
        private void TryInitializeData(IServiceProvider serviceProvider)
        {
            IEnumerable<BlogConfiguration> GetBlogConfigurationObjects(IEnumerable<KeyValuePair<string, string>> configData)
            {
                return Enumerable.ToList(
                    Enumerable.Select(configData, (t, i) => new BlogConfiguration
                    {
                        Id = i,
                        CfgKey = t.Key,
                        CfgValue = t.Value,
                        LastModifiedTimeUtc = DateTime.UtcNow
                    }));
            }

            void InitBlogConfiguration(DbContext moongladeDbContext)
            {
                // oh, I wish C# could simplify this syntax...
                var defaultConfigData = new List<KeyValuePair<string, string>>
                {
                    // Looks like I have to check in dirty words into source control, haha
                    new KeyValuePair<string, string>(nameof(BlogConfig.DisharmonyWords), "fuck|shit"),
                    new KeyValuePair<string, string>(nameof(BlogConfig.MetaKeyword), "Moonglade"),
                    new KeyValuePair<string, string>(nameof(BlogConfig.MetaAuthor), "Admin"),
                    new KeyValuePair<string, string>(nameof(BlogConfig.SiteTitle), "Moonglade"),
                    new KeyValuePair<string, string>(nameof(BlogConfig.BloggerAvatarBase64), string.Empty),
                    new KeyValuePair<string, string>(nameof(BlogConfig.EnableComments), "True"),

                    // Below code is too SB, may be I could init config from an external file in the future...
                    new KeyValuePair<string, string>(nameof(BlogConfig.FeedSettings),
                        @"{""RssItemCount"":20,""RssCopyright"":""(c) {year} Moonglade"",""RssDescription"":""Latest posts from Moonglade"",""RssGeneratorName"":""Moonglade"",""RssTitle"":""Moonglade"",""AuthorName"":""Admin""}"),
                    new KeyValuePair<string, string>(nameof(BlogConfig.WatermarkSettings),
                        @"{""IsEnabled"":true,""KeepOriginImage"":false,""FontSize"":20,""WatermarkText"":""Moonglade""}"),
                    new KeyValuePair<string, string>(nameof(BlogConfig.EmailConfiguration),
                        @"{""EnableEmailSending"":true,""EnableSsl"":true,""SendEmailOnCommentReply"":true,""SendEmailOnNewComment"":true,""SmtpServerPort"":587,""AdminEmail"":"""",""EmailDisplayName"":""Moonglade"",""SmtpPassword"":"""",""SmtpServer"":"""",""SmtpUserName"":"""",""BannedMailDomain"":""""}")
                };

                var cfgObjs = GetBlogConfigurationObjects(defaultConfigData);
                moongladeDbContext.AddRange(cfgObjs);
                moongladeDbContext.SaveChanges();

                _logger.LogInformation("BlogConfiguration Initialized");
            }

            Guid catId;
            void InitCategories(DbContext moongladeDbContext)
            {
                var cat = new Category
                {
                    Id = Guid.NewGuid(),
                    DisplayName = "Default",
                    Note = "Default Category",
                    Title = "default"
                };
                moongladeDbContext.Add(cat);
                moongladeDbContext.SaveChanges();
                catId = cat.Id;

                _logger.LogInformation("Default Categories Initialized");
            }

            void InitFriendLinks(DbContext moongladeDbContext)
            {
                var friendLink = new FriendLink
                {
                    Id = Guid.NewGuid(),
                    LinkUrl = "https://edi.wang",
                    Title = "Edi.Wang"
                };
                moongladeDbContext.Add(friendLink);
                moongladeDbContext.SaveChanges();

                _logger.LogInformation("Default Friend Links Initialized");
            }

            List<Tag> tags;
            void InitDefaultTags(DbContext moongladeDbContext)
            {
                tags = new List<Tag>
                {
                    new Tag{ DisplayName = "Moonglade", NormalizedName = "moonglade" },
                    new Tag{ DisplayName = ".NET Core", NormalizedName = "dot-net-core" }
                };
                moongladeDbContext.AddRange(tags);
                moongladeDbContext.SaveChanges();

                _logger.LogInformation("Default Tags Initialized");
            }

            void InitFirstPost(DbContext moongladeDbContext)
            {
                var rawPostContent =
                    "Moonglade is the successor of project Nordrassil, which was the .NET Framework version of the blog system. Moonglade is a complete rewrite of the old system using .NET Core, optimized for cloud-based hosting.";

                var id = Guid.NewGuid();
                var post = new Post
                {
                    Id = id,
                    CommentEnabled = true,
                    Title = "Welcome to Moonglade",
                    Slug = "welcome-to-moonglade",
                    PostContent = HttpUtility.HtmlEncode($"<p>{rawPostContent}</p>"),
                    ContentAbstract = rawPostContent,
                    CreateOnUtc = DateTime.UtcNow,
                    PostExtension = new PostExtension
                    {
                        Hits = 1024,
                        Likes = 512,
                        PostId = id
                    },
                    PostPublish = new PostPublish
                    {
                        PostId = id,
                        ContentLanguageCode = "en-us",
                        ExposedToSiteMap = true,
                        IsFeedIncluded = true,
                        IsPublished = true,
                        IsDeleted = false,
                        PubDateUtc = DateTime.UtcNow,
                        PublisherIp = "127.0.0.1"
                    },
                    PostCategory = new List<PostCategory>
                    {
                        new PostCategory{ CategoryId = catId, PostId = id }
                    },
                    PostTag = new List<PostTag>
                    {
                        new PostTag{ TagId = tags[0].Id, PostId = id },
                        new PostTag{ TagId = tags[1].Id, PostId = id }
                    }
                };

                moongladeDbContext.Add(post);
                moongladeDbContext.SaveChanges();

                _logger.LogInformation("First Post Created");
            }

            try
            {
                using (var serviceScope = serviceProvider.CreateScope())
                {
                    var scopeServiceProvider = serviceScope.ServiceProvider;
                    var db = scopeServiceProvider.GetService<MoongladeDbContext>();
                    var isFirstRun = !db.BlogConfiguration.Any();

                    if (isFirstRun)
                    {
                        InitBlogConfiguration(db);
                        InitCategories(db);
                        InitFriendLinks(db);
                        InitDefaultTags(db);
                        InitFirstPost(db);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogCritical("Something ugly blown up when trying to initialize blog configuration, what a day!", e);
            }
        }

        private void AddImageStorage(IServiceCollection services)
        {
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
        }

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

        private void ListAllRegisteredServices(IApplicationBuilder app)
        {
            app.Map("/allservices", builder => builder.Run(async context =>
            {
                var sb = new StringBuilder();
                sb.Append("<table border='1'><thead>");
                sb.Append("<tr><th>Type</th><th>Lifetime</th><th>Instance</th></tr>");
                sb.Append("</thead><tbody>");
                foreach (var svc in _services)
                {
                    sb.Append("<tr>");
                    sb.Append($"<td>{svc.ServiceType.FullName}</td>");
                    sb.Append($"<td>{svc.Lifetime}</td>");
                    sb.Append($"<td>{svc.ImplementationType?.FullName}</td>");
                    sb.Append("</tr>");
                }
                sb.Append("</tbody></table>");
                context.Response.ContentType = "text/html";
                await context.Response.WriteAsync(sb.ToString());
            }));
        }
        #endregion
    }
}
