using Moonglade.Configuration;

namespace Moonglade.Core;

public record GetAllSocialLinksQuery : IRequest<List<SocialLink>>;

public class GetAllSocialLinksQueryHandler(IBlogConfig blogConfig) : IRequestHandler<GetAllSocialLinksQuery, List<SocialLink>>
{
    public Task<List<SocialLink>> Handle(GetAllSocialLinksQuery request, CancellationToken ct)
    {
        var section = blogConfig.SocialLinkSettings;

        if (!section.IsEnabled)
        {
            return Task.FromResult(new List<SocialLink>());
        }

        var links = blogConfig.SocialLinkSettings.Links;
        return Task.FromResult(links ?? new List<SocialLink>());
    }
}