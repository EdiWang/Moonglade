using LiteBus.Queries.Abstractions;
using Moonglade.Data;
using Moonglade.Data.Entities;

namespace Moonglade.Configuration;

public record GetAllConfigurationsQuery : IQuery<IDictionary<string, string>>;

public class GetAllConfigurationsQueryHandler(MoongladeRepository<BlogConfigurationEntity> repo) : IQueryHandler<GetAllConfigurationsQuery, IDictionary<string, string>>
{
    public async Task<IDictionary<string, string>> HandleAsync(GetAllConfigurationsQuery request, CancellationToken ct)
    {
        var entities = await repo.ListAsync(ct);
        return entities.ToDictionary(k => k.CfgKey, v => v.CfgValue);
    }
}