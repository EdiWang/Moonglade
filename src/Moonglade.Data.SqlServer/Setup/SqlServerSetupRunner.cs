using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Moonglade.Data.Setup;
using System.Data;
using System.Reflection;

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

        public Task SetupDatabase(DatabaseFacade dbFacade)
        {
            throw new NotImplementedException();
        }
    }
}
