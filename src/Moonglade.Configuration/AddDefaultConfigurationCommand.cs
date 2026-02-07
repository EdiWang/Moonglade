using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;
using Moonglade.Data;
using Moonglade.Data.Entities;
using System.ComponentModel.DataAnnotations;

namespace Moonglade.Configuration;

public record AddDefaultConfigurationCommand(
    [Required, MaxLength(64)] string CfgKey,
    [Required] string DefaultJson) : ICommand<OperationCode>;

public class AddDefaultConfigurationCommandHandler(
    IRepositoryBase<BlogConfigurationEntity> repository,
    ILogger<AddDefaultConfigurationCommandHandler> logger
    ) : ICommandHandler<AddDefaultConfigurationCommand, OperationCode>
{
    public async Task<OperationCode> HandleAsync(AddDefaultConfigurationCommand request, CancellationToken ct)
    {
        try
        {
            var entity = new BlogConfigurationEntity
            {
                CfgKey = request.CfgKey,
                CfgValue = request.DefaultJson,
                LastModifiedTimeUtc = DateTime.UtcNow
            };

            await repository.AddAsync(entity, ct);

            logger.LogInformation("Successfully added default configuration: {CfgKey}", request.CfgKey);
            return OperationCode.Done;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to add default configuration: {CfgKey}", request.CfgKey);
            throw;
        }
    }
}