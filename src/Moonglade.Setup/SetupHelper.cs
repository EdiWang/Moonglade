using System;
using System.Data.SqlClient;
using System.IO;
using System.Reflection;
using Dapper;
using Edi.Net.AesEncryption;
using Edi.Practice.RequestResponseModel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Moonglade.Setup
{
    public class SetupHelper
    {
        public string DatabaseConnectionString { get; set; }

        public SetupHelper(string databaseConnectionString)
        {
            if (string.IsNullOrWhiteSpace(databaseConnectionString))
            {
                throw new ArgumentNullException(nameof(databaseConnectionString));
            }

            DatabaseConnectionString = databaseConnectionString;
        }

        /// <summary>
        /// Check if the blog system is first run
        /// Either BlogConfiguration table does not exist or it has empty data is treated as first run.
        /// </summary>
        public bool IsFirstRun()
        {
            using (var conn = new SqlConnection(DatabaseConnectionString))
            {
                var tableExists = conn.ExecuteScalar<int>("SELECT TOP 1 1 " +
                                                     "FROM INFORMATION_SCHEMA.TABLES " +
                                                     "WHERE TABLE_NAME = N'BlogConfiguration'") == 1;
                if (tableExists)
                {
                    var dataExists = conn.ExecuteScalar<int>("SELECT TOP 1 1 FROM BlogConfiguration") == 1;
                    return !dataExists;
                }

                return true;
            }
        }

        /// <summary>
        /// Execute SQL to build database schema
        /// </summary>
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

        /// <summary>
        /// Clear all data in database but preserve tables schema
        /// </summary>
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

        private static string GetEmbeddedSqlScript(string scriptName)
        {
            var assembly = typeof(SetupHelper).GetTypeInfo().Assembly;
            using (var stream = assembly.GetManifestResourceStream($"Moonglade.Setup.Data.{scriptName}.sql"))
            {
                if (stream == null) return null;
                using (var reader = new StreamReader(stream))
                {
                    var sql = reader.ReadToEnd();
                    return sql;
                }
            }
        }
    }
}
