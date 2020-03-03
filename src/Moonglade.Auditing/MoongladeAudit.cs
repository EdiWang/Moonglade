using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Edi.Practice.RequestResponseModel;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moonglade.Model;

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
                var ui = GetUsernameAndIp();

                if (message.Length > 256)
                {
                    // Truncate message so that SQL won't blow up
                    message = message.Substring(0, 256);
                }

                var auditEntry = new AuditEntry(eventType, eventId, ui.Username, ui.Ipv4, message);

                var connStr = _configuration.GetConnectionString(Constants.DbConnectionName);
                await using var conn = new SqlConnection(connStr);

                var sql = @"INSERT INTO AuditLog([EventId],[EventType],[EventTimeUtc],[WebUsername],[IpAddressV4],[Message])
                            VALUES(@EventId, @EventType, @EventTimeUtc, @Username, @IpAddressV4, @Message)";

                int rows = await conn.ExecuteAsync(sql, auditEntry);
                return new Response(rows > 0);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return new FailedResponse((int)ResponseFailureCode.GeneralException, e.Message, e);
            }
        }

        public async Task<Response<IReadOnlyList<AuditEntry>>> GetAuditEntries(
            int skip, int take, EventType? eventType, EventId? eventId, bool orderByTimeDesc = true)
        {
            try
            {
                throw new NotImplementedException();
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return new FailedResponse<IReadOnlyList<AuditEntry>>((int)ResponseFailureCode.GeneralException, e.Message, e);
            }
        }

        public async Task<Response> ClearAuditLog()
        {
            try
            {
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
                uname = _httpContextAccessor.HttpContext.User?.Identity?.Name;
                ip = _httpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString();
            }

            return (uname, ip);
        }
    }
}
