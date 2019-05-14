using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;
using Dapper;
using Edi.Net.AesEncryption;
using Edi.Practice.RequestResponseModel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moonglade.Configuration;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Newtonsoft.Json;

namespace Moonglade.Setup
{
    public class SetupHelper
    {
        public static string GetDatabaseSchemaScript()
        {
            var assembly = typeof(SetupHelper).GetTypeInfo().Assembly;
            using (var stream = assembly.GetManifestResourceStream("Moonglade.Setup.Data.schema-mssql-140.sql"))
            using (var reader = new StreamReader(stream))
            {
                var sql = reader.ReadToEnd();
                return sql;
            }
        }

        public static Response SetupDatabase(string dbConnection)
        {
            try
            {
                if (!TestDatabaseConnection(dbConnection)) return new FailedResponse("Database Connection Error");
                using (var conn = new SqlConnection(dbConnection))
                {
                    var sql = GetDatabaseSchemaScript();
                    if (!string.IsNullOrWhiteSpace(sql))
                    {
                        conn.Execute(sql);
                        return new SuccessResponse();
                    }
                    return new FailedResponse("Database Schema Script is empty.");
                }
            }
            catch (Exception e)
            {
                return new FailedResponse(e.Message);
            }
        }

        public static bool TestDatabaseConnection(string dbConnection, Action<Exception> errorLogAction = null)
        {
            try
            {
                using (var conn = new SqlConnection(dbConnection))
                {
                    int result = conn.ExecuteScalar<int>("SELECT 1");
                    return result == 1;
                }
            }
            catch (Exception e)
            {
                errorLogAction?.Invoke(e);
                return false;
            }
        }

        public static void ClearData(string dbConnection)
        {
            using (var conn = new SqlConnection(dbConnection))
            {
                // Clear Relation Tables
                conn.Execute("DELETE FROM PostTag");
                conn.Execute("DELETE FROM PostCategory");
                conn.Execute("DELETE FROM CommentReply");

                // Clear Individual Tables
                conn.Execute("DELETE FROM Category");
                conn.Execute("DELETE FROM Tag");
                conn.Execute("DELETE FROM Comment");
                conn.Execute("DELETE FROM FriendLink");
                conn.Execute("DELETE FROM PingbackHistory");
                conn.Execute("DELETE FROM PostExtension");
                conn.Execute("DELETE FROM PostPublish");
                conn.Execute("DELETE FROM Post");

                // Clear Configuration Table
                conn.Execute("DELETE FROM BlogConfiguration");
            }
        }

        public static bool IsFirstRun(string dbConnection)
        {
            using (var conn = new SqlConnection(dbConnection))
            {
                var result = conn.ExecuteScalar<int>("SELECT TOP 1 1 " +
                                                     "FROM INFORMATION_SCHEMA.TABLES " +
                                                     "WHERE TABLE_NAME = N'BlogConfiguration'");
                return result == 0;
            }
        }

        // Caveat: This will require non-readonly for the application directory
        public static void SetInitialEncryptionKey(IHostingEnvironment env, ILogger logger)
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

        public static void TryInitializeFirstRunData(IServiceProvider serviceProvider, ILogger logger)
        {
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
                    new KeyValuePair<string, string>(nameof(IBlogConfig.BlogOwnerSettings),  SetupConstants.BlogOwnerSettingsDefaultValue),
                    new KeyValuePair<string, string>(nameof(IBlogConfig.GeneralSettings), SetupConstants.GeneralSettingsDefaultValue),
                    new KeyValuePair<string, string>(nameof(IBlogConfig.ContentSettings), SetupConstants.ContentSettingsDefaultValue),
                    new KeyValuePair<string, string>(nameof(IBlogConfig.FeedSettings), SetupConstants.FeedSettingsDefaultValue),
                    new KeyValuePair<string, string>(nameof(IBlogConfig.WatermarkSettings), SetupConstants.WatermarkSettingsDefaultValue),
                    new KeyValuePair<string, string>(nameof(IBlogConfig.EmailConfiguration), SetupConstants.EmailConfigurationDefaultValue)
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
                    PostContent = HttpUtility.HtmlEncode($"<p>{SetupConstants.PostContentInitValue}</p>"),
                    ContentAbstract = SetupConstants.PostContentInitValue,
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
                    InitBlogConfiguration(db);
                    InitCategories(db);
                    InitFriendLinks(db);
                    InitDefaultTags(db);
                    InitFirstPost(db);
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
