using LiteBus.Commands.Abstractions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Data.Specifications;

namespace Moonglade.Configuration;

public record UpdateConfigurationCommand(string Name, string Json) : ICommand<OperationCode>;

public class UpdateConfigurationCommandHandler(
    MoongladeRepository<BlogConfigurationEntity> repository,
    ILogger<UpdateConfigurationCommandHandler> logger
    ) : ICommandHandler<UpdateConfigurationCommand, OperationCode>
{
    public async Task<OperationCode> HandleAsync(UpdateConfigurationCommand request, CancellationToken ct)
    {
        var (name, json) = request;
        var entity = await repository.FirstOrDefaultAsync(new BlogConfigurationSpec(name), ct);
        if (entity == null) return OperationCode.ObjectNotFound;

        entity.CfgValue = json;
        entity.LastModifiedTimeUtc = DateTime.UtcNow;

        await repository.UpdateAsync(entity, ct);

        logger.LogInformation("Configuration updated: {Name}", name);
        return OperationCode.Done;
    }
}