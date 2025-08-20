using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Moonglade.Web.HealthChecks;

public class DatabaseConnectivityHealthCheck(BlogDbContext dbContext) : IHealthCheck
{
    private readonly BlogDbContext _dbContext = dbContext;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Simple connectivity test
            await _dbContext.Database.CanConnectAsync(cancellationToken);

            // Optional: Check if we can read from a key table
            var catCount = await _dbContext.Category.CountAsync(cancellationToken);

            return HealthCheckResult.Healthy("Database is accessible",
                new Dictionary<string, object>
                {
                    ["catCount"] = catCount,
                    ["provider"] = _dbContext.Database.ProviderName
                });
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database connectivity failed", ex);
        }
    }
}