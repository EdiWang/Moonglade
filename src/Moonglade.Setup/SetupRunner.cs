using System;
using System.Data;
using System.IO;
using System.Reflection;
using Dapper;

namespace Moonglade.Setup
{
    public class SetupRunner
    {
        private readonly IDbConnection conn;

        public SetupRunner(IDbConnection dbConnection)
        {
            conn = dbConnection;
        }

        public void InitFirstRun()
        {
            SetupDatabase();
            ResetDefaultConfiguration();
            InitSampleData();
        }

        /// <summary>
        /// Check if the blog system is first run
        /// Either BlogConfiguration table does not exist or it has empty data is treated as first run.
        /// </summary>
        public bool IsFirstRun()
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

        /// <summary>
        /// Execute SQL to build database schema
        /// </summary>
        public void SetupDatabase()
        {
            var sql = GetEmbeddedSqlScript("schema-mssql-140");
            if (!string.IsNullOrWhiteSpace(sql))
            {
                conn.Execute(sql);
            }
            else
            {
                throw new InvalidOperationException("Database Schema Script is empty.");
            }
        }

        /// <summary>
        /// Clear all data in database but preserve tables schema
        /// </summary>
        public void ClearData()
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
            conn.Execute("DELETE FROM Post");
            conn.Execute("DELETE FROM Menu");

            // Clear Configuration Table
            conn.Execute("DELETE FROM BlogConfiguration");

            // Clear AuditLog Table
            conn.Execute("DELETE FROM AuditLog");

            // Clear LocalAccount Table
            conn.Execute("DELETE FROM LocalAccount");
        }

        public void ResetDefaultConfiguration()
        {
            var sql = GetEmbeddedSqlScript("init-blogconfiguration");
            if (!string.IsNullOrWhiteSpace(sql))
            {
                conn.Execute(sql);
            }
            else
            {
                throw new InvalidDataException("SQL Script is empty.");
            }
        }

        public void InitSampleData()
        {
            var sql = GetEmbeddedSqlScript("init-sampledata");
            if (!string.IsNullOrWhiteSpace(sql))
            {
                conn.Execute(sql);
            }
            else
            {
                throw new InvalidDataException("SQL Script is empty.");
            }
        }

        public bool TestDatabaseConnection(Action<Exception> errorLogAction = null)
        {
            try
            {
                var result = conn.ExecuteScalar<int>("SELECT 1");
                return result == 1;
            }
            catch (Exception e)
            {
                errorLogAction?.Invoke(e);
                return false;
            }
        }

        private static string GetEmbeddedSqlScript(string scriptName)
        {
            var assembly = typeof(SetupRunner).GetTypeInfo().Assembly;
            using var stream = assembly.GetManifestResourceStream($"Moonglade.Setup.Data.{scriptName}.sql");
            using var reader = new StreamReader(stream);
            var sql = reader.ReadToEnd();
            return sql;
        }
    }
}
