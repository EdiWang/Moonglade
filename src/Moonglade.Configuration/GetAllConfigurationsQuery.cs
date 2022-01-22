using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Configuration;

public class GetAllConfigurationsQuery : IRequest<IDictionary<string, string>>
{

}

public class GetAllConfigurationsQueryHandler : IRequestHandler<GetAllConfigurationsQuery, IDictionary<string, string>>
{
    private readonly IRepository<BlogConfigurationEntity> _repository;

    public GetAllConfigurationsQueryHandler(IRepository<BlogConfigurationEntity> repository)
    {
        _repository = repository;
    }

    public async Task<IDictionary<string, string>> Handle(GetAllConfigurationsQuery request, CancellationToken cancellationToken)
    {
        var entities = await _repository.SelectAsync(p => new { p.CfgKey, p.CfgValue });
        return entities.ToDictionary(k => k.CfgKey, v => v.CfgValue);
    }
}