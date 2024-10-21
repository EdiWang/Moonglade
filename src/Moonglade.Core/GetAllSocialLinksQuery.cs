using Microsoft.Extensions.Configuration;
using Moonglade.Configuration;

namespace Moonglade.Core;

public record GetAllSocialLinksQuery : IRequest<List<SocialLink>>;

public class GetAllSocialLinksQueryHandler(IConfiguration configuration) : IRequestHandler<GetAllSocialLinksQuery, List<SocialLink>>
{
    public Task<List<SocialLink>> Handle(GetAllSocialLinksQuery request, CancellationToken ct)
    {
        var section = configuration.GetSection("Experimental:SocialLinks");

        if (!section.Exists())
        {
            return Task.FromResult(new List<SocialLink>());
        }

        var links = section.Get<List<SocialLink>>();
        return Task.FromResult(links ?? new List<SocialLink>());
    }
}