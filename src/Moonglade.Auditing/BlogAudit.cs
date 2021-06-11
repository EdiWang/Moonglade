using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Auditing
{
    public class BlogAudit : IBlogAudit
    {
        private readonly ILogger<BlogAudit> _logger;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IFeatureManager _featureManager;
        private readonly IRepository<AuditLogEntity> _auditLogRepo;

        private readonly string _dbName = "MoongladeDatabase";

        public BlogAudit(
            ILogger<BlogAudit> logger,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor,
            IFeatureManager featureManager,
            IRepository<AuditLogEntity> auditLogRepo)
        {
            _logger = logger;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
            _featureManager = featureManager;
            _auditLogRepo = auditLogRepo;
        }

        public async Task AddAuditEntry(EventType eventType, AuditEventId auditEventId, string message)
        {
            try
            {
                if (!await IsAuditLogEnabled()) return;

                var (username, ipv4) = GetUsernameAndIp();

                // Truncate data so that SQL won't blow up
                if (message.Length > 256) message = message[..256];

                var machineName = Environment.MachineName;
                if (machineName.Length > 32) machineName = machineName[..32];

                var entity = new AuditLogEntity
                {
                    EventId = (int)auditEventId,
                    EventType = (int)eventType,
                    EventTimeUtc = DateTime.UtcNow,
                    IpAddressV4 = ipv4,
                    MachineName = machineName,
                    Message = message,
                    WebUsername = username
                };
                await _auditLogRepo.AddAsync(entity);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }

        public async Task<(IReadOnlyList<AuditEntry> Entries, int Count)> GetAuditEntries(
            int skip, int take, EventType? eventType = null, AuditEventId? eventId = null)
        {
            var connStr = _configuration.GetConnectionString(_dbName);
            await using var conn = new SqlConnection(connStr);

            const string sql = @"SELECT al.EventId, 
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
                            FETCH NEXT @Take ROWS ONLY";

            var entries = await conn.QueryAsync<AuditEntry>(sql, new
            {
                eventType,
                eventId,
                skip,
                take
            });

            var totalRows = _auditLogRepo.Count();
            var returnType = (entries.ToList(), totalRows);

            return returnType;
        }

        public async Task ClearAuditLog()
        {
            if (!await IsAuditLogEnabled()) return;

            await _auditLogRepo.ExecuteSqlRawAsync("DELETE FROM AuditLog");

            // Make sure who ever doing this can't get away with it
            var (username, ipv4) = GetUsernameAndIp();
            await AddAuditEntry(EventType.General, AuditEventId.ClearedAuditLog, $"Audit log was cleared by '{username}' from '{ipv4}'");
        }

        private (string Username, string Ipv4) GetUsernameAndIp()
        {
            var uname = string.Empty;
            var ip = "0.0.0.0";

            if (_httpContextAccessor?.HttpContext is not null)
            {
                uname = _httpContextAccessor.HttpContext.User.Identity?.Name ?? "N/A";
                ip = _httpContextAccessor.HttpContext.Connection.RemoteIpAddress?.ToString();
            }

            return (uname, ip);
        }

        private async Task<bool> IsAuditLogEnabled()
        {
            var flag = await _featureManager.IsEnabledAsync("EnableAudit");
            return flag;
        }
    }
}
