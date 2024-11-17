using MediatR;
using Moonglade.Configuration;

namespace Moonglade.Setup;

public interface IBlogConfigInitializer
{
    Task Initialize(bool isNew);
}

public class BlogConfigInitializer(IMediator mediator, IBlogConfig blogConfig) : IBlogConfigInitializer
{
    public async Task Initialize(bool isNew)
    {
        // Load configurations into singleton
        var config = await mediator.Send(new GetAllConfigurationsQuery());
        var keysToAdd = blogConfig.LoadFromConfig(config);

        var toAdd = keysToAdd as int[] ?? keysToAdd.ToArray();
        if (toAdd.Length == 0) return;

        var settingsMap = new Dictionary<int, (string Name, Func<string> DefaultValue)>
        {
            { 1, (nameof(ContentSettings), () => ContentSettings.DefaultValue.ToJson()) },
            { 2, (nameof(NotificationSettings), () => NotificationSettings.DefaultValue.ToJson()) },
            { 3, (nameof(FeedSettings), () => FeedSettings.DefaultValue.ToJson()) },
            { 4, (nameof(GeneralSettings), () => GeneralSettings.DefaultValue.ToJson()) },
            { 5, (nameof(ImageSettings), () => ImageSettings.DefaultValue.ToJson()) },
            { 6, (nameof(AdvancedSettings), () => AdvancedSettings.DefaultValue.ToJson()) },
            { 7, (nameof(CustomStyleSheetSettings), () => CustomStyleSheetSettings.DefaultValue.ToJson()) },
            { 10, (nameof(CustomMenuSettings), () => CustomMenuSettings.DefaultValue.ToJson()) },
            { 11, (nameof(LocalAccountSettings), () => LocalAccountSettings.DefaultValue.ToJson()) },
            { 12, (nameof(SocialLinkSettings), () => SocialLinkSettings.DefaultValue.ToJson()) },
            { 99, (nameof(SystemManifestSettings), () => isNew ? SystemManifestSettings.DefaultValueNew.ToJson() : SystemManifestSettings.DefaultValue.ToJson()) }
        };

        foreach (var key in toAdd)
        {
            if (settingsMap.TryGetValue(key, out var setting))
            {
                await mediator.Send(new AddDefaultConfigurationCommand(key, setting.Name, setting.DefaultValue()));
            }
        }
    }

}