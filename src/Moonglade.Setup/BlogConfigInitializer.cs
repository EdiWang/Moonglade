using LiteBus.Commands.Abstractions;
using LiteBus.Queries.Abstractions;
using Microsoft.Extensions.Logging;
using Moonglade.Configuration;

namespace Moonglade.Setup;

public interface IBlogConfigInitializer
{
    Task Initialize(bool isNew);
}

public class BlogConfigInitializer(
    IQueryMediator queryMediator,
    ICommandMediator commandMediator,
    IBlogConfig blogConfig,
    ILogger<BlogConfigInitializer> logger) : IBlogConfigInitializer
{
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
        { nameof(SocialLinkSettings), _ => SocialLinkSettings.DefaultValue.ToJson() },
        { nameof(SystemManifestSettings), isNew => isNew ? SystemManifestSettings.DefaultValueNew.ToJson() : SystemManifestSettings.DefaultValue.ToJson() }
    };

    public async Task Initialize(bool isNew)
    {
        try
        {
            logger.LogInformation("Starting blog configuration initialization. IsNew: {IsNew}", isNew);

            // Load configurations into singleton
            var config = await queryMediator.QueryAsync(new ListConfigurationsQuery());
            var keysToAdd = blogConfig.LoadFromConfig(config)?.ToArray() ?? [];

            if (keysToAdd.Length == 0)
            {
                logger.LogInformation("No configuration keys to initialize");
                return;
            }

            logger.LogInformation("Initializing {Count} configuration keys: {Keys}", keysToAdd.Length, string.Join(", ", keysToAdd));

            // Process settings one by one sequentially
            foreach (var key in keysToAdd)
            {
                await InitializeSettingAsync(key, isNew);
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

    private async Task InitializeSettingAsync(string key, bool isNew)
    {
        if (!SettingProviders.TryGetValue(key, out var settingProvider))
        {
            logger.LogWarning("No setting provider found for key: {Key}", key);
            return;
        }

        try
        {
            var settingJson = settingProvider(isNew);
            await commandMediator.SendAsync(new AddDefaultConfigurationCommand(key, settingJson));
            logger.LogDebug("Successfully initialized setting: {Key}", key);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initialize setting: {Key}", key);
            throw;
        }
    }
}