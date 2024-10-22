using Moonglade.Configuration;

namespace Moonglade.Core;

public record GetAllSocialLinksQuery : IRequest<SocialLink[]>;

public class GetAllSocialLinksQueryHandler(IBlogConfig blogConfig) : IRequestHandler<GetAllSocialLinksQuery, SocialLink[]>
{
    public Task<SocialLink[]> Handle(GetAllSocialLinksQuery request, CancellationToken ct)
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