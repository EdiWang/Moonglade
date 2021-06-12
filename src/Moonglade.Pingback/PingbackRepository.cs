using System;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dapper;

namespace Moonglade.Pingback
{
    public interface IPingbackRepository
    {
        Task<(Guid Id, string Title)> GetPostIdTitle(string url, IDbConnection conn);
        Task<IEnumerable<PingbackRecord>> GetPingbackHistoryAsync(IDbConnection conn);
        Task<bool> HasAlreadyBeenPinged(Guid postId, string sourceUrl, string sourceIp, IDbConnection conn);
    }

    public class PingbackRepository : IPingbackRepository
    {
        public async Task<(Guid Id, string Title)> GetPostIdTitle(string url, IDbConnection conn)
        {
            var (slug, pubDate) = GetSlugInfoFromPostUrl(url);
            const string sql = "SELECT p.Id, p.Title FROM Post p " +
                               "WHERE p.IsPublished = '1' " +
                               "AND p.IsDeleted = '0'" +
                               "AND p.Slug = @slug " +
                               "AND YEAR(p.PubDateUtc) = @year " +
                               "AND MONTH(p.PubDateUtc) = @month " +
                               "AND DAY(p.PubDateUtc) = @day";
            var p = await conn.QueryFirstOrDefaultAsync<(Guid Id, string Title)>(sql, new
            {
                slug,
                year = pubDate.Year,
                month = pubDate.Month,
                day = pubDate.Day
            });
            return p;
        }

        public async Task<IEnumerable<PingbackRecord>> GetPingbackHistoryAsync(IDbConnection conn)
        {
            var sql = $"SELECT p.{nameof(PingbackRecord.Id)}, " +
                          $"p.{nameof(PingbackRecord.Domain)}, " +
                          $"p.{nameof(PingbackRecord.SourceUrl)}, " +
                          $"p.{nameof(PingbackRecord.SourceTitle)}, " +
                          $"p.{nameof(PingbackRecord.TargetPostId)}, " +
                          $"p.{nameof(PingbackRecord.TargetPostTitle)}, " +
                          $"p.{nameof(PingbackRecord.PingTimeUtc)} " +
                          "FROM Pingback p";

            var list = await conn.QueryAsync<PingbackRecord>(sql);
            return list;
        }

        public async Task<bool> HasAlreadyBeenPinged(Guid postId, string sourceUrl, string sourceIp, IDbConnection conn)
        {
            var sql = "SELECT TOP 1 1 FROM Pingback p " +
                          "WHERE p.TargetPostId = @postId " +
                          "AND p.SourceUrl = @sourceUrl " +
                          "AND p.SourceIp = @sourceIp";
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
}
