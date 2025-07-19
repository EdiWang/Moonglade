using LiteBus.Queries.Abstractions;
using Moonglade.Configuration;

namespace Moonglade.Core;

public record GetAllSocialLinksQuery : IQuery<SocialLink[]>;

public class GetAllSocialLinksQueryHandler(IBlogConfig blogConfig) : IQueryHandler<GetAllSocialLinksQuery, SocialLink[]>
{
    public Task<SocialLink[]> HandleAsync(GetAllSocialLinksQuery request, CancellationToken ct)
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