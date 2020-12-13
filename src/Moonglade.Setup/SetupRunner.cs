using System;
using System.Data;
using System.IO;
using System.Reflection;
using Dapper;

namespace Moonglade.Setup
{
    public class SetupRunner
    {
        private readonly IDbConnection _conn;

        public SetupRunner(IDbConnection dbConnection)
        {
            _conn = dbConnection;
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
            var tableExists = _conn.ExecuteScalar<int>("SELECT TOP 1 1 " +
                                                      "FROM INFORMATION_SCHEMA.TABLES " +
                                                      "WHERE TABLE_NAME = N'BlogConfiguration'") == 1;
            if (tableExists)
            {
                var dataExists = _conn.ExecuteScalar<int>("SELECT TOP 1 1 FROM BlogConfiguration") == 1;
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
                _conn.Execute(sql);
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
            _conn.Execute("DELETE FROM PostTag");
            _conn.Execute("DELETE FROM PostCategory");
            _conn.Execute("DELETE FROM CommentReply");

            // Clear Individual Tables
            _conn.Execute("DELETE FROM Category");
            _conn.Execute("DELETE FROM Tag");
            _conn.Execute("DELETE FROM Comment");
            _conn.Execute("DELETE FROM FriendLink");
            _conn.Execute("DELETE FROM PingbackHistory");
            _conn.Execute("DELETE FROM PostExtension");
            _conn.Execute("DELETE FROM Post");
            _conn.Execute("DELETE FROM Menu");

            // Clear Configuration Table
            _conn.Execute("DELETE FROM BlogConfiguration");

            // Clear AuditLog Table
            _conn.Execute("DELETE FROM AuditLog");

            // Clear LocalAccount Table
            _conn.Execute("DELETE FROM LocalAccount");
        }

        public void ResetDefaultConfiguration()
        {
            var sql = GetEmbeddedSqlScript("init-blogconfiguration");
            if (!string.IsNullOrWhiteSpace(sql))
            {
                _conn.Execute(sql);
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
                _conn.Execute(sql);
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
                var result = _conn.ExecuteScalar<int>("SELECT 1");
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
