using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moonglade.Model;

namespace Moonglade.Syndication
{
    public interface ISyndicationData
    {
        Task<Guid> GetCategoryId(string categoryName);
    }

    public class SyndicationData : ISyndicationData
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SyndicationData> _logger;

        public SyndicationData(IConfiguration configuration, ILogger<SyndicationData> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<Guid> GetCategoryId(string categoryName)
        {
            try
            {
                var connStr = _configuration.GetConnectionString(Constants.DbConnectionName);
                await using var conn = new SqlConnection(connStr);

                var sql = @"SELECT TOP 1 c.Id FROM Category c WHERE c.Title = @categoryName";
                var guid = await conn.ExecuteScalarAsync<Guid>(sql, new { categoryName });
                
                return guid;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return Guid.Empty;
            }
        }
    }
}
