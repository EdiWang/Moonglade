using LiteBus.Commands.Abstractions;
using LiteBus.Queries.Abstractions;
using Microsoft.Extensions.Logging;
using Moonglade.Configuration;
using Moonglade.Auth;

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
    ILogger<ConfigInitializer> logger,
    LocalAccountState accounts)
{
    public async Task Initialize(bool isNew, CancellationToken cancellationToken = default)
    {
        var config = await InitializeStoreAsync(isNew, cancellationToken);
        var account = LocalAccountSettings.ParseStored(config[nameof(LocalAccountSettings)]);
        blogConfig.LoadFromConfig(config, isNew);
        accounts.Publish(account);
    }

    internal async Task<IDictionary<string, string>> InitializeStoreAsync(bool isNew, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Starting blog configuration initialization. IsNew: {IsNew}", isNew);

            var config = await queryMediator.QueryAsync(new ListConfigurationsQuery(), cancellationToken);
            var accountKey = nameof(LocalAccountSettings);
            var manifestKey = nameof(SystemManifestSettings);
            if (!isNew && !config.ContainsKey(accountKey))
            {
                throw new InvalidOperationException(
                    "Stored configuration 'LocalAccountSettings' is missing. Restore a valid value before restarting.");
            }

            var account = config.TryGetValue(accountKey, out var accountJson)
                ? LocalAccountSettings.ParseStored(accountJson) : LocalAccountSettings.DefaultValue;
            var manifest = config.TryGetValue(manifestKey, out var manifestJson)
                ? SystemManifestSettings.ParseStored(manifestJson)
                : isNew ? SystemManifestSettings.DefaultValueNew : SystemManifestSettings.DefaultValue;

            // Validate detached settings before any repair writes or runtime publication.
            var defaultsToAdd = new BlogConfig().LoadFromConfig(config, isNew).ToDictionary();
            if (!config.ContainsKey(accountKey)) defaultsToAdd.Add(accountKey, account.ToJson());
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
                return config;
            }

            logger.LogInformation(
                "Initializing {Count} configuration keys: {Keys}",
                defaultsToAdd.Count,
                string.Join(", ", defaultsToAdd.Keys));

            foreach (var (key, defaultJson) in defaultsToAdd)
            {
                await InitializeSettingAsync(key, defaultJson, cancellationToken);
                config[key] = defaultJson;
            }

            logger.LogInformation("Blog configuration initialization completed successfully");
            return config;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initialize blog configuration");
            throw;
        }
    }

    /// <summary>
    /// Initializes a specific configuration setting with its default value.
    /// </summary>
    /// <param name="key">The configuration key to initialize.</param>
    /// <param name="defaultJson">The validated default settings to persist before runtime publication.</param>
    /// <returns>A task representing the asynchronous setting initialization operation.</returns>
    /// <exception cref="Exception">
    /// Thrown when the setting initialization fails due to command execution issues.
    /// </exception>
    private async Task InitializeSettingAsync(string key, string defaultJson, CancellationToken cancellationToken)
    {
        try
        {
            await commandMediator.SendAsync(new AddDefaultConfigurationCommand(key, defaultJson), cancellationToken);
            logger.LogDebug("Successfully initialized setting: {Key}", key);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initialize setting: {Key}", key);
            throw;
        }
    }
}
