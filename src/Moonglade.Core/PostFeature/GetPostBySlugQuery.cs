using Edi.CacheAside.InMemory;
using Microsoft.Extensions.Configuration;
using Moonglade.Data;
using Moonglade.Data.Specifications;
using Moonglade.Utils;

namespace Moonglade.Core.PostFeature;

public record GetPostBySlugQuery(PostSlug Slug) : IRequest<Post>;

public class GetPostBySlugQueryHandler(MoongladeRepository<PostEntity> repo, ICacheAside cache, IConfiguration configuration)
    : IRequestHandler<GetPostBySlugQuery, Post>
{
    public async Task<Post> Handle(GetPostBySlugQuery request, CancellationToken ct)
    {
        var date = new DateTime(request.Slug.Year, request.Slug.Month, request.Slug.Day);

        // Try to find by checksum
        var slugCheckSum = Helper.ComputeCheckSum($"{request.Slug.Slug}#{date:yyyyMMdd}");
        var spec = new PostByChecksumSpec(slugCheckSum);

        var pid = await repo.FirstOrDefaultAsync(spec, p => p.Id);
        if (pid == Guid.Empty) return null;

        var psm = await cache.GetOrCreateAsync(BlogCachePartition.Post.ToString(), $"{pid}", async entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromMinutes(int.Parse(configuration["CacheSlidingExpirationMinutes:Post"]!));

            var post = await repo.FirstOrDefaultAsync(spec, Post.EntitySelector);
            return post;
        });

        return psm;
    }
}