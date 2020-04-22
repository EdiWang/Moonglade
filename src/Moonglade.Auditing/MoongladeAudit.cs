using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Edi.Practice.RequestResponseModel;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moonglade.Model;
using Moonglade.Model.Settings;

namespace Moonglade.Auditing
{
    public class MoongladeAudit : IMoongladeAudit
    {
        private readonly ILogger<MoongladeAudit> _logger;

        private readonly IConfiguration _configuration;

        private readonly IHttpContextAccessor _httpContextAccessor;

        public MoongladeAudit(
            ILogger<MoongladeAudit> logger,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<Response> AddAuditEntry(EventType eventType, EventId eventId, string message)
        {
            try
            {
                if (!IsAuditLogEnabled())
                {
                    return new FailedResponse("Audit Log is disabled.");
                }

                var ui = GetUsernameAndIp();

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

                var auditEntry = new AuditEntry(eventType, eventId, ui.Username, ui.Ipv4, machineName, message);

                var connStr = _configuration.GetConnectionString(Constants.DbConnectionName);
                await using var conn = new SqlConnection(connStr);

                var sql = @"INSERT INTO AuditLog([EventId],[EventType],[EventTimeUtc],[WebUsername],[IpAddressV4],[MachineName],[Message])
                            VALUES(@EventId, @EventType, @EventTimeUtc, @Username, @IpAddressV4, @MachineName, @Message)";

                int rows = await conn.ExecuteAsync(sql, auditEntry);
                return new Response(rows > 0);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return new FailedResponse((int)ResponseFailureCode.GeneralException, e.Message, e);
            }
        }

        public async Task<Response<(IReadOnlyList<AuditEntry> Entries, int Count)>> GetAuditEntries(
            int skip, int take, EventType? eventType = null, EventId? eventId = null)
        {
            try
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

                return new SuccessResponse<(IReadOnlyList<AuditEntry> Entries, int Count)>(returnType);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return new FailedResponse<(IReadOnlyList<AuditEntry> Entries, int Count)>((int)ResponseFailureCode.GeneralException, e.Message, e);
            }
        }

        public async Task<Response> ClearAuditLog()
        {
            try
            {
                if (!IsAuditLogEnabled())
                {
                    return new FailedResponse("Audit Log is disabled.");
                }

                var connStr = _configuration.GetConnectionString(Constants.DbConnectionName);
                await using var conn = new SqlConnection(connStr);

                var sql = "DELETE FROM AuditLog";
                int rows = await conn.ExecuteAsync(sql);

                // Make sure who ever doing this can't get away with it
                var ui = GetUsernameAndIp();
                await AddAuditEntry(EventType.General, EventId.ClearedAuditLog, $"Audit log was cleared by '{ui.Username}' from '{ui.Ipv4}'");

                return new Response(rows > 0);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return new FailedResponse((int)ResponseFailureCode.GeneralException, e.Message, e);
            }
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
