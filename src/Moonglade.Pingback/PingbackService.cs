using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

        public async Task DeletePingbackHistory(Guid id)
        {
            try
            {
                var connStr = _configuration.GetConnectionString(Constants.DbConnectionName);
                await using var conn = new SqlConnection(connStr);
                var sql = $"DELETE FROM {nameof(PingbackHistory)} WHERE Id = @id";
                await conn.ExecuteAsync(sql, new { id });
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error {nameof(id)}");
                throw;
            }
        }

        private async Task SavePingbackRecordAsync(PingbackHistory request)
        {
            var connStr = _configuration.GetConnectionString(Constants.DbConnectionName);
            await using var conn = new SqlConnection(connStr);

            var sql = $"INSERT INTO {nameof(PingbackHistory)}" +
                      $"(Id, Domain, SourceUrl, SourceTitle, SourceIp, TargetPostId, PingTimeUtc, TargetPostTitle) " +
                      $"VALUES (@id, @domain, @sourceUrl, @sourceTitle, @targetPostId, @pingTimeUtc, @targetPostTitle)";
            await conn.ExecuteAsync(sql, request);
        }

        private async Task<(Guid Id, string Title)> GetPostIdTitle(string url)
        {
            var slugInfo = GetSlugInfoFromPostUrl(url);

            var connStr = _configuration.GetConnectionString(Constants.DbConnectionName);
            await using var conn = new SqlConnection(connStr);
            var sql = "SELECT p.Id, p.Title FROM Post p " +
                      "WHERE p.IsPublished = '1' " +
                      "AND p.IsDeleted = '0'" +
                      "AND p.Slug = @slug " +
                      "AND YEAR(p.PubDateUtc) = @year " +
                      "AND MONTH(p.PubDateUtc) = @month " +
                      "AND DAY(p.PubDateUtc) = @day";
            var p = await conn.QueryFirstOrDefaultAsync<(Guid Id, string Title)>(sql, new
            {
                slug = slugInfo.Slug,
                year = slugInfo.PubDate.Year,
                month = slugInfo.PubDate.Month,
                day = slugInfo.PubDate.Day
            });
            return p;
        }

        private async Task<bool> HasAlreadyBeenPinged(Guid postId, string sourceUrl, string sourceIp)
        {
            var connStr = _configuration.GetConnectionString(Constants.DbConnectionName);
            await using var conn = new SqlConnection(connStr);
            var sql = $"SELECT TOP 1 1 FROM {nameof(PingbackHistory)} ph " +
                      $"WHERE ph.TargetPostId = @postId " +
                      $"AND ph.SourceUrl = @sourceUrl " +
                      $"AND ph.SourceIp = @sourceIp";
            var result = await conn.ExecuteScalarAsync<int>(sql, new { postId, sourceUrl, sourceIp });
            return result == 1;
        }

        private static (string Slug, DateTime PubDate) GetSlugInfoFromPostUrl(string url)
        {
            var blogSlugRegex = new Regex(@"^https?:\/\/.*\/post\/(?<yyyy>\d{4})\/(?<MM>\d{1,12})\/(?<dd>\d{1,31})\/(?<slug>.*)$");
            Match match = blogSlugRegex.Match(url);
            if (!match.Success)
            {
                throw new FormatException("Invalid Slug Format");
            }

            int year = int.Parse(match.Groups["yyyy"].Value);
            int month = int.Parse(match.Groups["MM"].Value);
            int day = int.Parse(match.Groups["dd"].Value);
            string slug = match.Groups["slug"].Value;
            var date = new DateTime(year, month, day);

            return (slug, date);
        }
    }

    public interface IPingbackService
    {
        Task<IEnumerable<PingbackHistory>> GetPingbackHistoryAsync();
        Task DeletePingbackHistory(Guid id);
    }
}
