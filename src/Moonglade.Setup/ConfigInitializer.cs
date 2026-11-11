using LiteBus.Commands.Abstractions;
using LiteBus.Queries.Abstractions;
using Microsoft.Extensions.Logging;
using Moonglade.Configuration;

namespace Moonglade.Setup;

/// <summary>
/// Initializes blog configuration settings by loading default values for missing or invalid configuration keys.
/// This class ensures that all required configuration settings have appropriate default values
/// when the blog is first set up or when new settings are introduced.
/// </summary>
/// <param name="queryMediator">The query mediator for retrieving configuration data.</param>
/// <param name="commandMediator">The command mediator for executing configuration commands.</param>
/// <param name="blogConfig">The blog configuration service.</param>
/// <param name="logger">The logger for tracking initialization operations.</param>
public class ConfigInitializer(
    IQueryMediator queryMediator,
    ICommandMediator commandMediator,
    BlogConfig blogConfig,
    ILogger<ConfigInitializer> logger)
{
    public async Task Initialize(bool isNew, CancellationToken cancellationToken = default)
    {
        var (_, snapshot) = await InitializeStoreAsync(isNew, cancellationToken);
        blogConfig.Publish(snapshot);
    }

    internal async Task<(IDictionary<string, string> Values, BlogSettingsSnapshot Snapshot)> InitializeStoreAsync(
        bool isNew, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Starting blog configuration initialization. IsNew: {IsNew}", isNew);

            var config = await queryMediator.QueryAsync(new ListConfigurationsQuery(), cancellationToken);
            var manifestKey = nameof(SystemManifestSettings);
            if (!isNew && !config.ContainsKey(manifestKey)
                && !typeof(BlogSettingsSnapshot).GetProperties().Any(property => config.ContainsKey(property.Name)))
            {
                throw new InvalidOperationException(
                    "Stored installation configuration is missing. Restore valid settings before restarting.");
            }

            var manifest = config.TryGetValue(manifestKey, out var manifestJson)
                ? SystemManifestSettings.ParseStored(manifestJson)
                : isNew ? SystemManifestSettings.DefaultValueNew : SystemManifestSettings.DefaultValue;

            // Validate detached settings before any repair writes or runtime publication.
            var (snapshot, defaultsToAdd) = BlogConfig.Parse(config);
            if (!config.ContainsKey(manifestKey)) defaultsToAdd.Add(manifestKey, manifest.ToJson());
            if (!isNew && !config.ContainsKey(manifestKey))
            {
                logger.LogWarning(
                    "Reconstructing missing configuration {Key}; version is unknown and InstallTimeUtc is the reconstruction time",
                    nameof(SystemManifestSettings));
            }

            if (defaultsToAdd.Count == 0)
            {
                logger.LogInformation("No configuration keys to initialize");
                return (config, snapshot);
            }

            logger.LogInformation(
                "Initializing {Count} configuration keys: {Keys}",
                defaultsToAdd.Count,
                string.Join(", ", defaultsToAdd.Keys));

            foreach (var (key, defaultJson) in defaultsToAdd)
            {
                await commandMediator.SendAsync(new AddDefaultConfigurationCommand(key, defaultJson), cancellationToken);
                config[key] = defaultJson;
            }

            logger.LogInformation("Blog configuration initialization completed successfully");
            return (config, snapshot);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initialize blog configuration");
            throw;
        }
    }
}
