using LiteBus.Commands.Abstractions;
using LiteBus.Events.Abstractions;
using Microsoft.Extensions.Logging;
using Moonglade.Data;
using System.ComponentModel.DataAnnotations;

namespace Moonglade.Configuration;

public record UpdateConfigurationCommand : ICommand
{
    public object Settings { get; }

    private UpdateConfigurationCommand(object settings) => Settings = settings;

    public static UpdateConfigurationCommand Create<T>(T settings) where T : IBlogSettings<T>
    {
        ArgumentNullException.ThrowIfNull(settings);
        return new(settings);
    }
}

public class UpdateConfigurationCommandHandler(
    BlogDbContext db,
    BlogConfig blogConfig,
    IEventMediator eventMediator,
    ConfigurationWriteGate gate,
    ILogger<UpdateConfigurationCommandHandler> logger) : ICommandHandler<UpdateConfigurationCommand>
{
    public async Task HandleAsync(UpdateConfigurationCommand request, CancellationToken ct)
    {
        var settings = request.Settings;
        var name = settings.GetType().Name;
        await gate.WaitAsync(ct);
        try
        {
            var property = typeof(BlogSettingsSnapshot).GetProperty(name)
                ?? throw new InvalidOperationException($"Unknown blog setting '{name}'.");
            Validator.ValidateObject(settings, new ValidationContext(settings), true);

            var candidate = blogConfig.Capture() with { };
            property.SetValue(candidate, settings);

            var entity = await db.BlogConfiguration.SingleOrDefaultAsync(p => p.CfgKey == name, ct)
                ?? throw new InvalidOperationException($"Configuration '{name}' was not found.");

            var json = settings.ToJson();
            var changed = entity.CfgValue != json;
            if (changed)
            {
                entity.CfgValue = json;
                entity.LastModifiedTimeUtc = DateTime.UtcNow;
            }

            await db.SaveChangesAsync(ct);

            blogConfig.Publish(candidate);

            // Commit and runtime publication must precede invalidation, even if the client disconnects.
            if (changed)
                await eventMediator.PublishAsync(new ConfigurationChangedEvent([name]), CancellationToken.None);

            logger.LogInformation("Configuration updated successfully: {Name}", name);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update configuration {Name}", name);
            throw;
        }
        finally
        {
            gate.Release();
        }
    }
}
