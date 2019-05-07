using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using Edi.Net.AesEncryption;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moonglade.Configuration;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Model;
using Newtonsoft.Json;

namespace Moonglade.Core
{
    public class BlogManager
    {
        public static void TryInitializeFirstRunData(IHostingEnvironment env, IServiceProvider serviceProvider, ILogger logger)
        {
            // Caveat: This will require non-readonly for the application directory
            void SetInitialEncryptionKey()
            {
                try
                {
                    var ki = new KeyInfo();

                    var appSettingsFilePath = Path.Combine(env.ContentRootPath,
                        env.EnvironmentName != EnvironmentName.Production ?
                            $"appsettings.{env.EnvironmentName}.json" :
                            "appsettings.json");

                    if (File.Exists(appSettingsFilePath))
                    {
                        var json = File.ReadAllText(appSettingsFilePath);
                        var jsonObj = JsonConvert.DeserializeObject<dynamic>(json);
                        var encryptionNode = jsonObj["Encryption"];
                        if (null != encryptionNode)
                        {
                            encryptionNode["Key"] = ki.KeyString;
                            encryptionNode["IV"] = ki.IVString;
                            var newJson = JsonConvert.SerializeObject(jsonObj, Formatting.Indented);
                            File.WriteAllText(appSettingsFilePath, newJson);
                        }
                    }
                    else
                    {
                        throw new FileNotFoundException("Failed to initialize Key and IV for password encryption. Settings file is not found.", appSettingsFilePath);
                    }
                }
                catch (Exception e)
                {
                    logger.LogError("Unable to set initial Key and IV, please do it manually.", e);
                }
            }

            IEnumerable<BlogConfiguration> GetBlogConfigurationObjects(IEnumerable<KeyValuePair<string, string>> configData)
            {
                return configData.Select((t, i) => new BlogConfiguration
                {
                    Id = i,
                    CfgKey = t.Key,
                    CfgValue = t.Value,
                    LastModifiedTimeUtc = DateTime.UtcNow
                }).ToList();
            }

            void InitBlogConfiguration(DbContext moongladeDbContext)
            {
                // oh, I wish C# could simplify this syntax...
                var defaultConfigData = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>(nameof(IBlogConfig.BlogOwnerSettings),  Constants.BlogOwnerSettingsDefaultValue),
                    new KeyValuePair<string, string>(nameof(IBlogConfig.GeneralSettings), Constants.GeneralSettingsDefaultValue),
                    new KeyValuePair<string, string>(nameof(IBlogConfig.ContentSettings), Constants.ContentSettingsDefaultValue),
                    new KeyValuePair<string, string>(nameof(IBlogConfig.FeedSettings), Constants.FeedSettingsDefaultValue),
                    new KeyValuePair<string, string>(nameof(IBlogConfig.WatermarkSettings), Constants.WatermarkSettingsDefaultValue),
                    new KeyValuePair<string, string>(nameof(IBlogConfig.EmailConfiguration), Constants.EmailConfigurationDefaultValue)
                };

                var cfgObjs = GetBlogConfigurationObjects(defaultConfigData);
                moongladeDbContext.AddRange(cfgObjs);
                moongladeDbContext.SaveChanges();

                logger.LogInformation("BlogConfiguration Initialized");
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

                logger.LogInformation("Default Categories Initialized");
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

                logger.LogInformation("Default Friend Links Initialized");
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

                logger.LogInformation("Default Tags Initialized");
            }

            void InitFirstPost(DbContext moongladeDbContext)
            {
                var id = Guid.NewGuid();
                var post = new Post
                {
                    Id = id,
                    CommentEnabled = true,
                    Title = "Welcome to Moonglade",
                    Slug = "welcome-to-moonglade",
                    PostContent = HttpUtility.HtmlEncode($"<p>{Constants.PostContentInitValue}</p>"),
                    ContentAbstract = Constants.PostContentInitValue,
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

                logger.LogInformation("First Post Created");
            }

            try
            {
                using (var serviceScope = serviceProvider.CreateScope())
                {
                    var scopeServiceProvider = serviceScope.ServiceProvider;
                    var db = scopeServiceProvider.GetService<MoongladeDbContext>();
                    var isFirstRun = !EnumerableExtensions.Any(db.BlogConfiguration);

                    if (isFirstRun)
                    {
                        SetInitialEncryptionKey();
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
                logger.LogCritical("Something ugly blown up when trying to initialize blog configuration, what a day!", e);
                throw;
            }
        }
    }
}
