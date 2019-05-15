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
using Moonglade.Data;
using Moonglade.Data.Entities;
using Newtonsoft.Json;

namespace Moonglade.Setup
{
    public class SetupHelper
    {
        public string DatabaseConnectionString { get; set; }

        public SetupHelper(string databaseConnectionString)
        {
            DatabaseConnectionString = databaseConnectionString;
        }

        public bool IsFirstRun()
        {
            using (var conn = new SqlConnection(DatabaseConnectionString))
            {
                var result = conn.ExecuteScalar<int>("SELECT TOP 1 1 " +
                                                     "FROM INFORMATION_SCHEMA.TABLES " +
                                                     "WHERE TABLE_NAME = N'BlogConfiguration'");
                return result == 0;
            }
        }

        public Response SetupDatabase()
        {
            try
            {
                using (var conn = new SqlConnection(DatabaseConnectionString))
                {
                    var sql = GetEmbeddedSqlScript("schema-mssql-140");
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

        public Response ClearData()
        {
            try
            {
                using (var conn = new SqlConnection(DatabaseConnectionString))
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
                    return new SuccessResponse();
                }
            }
            catch (Exception e)
            {
                return new FailedResponse(e.Message);
            }
        }

        public Response ResetDefaultConfiguration()
        {
            try
            {
                using (var conn = new SqlConnection(DatabaseConnectionString))
                {
                    var sql = GetEmbeddedSqlScript("init-blogconfiguration");
                    if (!string.IsNullOrWhiteSpace(sql))
                    {
                        conn.Execute(sql);
                        return new SuccessResponse();
                    }
                    return new FailedResponse("SQL Script is empty.");
                }
            }
            catch (Exception e)
            {
                return new FailedResponse(e.Message);
            }
        }

        public Response InitSampleData()
        {
            try
            {
                using (var conn = new SqlConnection(DatabaseConnectionString))
                {
                    var sql = GetEmbeddedSqlScript("init-sampledata");
                    if (!string.IsNullOrWhiteSpace(sql))
                    {
                        conn.Execute(sql);
                        return new SuccessResponse();
                    }
                    return new FailedResponse("SQL Script is empty.");
                }
            }
            catch (Exception e)
            {
                return new FailedResponse(e.Message);
            }
        }

        public bool TestDatabaseConnection(Action<Exception> errorLogAction = null)
        {
            try
            {
                using (var conn = new SqlConnection(DatabaseConnectionString))
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
            Guid catId;
            void GetDefaultCategoryId(DbContext moongladeDbContext)
            {
                var cat = moongladeDbContext.Set<Category>().First();
                catId = cat.Id;
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
                        new PostTag{ TagId = 1, PostId = id },
                        new PostTag{ TagId = 2, PostId = id }
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
                    GetDefaultCategoryId(db);
                    InitFirstPost(db);
                }
            }
            catch (Exception e)
            {
                logger.LogCritical("Something ugly blown up when trying to initialize blog configuration, what a day!", e);
                throw;
            }
        }

        private string GetEmbeddedSqlScript(string scriptName)
        {
            var assembly = typeof(SetupHelper).GetTypeInfo().Assembly;
            using (var stream = assembly.GetManifestResourceStream($"Moonglade.Setup.Data.{scriptName}.sql"))
            using (var reader = new StreamReader(stream))
            {
                var sql = reader.ReadToEnd();
                return sql;
            }
        }
    }
}
