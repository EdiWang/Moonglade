using Edi.CacheAside.InMemory;
using Microsoft.Extensions.Configuration;
using Moonglade.Data;
using Moonglade.Data.Specifications;
using Moonglade.Utils;

namespace Moonglade.Core.PostFeature;

public record GetPostBySlugQuery(PostSlug Slug) : IRequest<PostEntity>;

public class GetPostBySlugQueryHandler(MoongladeRepository<PostEntity> repo, ICacheAside cache, IConfiguration configuration)
    : IRequestHandler<GetPostBySlugQuery, PostEntity>
{
    public async Task<PostEntity> Handle(GetPostBySlugQuery request, CancellationToken ct)
    {
        var date = new DateTime(request.Slug.Year, request.Slug.Month, request.Slug.Day);

        // Try to find by checksum
        var slugCheckSum = Helper.ComputeCheckSum($"{request.Slug.Slug}#{date:yyyyMMdd}");
        var spec = new PostByChecksumSpec(slugCheckSum);

        var psm = await cache.GetOrCreateAsync(BlogCachePartition.Post.ToString(), $"{slugCheckSum}", async entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromMinutes(int.Parse(configuration["CacheSlidingExpirationMinutes:Post"]!));

            var post = await repo.FirstOrDefaultAsync(spec, ct);
            return post;
        });

        return psm;
    }
}