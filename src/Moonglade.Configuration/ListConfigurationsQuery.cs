using LiteBus.Queries.Abstractions;
using Microsoft.EntityFrameworkCore;
using Moonglade.Data;

namespace Moonglade.Configuration;

public record ListConfigurationsQuery : IQuery<IDictionary<string, string>>;

public class ListConfigurationsQueryHandler(BlogDbContext db) : IQueryHandler<ListConfigurationsQuery, IDictionary<string, string>>
{
    public async Task<IDictionary<string, string>> HandleAsync(ListConfigurationsQuery request, CancellationToken ct)
    {
        return await db.BlogConfiguration
            .AsNoTracking()
            .ToDictionaryAsync(k => k.CfgKey, v => v.CfgValue, ct);
    }
}