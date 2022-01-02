using Dapper;
using Moonglade.Data.Setup;
using System.Data;
using System.Reflection;

namespace Moonglade.Data.MySql.Setup
{
    public class SetupRunnerForMySql : SetupRunnerBase, ISetupRunner
    {
        private readonly IDbConnection _dbConnection;

        public SetupRunnerForMySql(IDbConnection dbConnection)
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

        public override void InitSampleData()
        {
            var catId = Guid.NewGuid();
            _dbConnection.Execute("INSERT INTO Category(Id, DisplayName, Note, RouteName) VALUES (@catId, 'Default', 'Default Category', 'default')", new { catId });
            var postId = Guid.NewGuid();
            var postCotent = "Moonglade is the new blog system for https://edi.wang. It is a complete rewrite of the old system using .NET 5 and runs on Microsoft Azure.";

            var addPostText = @"INSERT INTO Post(Id, Title, Slug, Author, PostContent, CommentEnabled, CreateTimeUtc, ContentAbstract, IsPublished, IsFeatured, IsFeedIncluded, LastModifiedUtc, IsDeleted, PubDateUtc, ContentLanguageCode, HashCheckSum, IsOriginal) 
VALUES (@postId, 'Welcome to Moonglade', 'welcome-to-moonglade', 'admin', @postCotent, 1, '2021-1-1', @postCotent, 1, 0, 1, NULL, 0, NOW(), 'en-us', -1688639577, 1);";
            _dbConnection.Execute(addPostText, new { postId, postCotent });

            var addPostExtensionText = @"INSERT INTO PostExtension(PostId,  Hits,  Likes) 
VALUES (@postId,  1024,  512);
INSERT INTO PostCategory (PostId, CategoryId) VALUES (@postId, @catId);";

            _dbConnection.Execute(addPostExtensionText, new { postId, catId });

            base.InitSampleData();
        }

        protected override string? GetEmbeddedSqlScript(string scriptName)
        {
            var assembly = typeof(SetupRunnerForMySql).GetTypeInfo().Assembly;
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
