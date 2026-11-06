using LiteBus.Commands.Abstractions;
using LiteBus.Events.Abstractions;
using Microsoft.Extensions.Logging;
using Moonglade.Data;
using System.Collections.ObjectModel;
using System.Reflection;
using System.ComponentModel.DataAnnotations;

namespace Moonglade.Configuration;

public record UpdateConfigurationCommand : ICommand
{
    public IReadOnlyDictionary<string, string> Settings { get; }

    private UpdateConfigurationCommand(params KeyValuePair<string, string>[] settings) =>
        Settings = new ReadOnlyDictionary<string, string>(settings.ToDictionary());

    public static UpdateConfigurationCommand Create<T>(T settings) where T : IBlogSettings<T> =>
        new(Snapshot(settings));

    public static UpdateConfigurationCommand Create<TFirst, TSecond>(TFirst first, TSecond second)
        where TFirst : IBlogSettings<TFirst>
        where TSecond : IBlogSettings<TSecond> => new(Snapshot(first), Snapshot(second));

    private static KeyValuePair<string, string> Snapshot<T>(T settings) where T : IBlogSettings<T>
    {
        ArgumentNullException.ThrowIfNull(settings);
        return new(typeof(T).Name, settings.ToJson());
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
        var names = request.Settings.Keys.ToArray();
        await gate.WaitAsync(ct);
        try
        {
            var replacements = new List<(PropertyInfo Property, object Value)>();
            foreach (var (name, json) in request.Settings)
            {
                var property = typeof(BlogSettingsSnapshot).GetProperty(name)
                    ?? throw new InvalidOperationException($"Unknown blog setting '{name}'.");
                var value = json.FromJson(property.PropertyType)
                    ?? throw new InvalidOperationException($"Blog setting '{name}' cannot be null.");
                Validator.ValidateObject(value, new ValidationContext(value), true);
                replacements.Add((property, value));
            }

            var candidate = blogConfig.Capture() with { };
            foreach (var (property, value) in replacements) property.SetValue(candidate, value);

            var entities = await db.BlogConfiguration
                .Where(p => names.Contains(p.CfgKey))
                .ToDictionaryAsync(p => p.CfgKey, ct);
            foreach (var name in names)
            {
                if (!entities.ContainsKey(name))
                {
                    throw new InvalidOperationException($"Configuration '{name}' was not found.");
                }
            }

            var utcNow = DateTime.UtcNow;
            var changedNames = new List<string>();
            foreach (var (name, json) in request.Settings)
            {
                var entity = entities[name];
                if (entity.CfgValue == json) continue;

                entity.CfgValue = json;
                entity.LastModifiedTimeUtc = utcNow;
                changedNames.Add(name);
            }

            await db.SaveChangesAsync(ct);

            blogConfig.Publish(candidate);

            // Commit and runtime publication must precede invalidation, even if the client disconnects.
            if (changedNames.Count > 0)
                await eventMediator.PublishAsync(new ConfigurationChangedEvent(changedNames.AsReadOnly()), CancellationToken.None);

            logger.LogInformation("Configuration updated successfully: {Names}", string.Join(", ", names));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update configuration {Names}", string.Join(", ", names));
            throw;
        }
        finally
        {
            gate.Release();
        }
    }
}
