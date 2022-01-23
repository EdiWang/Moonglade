using Dapper;
using Moonglade.Data.Setup;
using System.Data;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Moonglade.Data.SqlServer.Setup
{
    public class SqlServerSetupRunner : ISetupRunner
    {
        private readonly IDbConnection _dbConnection;

        public SqlServerSetupRunner(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        /// <summary>
        /// Check if the blog system is first run
        /// Either BlogConfiguration table does not exist or it has empty data is treated as first run.
        /// </summary>
        public bool IsFirstRun()
        {
            var tableExists = _dbConnection.ExecuteScalar<int>("SELECT TOP 1 1 " +
                                                       "FROM INFORMATION_SCHEMA.TABLES " +
                                                       "WHERE TABLE_NAME = N'BlogConfiguration'") == 1;
            if (tableExists)
            {
                var dataExists = _dbConnection.ExecuteScalar<int>("SELECT TOP 1 1 FROM BlogConfiguration") == 1;
                return !dataExists;
            }

            return true;
        }

        /// <summary>
        /// Execute SQL to build database schema
        /// </summary>
        public async Task SetupDatabase(DatabaseFacade dbFacade)
        {
            var sql = GetEmbeddedSqlScript("schema-mssql-140");
            if (!string.IsNullOrWhiteSpace(sql))
            {
                await dbFacade.ExecuteSqlRawAsync(sql);
            }
            else
            {
                throw new InvalidOperationException("Database Schema Script is empty.");
            }
        }

        protected string? GetEmbeddedSqlScript(string scriptName)
        {
            var assembly = typeof(SqlServerSetupRunner).GetTypeInfo().Assembly;
            using var stream = assembly.GetManifestResourceStream($"Moonglade.Data.SqlServer.SQLScripts.{scriptName}.sql");

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
