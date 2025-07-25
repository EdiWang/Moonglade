﻿using LiteBus.Commands.Abstractions;
using LiteBus.Queries.Abstractions;
using Moonglade.Configuration;

namespace Moonglade.Setup;

public interface IBlogConfigInitializer
{
    Task Initialize(bool isNew);
}

public class BlogConfigInitializer(IQueryMediator queryMediator, ICommandMediator commandMediator, IBlogConfig blogConfig) : IBlogConfigInitializer
{
    public async Task Initialize(bool isNew)
    {
        // Load configurations into singleton
        var config = await queryMediator.QueryAsync(new GetAllConfigurationsQuery());
        var keysToAdd = blogConfig.LoadFromConfig(config)?.ToArray() ?? [];
        if (keysToAdd.Length == 0) return;

        var settingsMap = new Dictionary<string, Func<string>>
        {
            { nameof(ContentSettings), () => ContentSettings.DefaultValue.ToJson() },
            { nameof(NotificationSettings), () => NotificationSettings.DefaultValue.ToJson() },
            { nameof(FeedSettings), () => FeedSettings.DefaultValue.ToJson() },
            { nameof(GeneralSettings), () => GeneralSettings.DefaultValue.ToJson() },
            { nameof(ImageSettings), () => ImageSettings.DefaultValue.ToJson() },
            { nameof(AdvancedSettings), () => AdvancedSettings.DefaultValue.ToJson() },
            { nameof(AppearanceSettings), () => AppearanceSettings.DefaultValue.ToJson() },
            { nameof(CommentSettings), () => CommentSettings.DefaultValue.ToJson() },
            { nameof(CustomMenuSettings), () => CustomMenuSettings.DefaultValue.ToJson() },
            { nameof(LocalAccountSettings), () => LocalAccountSettings.DefaultValue.ToJson() },
            { nameof(SocialLinkSettings), () => SocialLinkSettings.DefaultValue.ToJson() },
            { nameof(SystemManifestSettings), () => isNew ? SystemManifestSettings.DefaultValueNew.ToJson() : SystemManifestSettings.DefaultValue.ToJson() }
        };

        foreach (var key in keysToAdd)
        {
            if (settingsMap.TryGetValue(key, out var setting))
            {
                await commandMediator.SendAsync(new AddDefaultConfigurationCommand(key, setting()));
            }
        }
    }
}