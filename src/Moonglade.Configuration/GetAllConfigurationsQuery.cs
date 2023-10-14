using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Configuration;

public record GetAllConfigurationsQuery : IRequest<IDictionary<string, string>>;

public class GetAllConfigurationsQueryHandler(IRepository<BlogConfigurationEntity> repo) : IRequestHandler<GetAllConfigurationsQuery, IDictionary<string, string>>
{
    public async Task<IDictionary<string, string>> Handle(GetAllConfigurationsQuery request, CancellationToken ct)
    {
        var entities = await repo.SelectAsync(p => new { p.CfgKey, p.CfgValue }, ct);
        return entities.ToDictionary(k => k.CfgKey, v => v.CfgValue);
    }
}