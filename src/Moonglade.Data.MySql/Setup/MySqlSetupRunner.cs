using Dapper;
using Moonglade.Data.Setup;
using System.Data;
using System.Reflection;

namespace Moonglade.Data.MySql.Setup
{
    public class MySqlSetupRunner : SetupRunnerBase, ISetupRunner
    {
        private readonly IDbConnection _dbConnection;

        public MySqlSetupRunner(IDbConnection dbConnection)
            : base(dbConnection)
        {
            _dbConnection = dbConnection;
        }

        /// <summary>
        /// Check if the blog system is first run
        /// Either BlogConfiguration table does not exist or it has empty data is treated as first run.
        /// </summary>
        public bool IsFirstRun()
        {
            var tableExists = _dbConnection.ExecuteScalar<int>("SELECT 1 FROM INFORMATION_SCHEMA.TABLES " +
                "WHERE TABLE_NAME = N'BlogConfiguration' LIMIT 1") == 1;
            if (tableExists)
            {
                var dataExists = _dbConnection.ExecuteScalar<int>("SELECT 1 FROM BlogConfiguration LIMIT 1 ") == 1;
                return !dataExists;
            }

            return true;
        }

        /// <summary>
        /// Execute SQL to build database schema
        /// </summary>
        public override void SetupDatabase()
        {
            var sql = GetEmbeddedSqlScript("schema-mysql-8");
            if (!string.IsNullOrWhiteSpace(sql))
            {
                _dbConnection.Execute(sql);
            }
            else
            {
                throw new InvalidOperationException("Database Schema Script is empty.");
            }
        }

        protected override string? GetEmbeddedSqlScript(string scriptName)
        {
            var assembly = typeof(MySqlSetupRunner).GetTypeInfo().Assembly;
            using var stream = assembly.GetManifestResourceStream($"Moonglade.Data.MySql.SQLScripts.{scriptName}.sql");

            if (stream == null)
            {
                return null;
            }

            using var reader = new StreamReader(stream);
            var sql = reader.ReadToEnd();
            return sql;
        }
    }
}
