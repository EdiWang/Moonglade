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
        // load configurations into singleton
        var config = await mediator.Send(new GetAllConfigurationsQuery());
        var keysToAdd = blogConfig.LoadFromConfig(config);

        var toAdd = keysToAdd as int[] ?? keysToAdd.ToArray();
        if (toAdd.Length != 0)
        {
            foreach (var key in toAdd)
            {
                switch (key)
                {
                    case 1:
                        await mediator.Send(new AddDefaultConfigurationCommand(key, nameof(ContentSettings),
                            ContentSettings.DefaultValue.ToJson()));
                        break;
                    case 2:
                        await mediator.Send(new AddDefaultConfigurationCommand(key, nameof(NotificationSettings),
                            NotificationSettings.DefaultValue.ToJson()));
                        break;
                    case 3:
                        await mediator.Send(new AddDefaultConfigurationCommand(key, nameof(FeedSettings),
                            FeedSettings.DefaultValue.ToJson()));
                        break;
                    case 4:
                        await mediator.Send(new AddDefaultConfigurationCommand(key, nameof(GeneralSettings),
                            GeneralSettings.DefaultValue.ToJson()));
                        break;
                    case 5:
                        await mediator.Send(new AddDefaultConfigurationCommand(key, nameof(ImageSettings),
                            ImageSettings.DefaultValue.ToJson()));
                        break;
                    case 6:
                        await mediator.Send(new AddDefaultConfigurationCommand(key, nameof(AdvancedSettings),
                            AdvancedSettings.DefaultValue.ToJson()));
                        break;
                    case 7:
                        await mediator.Send(new AddDefaultConfigurationCommand(key, nameof(CustomStyleSheetSettings),
                            CustomStyleSheetSettings.DefaultValue.ToJson()));
                        break;
                    case 10:
                        await mediator.Send(new AddDefaultConfigurationCommand(key, nameof(CustomMenuSettings),
                            CustomMenuSettings.DefaultValue.ToJson()));
                        break;
                    case 11:
                        await mediator.Send(new AddDefaultConfigurationCommand(key, nameof(LocalAccountSettings),
                            LocalAccountSettings.DefaultValue.ToJson()));
                        break;
                    case 12:
                        await mediator.Send(new AddDefaultConfigurationCommand(key, nameof(SocialLinkSettings),
                            SocialLinkSettings.DefaultValue.ToJson()));
                        break;
                    case 99:
                        await mediator.Send(new AddDefaultConfigurationCommand(key, nameof(SystemManifestSettings),
                            isNew ?
                                SystemManifestSettings.DefaultValueNew.ToJson() :
                                SystemManifestSettings.DefaultValue.ToJson()));
                        break;
                }
            }
        }
    }
}