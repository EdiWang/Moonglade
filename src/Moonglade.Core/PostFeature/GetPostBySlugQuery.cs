using Edi.CacheAside.InMemory;
using Microsoft.Extensions.Configuration;
using Moonglade.Data;
using Moonglade.Data.Specifications;

namespace Moonglade.Core.PostFeature;

public record GetPostBySlugQuery(int Year, int Month, int Day, string Slug) : IRequest<PostEntity>;

public class GetPostBySlugQueryHandler(MoongladeRepository<PostEntity> repo, ICacheAside cache, IConfiguration configuration)
    : IRequestHandler<GetPostBySlugQuery, PostEntity>
{
    public async Task<PostEntity> Handle(GetPostBySlugQuery request, CancellationToken ct)
    {
        var routeLink = $"{request.Year}/{request.Month}/{request.Day}/{request.Slug}".ToLower();
        var spec = new PostByRouteLinkSpec(routeLink);

        var psm = await cache.GetOrCreateAsync(BlogCachePartition.Post.ToString(), $"{routeLink}", async entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromMinutes(int.Parse(configuration["Post:CacheMinutes"]!));

            var post = await repo.FirstOrDefaultAsync(spec, ct);
            return post;
        });

        return psm;
    }
}