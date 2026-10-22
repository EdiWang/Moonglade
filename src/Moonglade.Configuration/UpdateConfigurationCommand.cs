using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;
using Moonglade.Data;
using System.Collections.ObjectModel;
using System.Reflection;

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
    IBlogConfig blogConfig,
    ILogger<UpdateConfigurationCommandHandler> logger) : ICommandHandler<UpdateConfigurationCommand>
{
    // Keep concurrent commits and singleton replacements in the same order within this process.
    private static readonly SemaphoreSlim UpdateLock = new(1, 1);

    public async Task HandleAsync(UpdateConfigurationCommand request, CancellationToken ct)
    {
        var names = request.Settings.Keys.ToArray();
        await UpdateLock.WaitAsync(ct);
        try
        {
            var replacements = new List<(PropertyInfo Property, object Value)>();
            foreach (var (name, json) in request.Settings)
            {
                var property = typeof(IBlogConfig).GetProperty(name)
                    ?? throw new InvalidOperationException($"Unknown blog setting '{name}'.");
                var value = json.FromJson(property.PropertyType)
                    ?? throw new InvalidOperationException($"Blog setting '{name}' cannot be null.");
                replacements.Add((property, value));
            }

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
            foreach (var (name, json) in request.Settings)
            {
                var entity = entities[name];
                if (entity.CfgValue == json) continue;

                entity.CfgValue = json;
                entity.LastModifiedTimeUtc = utcNow;
            }

            await db.SaveChangesAsync(ct);

            foreach (var (property, value) in replacements)
            {
                property.SetValue(blogConfig, value);
            }

            logger.LogInformation("Configuration updated successfully: {Names}", string.Join(", ", names));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update configuration {Names}", string.Join(", ", names));
            throw;
        }
        finally
        {
            UpdateLock.Release();
        }
    }
}
