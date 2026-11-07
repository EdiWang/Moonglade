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
    BlogDbContext db,
    ILogger<AddDefaultConfigurationCommandHandler> logger
    ) : ICommandHandler<AddDefaultConfigurationCommand, OperationCode>
{
    public async Task<OperationCode> HandleAsync(AddDefaultConfigurationCommand request, CancellationToken ct)
    {
        try
        {
            var entity = await db.BlogConfiguration.FindAsync([request.CfgKey], ct);
            if (entity is null)
            {
                entity = new BlogConfigurationEntity { CfgKey = request.CfgKey };
                await db.BlogConfiguration.AddAsync(entity, ct);
            }
            else
            {
                logger.LogWarning("Replacing existing configuration with defaults: {CfgKey}", request.CfgKey);
            }

            entity.CfgValue = request.DefaultJson;
            entity.LastModifiedTimeUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);

            logger.LogInformation("Successfully initialized default configuration: {CfgKey}", request.CfgKey);
            return OperationCode.Done;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to add default configuration: {CfgKey}", request.CfgKey);
            throw;
        }
    }
}
