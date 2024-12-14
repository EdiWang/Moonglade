using MediatR;
using Microsoft.Extensions.Logging;
using Moonglade.Data;
using Moonglade.Data.Entities;

namespace Moonglade.Configuration;

public record AddDefaultConfigurationCommand(int Id, string CfgKey, string DefaultJson) : IRequest<OperationCode>;

public class AddDefaultConfigurationCommandHandler(
    MoongladeRepository<BlogConfigurationEntity> repository,
    ILogger<AddDefaultConfigurationCommandHandler> logger
    ) : IRequestHandler<AddDefaultConfigurationCommand, OperationCode>
{
    public async Task<OperationCode> Handle(AddDefaultConfigurationCommand request, CancellationToken ct)
    {
        var entity = new BlogConfigurationEntity
        {
            Id = request.Id,
            CfgKey = request.CfgKey,
            CfgValue = request.DefaultJson,
            LastModifiedTimeUtc = DateTime.UtcNow
        };

        await repository.AddAsync(entity, ct);

        logger.LogInformation("Added default configuration: {CfgKey}", request.CfgKey);
        return OperationCode.Done;
    }
}