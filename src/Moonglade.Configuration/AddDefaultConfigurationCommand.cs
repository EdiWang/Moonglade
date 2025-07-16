using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;
using Moonglade.Data;
using Moonglade.Data.Entities;

namespace Moonglade.Configuration;

public record AddDefaultConfigurationCommand(string CfgKey, string DefaultJson) : ICommand<OperationCode>;

public class AddDefaultConfigurationCommandHandler(
    MoongladeRepository<BlogConfigurationEntity> repository,
    ILogger<AddDefaultConfigurationCommandHandler> logger
    ) : ICommandHandler<AddDefaultConfigurationCommand, OperationCode>
{
    public async Task<OperationCode> HandleAsync(AddDefaultConfigurationCommand request, CancellationToken ct)
    {
        var entity = new BlogConfigurationEntity
        {
            CfgKey = request.CfgKey,
            CfgValue = request.DefaultJson,
            LastModifiedTimeUtc = DateTime.UtcNow
        };

        await repository.AddAsync(entity, ct);

        logger.LogInformation("Added default configuration: {CfgKey}", request.CfgKey);
        return OperationCode.Done;
    }
}