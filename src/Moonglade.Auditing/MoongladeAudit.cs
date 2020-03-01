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
    public class MoongladeAudit
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
                var uname = string.Empty;
                var ip = "0.0.0.0";

                if (null != _httpContextAccessor)
                {
                    uname = _httpContextAccessor.HttpContext.User?.Identity?.Name;
                    ip = _httpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString();
                }

                if (message.Length > 256)
                {
                    // Truncate message so that SQL won't blow up
                    message = message.Substring(0, 256);
                }

                var auditEntry = new AuditEntry(eventType, eventId, uname, ip, message);

                var connStr = _configuration.GetConnectionString(Constants.DbConnectionName);
                await using var conn = new SqlConnection(connStr);

                // TODO
                var sql = "";

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
            throw new NotImplementedException();
        }
    }
}
