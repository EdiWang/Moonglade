using LiteBus.Queries.Abstractions;
using Moonglade.Configuration;

namespace Moonglade.Features;

public record ListSocialLinksQuery : IQuery<SocialLink[]>;

public class ListSocialLinksQueryHandler(IBlogConfig blogConfig) : IQueryHandler<ListSocialLinksQuery, SocialLink[]>
{
    public Task<SocialLink[]> HandleAsync(ListSocialLinksQuery request, CancellationToken ct)
    {
        var section = blogConfig.SocialLinkSettings;

        if (!section.IsEnabled)
        {
            return Task.FromResult(Array.Empty<SocialLink>());
        }

        var links = blogConfig.SocialLinkSettings.Links;
        return Task.FromResult(links);
    }
}