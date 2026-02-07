using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Data.Specifications;

namespace Moonglade.Configuration;

public record UpdateConfigurationCommand(string Name, string Json) : ICommand<OperationCode>;

public class UpdateConfigurationCommandHandler(
    IRepositoryBase<BlogConfigurationEntity> repository,
    ILogger<UpdateConfigurationCommandHandler> logger
    ) : ICommandHandler<UpdateConfigurationCommand, OperationCode>
{
    public async Task<OperationCode> HandleAsync(UpdateConfigurationCommand request, CancellationToken ct)
    {
        var (name, json) = request;

        try
        {
            var entity = await repository.FirstOrDefaultAsync(new BlogConfigurationSpec(name), ct);
            if (entity == null)
            {
                logger.LogWarning("Configuration entity not found: {Name}", name);
                return OperationCode.ObjectNotFound;
            }

            // Check if the value has actually changed to avoid unnecessary updates
            if (entity.CfgValue == json)
            {
                logger.LogDebug("Configuration value unchanged for {Name}, skipping update", name);
                return OperationCode.Done;
            }

            entity.CfgValue = json;
            entity.LastModifiedTimeUtc = DateTime.UtcNow;

            await repository.UpdateAsync(entity, ct);

            logger.LogInformation("Configuration updated successfully: {Name}", name);
            return OperationCode.Done;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update configuration {Name}", name);
            throw;
        }
    }
}