using MediatR;
using Moonglade.Data;
using Moonglade.Data.Entities;

namespace Moonglade.Configuration;

public record GetAllConfigurationsQuery : IRequest<IDictionary<string, string>>;

public class GetAllConfigurationsQueryHandler(MoongladeRepository<BlogConfigurationEntity> repo) : IRequestHandler<GetAllConfigurationsQuery, IDictionary<string, string>>
{
    public async Task<IDictionary<string, string>> Handle(GetAllConfigurationsQuery request, CancellationToken ct)
    {
        var entities = await repo.ListAsync(ct);
        return entities.ToDictionary(k => k.CfgKey, v => v.CfgValue);
    }
}