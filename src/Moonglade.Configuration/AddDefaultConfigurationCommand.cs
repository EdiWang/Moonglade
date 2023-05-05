using MediatR;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Configuration;

public record AddDefaultConfigurationCommand(int Id, string CfgKey, string DefaultJson) : IRequest<OperationCode>;

public class AddDefaultConfigurationCommandHandler : IRequestHandler<AddDefaultConfigurationCommand, OperationCode>
{
    private readonly IRepository<BlogConfigurationEntity> _repository;
    public AddDefaultConfigurationCommandHandler(IRepository<BlogConfigurationEntity> repository) => _repository = repository;

    public async Task<OperationCode> Handle(AddDefaultConfigurationCommand request, CancellationToken ct)
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