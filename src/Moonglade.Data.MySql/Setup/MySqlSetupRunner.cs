using Dapper;
using Moonglade.Data.Setup;
using System.Data;

namespace Moonglade.Data.MySql.Setup
{
    public class MySqlSetupRunner : ISetupRunner
    {
        private readonly IDbConnection _dbConnection;

        public MySqlSetupRunner(IDbConnection dbConnection)
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
    }
}
