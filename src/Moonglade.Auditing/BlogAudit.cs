using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moonglade.Model;
using Moonglade.Model.Settings;

namespace Moonglade.Auditing
{
    public class BlogAudit : IBlogAudit
    {
        private readonly ILogger<BlogAudit> _logger;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public BlogAudit(
            ILogger<BlogAudit> logger,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task AddAuditEntry(EventType eventType, AuditEventId auditEventId, string message)
        {
            try
            {
                if (!IsAuditLogEnabled()) { return; }

                (string username, string ipv4) = GetUsernameAndIp();

                // Truncate data so that SQL won't blow up
                if (message.Length > 256)
                {
                    message = message.Substring(0, 256);
                }

                string machineName = Environment.MachineName;
                if (machineName.Length > 32)
                {
                    machineName = machineName.Substring(0, 32);
                }

                var auditEntry = new AuditEntry(eventType, auditEventId, username, ipv4, machineName, message);

                var connStr = _configuration.GetConnectionString(Constants.DbConnectionName);
                await using var conn = new SqlConnection(connStr);

                var sql = @"INSERT INTO AuditLog([EventId],[EventType],[EventTimeUtc],[WebUsername],[IpAddressV4],[MachineName],[Message])
                            VALUES(@EventId, @EventType, @EventTimeUtc, @Username, @IpAddressV4, @MachineName, @Message)";

                await conn.ExecuteAsync(sql, auditEntry);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }

        public async Task<(IReadOnlyList<AuditEntry> Entries, int Count)> GetAuditEntries(
            int skip, int take, EventType? eventType = null, AuditEventId? eventId = null)
        {
            var connStr = _configuration.GetConnectionString(Constants.DbConnectionName);
            await using var conn = new SqlConnection(connStr);

            var sql = @"SELECT al.EventId, 
                                   al.EventType, 
                                   al.EventTimeUtc, 
                                   al.[Message],
                                   al.WebUsername as [Username], 
                                   al.IpAddressV4, 
                                   al.MachineName
                            FROM AuditLog al 
                            WITH(NOLOCK)
                            WHERE 1 = 1 
                            AND (@EventType IS NULL OR al.EventType = @EventType)
                            AND (@EventId IS NULL OR al.EventId = @EventId)
                            ORDER BY al.EventTimeUtc DESC
                            OFFSET @Skip ROWS
                            FETCH NEXT @Take ROWS ONLY

                            SELECT COUNT(al.Id)
                            FROM AuditLog al
                            WHERE 1 = 1
                            AND(@EventType IS NULL OR al.EventType = @EventType)
                            AND(@EventId IS NULL OR al.EventId = @EventId);";

            using var multi = await conn.QueryMultipleAsync(sql, new
            {
                eventType,
                eventId,
                skip,
                take
            });

            var entries = multi.Read<AuditEntry>().ToList();
            var count = multi.ReadFirstOrDefault<int>();
            var returnType = (entries, count);

            return returnType;
        }

        public async Task ClearAuditLog()
        {
            if (!IsAuditLogEnabled()) { return; }

            var connStr = _configuration.GetConnectionString(Constants.DbConnectionName);
            await using var conn = new SqlConnection(connStr);

            var sql = "DELETE FROM AuditLog";
            await conn.ExecuteAsync(sql);

            // Make sure who ever doing this can't get away with it
            (string username, string ipv4) = GetUsernameAndIp();
            await AddAuditEntry(EventType.General, AuditEventId.ClearedAuditLog, $"Audit log was cleared by '{username}' from '{ipv4}'");
        }

        private (string Username, string Ipv4) GetUsernameAndIp()
        {
            var uname = string.Empty;
            var ip = "0.0.0.0";

            if (null != _httpContextAccessor)
            {
                uname = _httpContextAccessor.HttpContext.User?.Identity?.Name ?? "N/A";
                ip = _httpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString();
            }

            return (uname, ip);
        }

        private bool IsAuditLogEnabled()
        {
            string enableAuditLogSettings = _configuration[$"{nameof(AppSettings)}:{nameof(AppSettings.EnableAudit)}"];
            return !string.IsNullOrWhiteSpace(enableAuditLogSettings) && bool.Parse(enableAuditLogSettings);
        }
    }
}
