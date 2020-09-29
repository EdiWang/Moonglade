using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moonglade.Model;

namespace Moonglade.Pingback
{
    public class PingbackService : IPingbackService
    {
        private readonly ILogger<PingbackService> _logger;
        private readonly IConfiguration _configuration;

        public PingbackService(ILogger<PingbackService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<IEnumerable<PingbackHistory>> GetPingbackHistoryAsync()
        {
            try
            {
                var connStr = _configuration.GetConnectionString(Constants.DbConnectionName);
                await using var conn = new SqlConnection(connStr);
                var sql = $"SELECT ph.{nameof(PingbackHistory.Id)}, " +
                          $"ph.{nameof(PingbackHistory.Domain)}, " +
                          $"ph.{nameof(PingbackHistory.SourceUrl)}, " +
                          $"ph.{nameof(PingbackHistory.SourceTitle)}, " +
                          $"ph.{nameof(PingbackHistory.TargetPostId)}, " +
                          $"ph.{nameof(PingbackHistory.TargetPostTitle)}, " +
                          $"ph.{nameof(PingbackHistory.PingTimeUtc)} " +
                          $"FROM {nameof(PingbackHistory)} ph";

                var list = await conn.QueryAsync<PingbackHistory>(sql);
                return list;
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error {nameof(GetPingbackHistoryAsync)}");
                throw;
            }
        }
    }

    public interface IPingbackService
    {
        Task<IEnumerable<PingbackHistory>> GetPingbackHistoryAsync();
    }
}
