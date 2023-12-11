using MediatR;
using Moonglade.Data;
using Moonglade.Data.Generated.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Configuration;

public record AddEmptyConfigurationCommand(int Id, string CfgKey, string DefaultJson) : IRequest<OperationCode>;

public class AddEmptyConfigurationCommandHandler : IRequestHandler<AddEmptyConfigurationCommand, OperationCode>
{
    private readonly IRepository<BlogConfigurationEntity> _repository;
    public AddEmptyConfigurationCommandHandler(IRepository<BlogConfigurationEntity> repository) => _repository = repository;

    public async Task<OperationCode> Handle(AddEmptyConfigurationCommand request, CancellationToken ct)
    {
        var entity = new BlogConfigurationEntity
        {
            Id = request.Id,
            CfgKey = request.CfgKey,
            CfgValue = request.DefaultJson,
            LastModifiedTimeUtc = DateTime.UtcNow
        };

        await _repository.AddAsync(entity, ct);
        return OperationCode.Done;
    }
}