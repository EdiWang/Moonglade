using LiteBus.Commands.Abstractions;
using LiteBus.Queries.Abstractions;
using Microsoft.Extensions.Logging;
using Moonglade.Configuration;

namespace Moonglade.Setup;

/// <summary>
/// Defines the contract for initializing blog configuration settings.
/// </summary>
public interface IConfigInitializer
{
    /// <summary>
    /// Loads persisted blog configuration and writes defaults for missing settings.
    /// </summary>
    /// <param name="isNew">Indicates whether this is a new blog installation.</param>
    /// <returns>A task representing the asynchronous initialization operation.</returns>
    Task Initialize(bool isNew, CancellationToken cancellationToken = default);
}

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
    ILogger<ConfigInitializer> logger) : IConfigInitializer
{
    /// <summary>
    /// Dictionary that maps configuration setting names to their corresponding default value providers.
    /// Each provider function takes a boolean parameter indicating if this is a new installation
    /// and returns the JSON representation of the default settings.
    /// </summary>
    private static readonly Dictionary<string, Func<bool, string>> SettingProviders = new()
    {
        { nameof(ContentSettings), _ => ContentSettings.DefaultValue.ToJson() },
        { nameof(NotificationSettings), _ => NotificationSettings.DefaultValue.ToJson() },
        { nameof(FeedSettings), _ => FeedSettings.DefaultValue.ToJson() },
        { nameof(GeneralSettings), _ => GeneralSettings.DefaultValue.ToJson() },
        { nameof(ImageSettings), _ => ImageSettings.DefaultValue.ToJson() },
        { nameof(AdvancedSettings), _ => AdvancedSettings.DefaultValue.ToJson() },
        { nameof(AppearanceSettings), _ => AppearanceSettings.DefaultValue.ToJson() },
        { nameof(CommentSettings), _ => CommentSettings.DefaultValue.ToJson() },
        { nameof(CustomMenuSettings), _ => CustomMenuSettings.DefaultValue.ToJson() },
        { nameof(LocalAccountSettings), _ => LocalAccountSettings.DefaultValue.ToJson() },
        { nameof(SystemManifestSettings), isNew => isNew ? SystemManifestSettings.DefaultValueNew.ToJson() : SystemManifestSettings.DefaultValue.ToJson() }
    };

    public async Task Initialize(bool isNew, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Starting blog configuration initialization. IsNew: {IsNew}", isNew);

            var config = await queryMediator.QueryAsync(new ListConfigurationsQuery(), cancellationToken);
            var keysToAdd = blogConfig.LoadFromConfig(config)?.Distinct().ToArray() ?? [];

            if (keysToAdd.Length == 0)
            {
                logger.LogInformation("No configuration keys to initialize");
                return;
            }

            logger.LogInformation(
                "Initializing {Count} configuration keys: {Keys}",
                keysToAdd.Length,
                string.Join(", ", keysToAdd));

            foreach (var key in keysToAdd)
            {
                await InitializeSettingAsync(key, isNew, cancellationToken);
            }

            var missingProviders = keysToAdd.Except(SettingProviders.Keys).ToArray();
            if (missingProviders.Length > 0)
            {
                logger.LogWarning("No setting providers found for keys: {MissingKeys}", string.Join(", ", missingProviders));
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
    /// <param name="isNew">
    /// Indicates whether this is a new blog installation, which may affect
    /// the default values for certain settings.
    /// </param>
    /// <returns>A task representing the asynchronous setting initialization operation.</returns>
    /// <exception cref="Exception">
    /// Thrown when the setting initialization fails due to command execution issues.
    /// </exception>
    private async Task InitializeSettingAsync(string key, bool isNew, CancellationToken cancellationToken)
    {
        if (!SettingProviders.TryGetValue(key, out var settingProvider))
        {
            logger.LogWarning("No setting provider found for key: {Key}", key);
            return;
        }

        try
        {
            var settingJson = settingProvider(isNew);
            await commandMediator.SendAsync(new AddDefaultConfigurationCommand(key, settingJson), cancellationToken);
            logger.LogDebug("Successfully initialized setting: {Key}", key);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initialize setting: {Key}", key);
            throw;
        }
    }
}
