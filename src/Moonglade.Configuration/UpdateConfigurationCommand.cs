using MediatR;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Configuration;

public record UpdateConfigurationCommand(string Name, string Json) : IRequest<OperationCode>;

public class UpdateConfigurationCommandHandler : IRequestHandler<UpdateConfigurationCommand, OperationCode>
{
    private readonly IRepository<BlogConfigurationEntity> _repository;
    public UpdateConfigurationCommandHandler(IRepository<BlogConfigurationEntity> repository) => _repository = repository;

    public async Task<OperationCode> Handle(UpdateConfigurationCommand request, CancellationToken ct)
    {
        var (name, json) = request;
        var entity = await _repository.GetAsync(p => p.CfgKey == name);
        if (entity == null) return OperationCode.ObjectNotFound;

        entity.CfgValue = json;
        entity.LastModifiedTimeUtc = DateTime.UtcNow;

        await _repository.UpdateAsync(entity, ct);
        return OperationCode.Done;
    }
}