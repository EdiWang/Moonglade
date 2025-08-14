using LiteBus.Queries.Abstractions;
using Moonglade.Data;
using Moonglade.Data.Entities;

namespace Moonglade.Configuration;

public record ListConfigurationsQuery : IQuery<IDictionary<string, string>>;

public class ListConfigurationsQueryHandler(MoongladeRepository<BlogConfigurationEntity> repo) : IQueryHandler<ListConfigurationsQuery, IDictionary<string, string>>
{
    public async Task<IDictionary<string, string>> HandleAsync(ListConfigurationsQuery request, CancellationToken ct)
    {
        var entities = await repo.ListAsync(ct);
        return entities.ToDictionary(k => k.CfgKey, v => v.CfgValue);
    }
}