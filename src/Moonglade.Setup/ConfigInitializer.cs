using LiteBus.Commands.Abstractions;
using LiteBus.Queries.Abstractions;
using Microsoft.Extensions.Logging;
using Moonglade.Configuration;

namespace Moonglade.Setup;

/// <summary>
/// Initializes blog configuration settings by loading default values for missing configuration keys.
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
    IBlogConfig blogConfig,
    ILogger<ConfigInitializer> logger)
{
    public async Task Initialize(bool isNew, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Starting blog configuration initialization. IsNew: {IsNew}", isNew);

            var config = await queryMediator.QueryAsync(new ListConfigurationsQuery(), cancellationToken);
            var defaultsToAdd = blogConfig.LoadFromConfig(config, isNew);

            if (defaultsToAdd.Count == 0)
            {
                logger.LogInformation("No configuration keys to initialize");
                return;
            }

            logger.LogInformation(
                "Initializing {Count} configuration keys: {Keys}",
                defaultsToAdd.Count,
                string.Join(", ", defaultsToAdd.Keys));

            foreach (var (key, defaultJson) in defaultsToAdd)
            {
                await InitializeSettingAsync(key, defaultJson, cancellationToken);
            }

            logger.LogInformation("Blog configuration initialization completed successfully");
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
    /// <param name="defaultJson">The default settings already assigned to the runtime configuration.</param>
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
