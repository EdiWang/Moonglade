using Dapper;
using System.Data;

namespace Moonglade.Data.Setup
{
    public abstract class SetupRunnerBase
    {
        private readonly IDbConnection _dbConnection;
        public void InitFirstRun()
        {
            SetupDatabase();
            ResetDefaultConfiguration();
            InitSampleData();
        }

        public SetupRunnerBase(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        /// <summary>
        /// Clear all data in database but preserve tables schema
        /// </summary>
        public virtual void ClearData()
        {
            // Clear Relation Tables
            _dbConnection.Execute("DELETE FROM PostTag");
            _dbConnection.Execute("DELETE FROM PostCategory");
            _dbConnection.Execute("DELETE FROM CommentReply");

            // Clear Individual Tables
            _dbConnection.Execute("DELETE FROM Category");
            _dbConnection.Execute("DELETE FROM Tag");
            _dbConnection.Execute("DELETE FROM Comment");
            _dbConnection.Execute("DELETE FROM FriendLink");
            _dbConnection.Execute("DELETE FROM PingbackHistory");
            _dbConnection.Execute("DELETE FROM PostExtension");
            _dbConnection.Execute("DELETE FROM Post");
            _dbConnection.Execute("DELETE FROM Menu");

            // Clear Configuration Table
            _dbConnection.Execute("DELETE FROM BlogConfiguration");
            _dbConnection.Execute("DELETE FROM BlogAsset");

            // Clear LocalAccount Table
            _dbConnection.Execute("DELETE FROM LocalAccount");
        }

        public virtual bool TestDatabaseConnection()
        {
            var result = _dbConnection.ExecuteScalar<int>("SELECT 1");
            return result == 1;
        }

        public virtual void ResetDefaultConfiguration()
        {
            var sql = GetEmbeddedSqlScript("seed-configuration");
            if (!string.IsNullOrWhiteSpace(sql))
            {
                _dbConnection.Execute(sql);
            }
            else
            {
                throw new InvalidDataException("SQL Script is empty.");
            }
        }

        public virtual void InitSampleData()
        {
            var sql = GetEmbeddedSqlScript("seed-sampledata");
            if (!string.IsNullOrWhiteSpace(sql))
            {
                _dbConnection.Execute(sql);
            }
            else
            {
                throw new InvalidDataException("SQL Script is empty.");
            }
        }

        protected abstract string GetEmbeddedSqlScript(string scriptName);

        public abstract void SetupDatabase();
    }
}
