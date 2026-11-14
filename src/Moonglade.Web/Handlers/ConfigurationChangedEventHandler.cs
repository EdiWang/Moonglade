using LiteBus.Events.Abstractions;
using Moonglade.BackgroundServices;

namespace Moonglade.Web.Handlers;

public class ConfigurationChangedEventHandler(PostCacheInvalidator posts, ThemeCacheInvalidator themes, ScheduledPublishWakeUp scheduler)
    : IEventHandler<ConfigurationChangedEvent>
{
    public Task HandleAsync(ConfigurationChangedEvent request, CancellationToken ct)
    {
        if (request.Names.Any(name => name is nameof(GeneralSettings) or nameof(SEOSettings) or nameof(FeedSettings) or nameof(ImageSettings)))
            posts.InvalidateFeeds();
        if (request.Names.Any(name => name is nameof(GeneralSettings) or nameof(SEOSettings)))
            posts.InvalidateSiteMap();
        if (request.Names.Contains(nameof(AppearanceSettings))) themes.Invalidate();
        if (request.Names.Contains(nameof(AdvancedSettings))) scheduler.WakeUp();
        return Task.CompletedTask;
    }
}
