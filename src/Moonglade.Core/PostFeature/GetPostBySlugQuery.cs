using Microsoft.Extensions.Configuration;
using Moonglade.Caching;
using Moonglade.Data.Spec;
using Moonglade.Utils;

namespace Moonglade.Core.PostFeature;

public record GetPostBySlugQuery(PostSlug Slug) : IRequest<Post>;

public class GetPostBySlugQueryHandler : IRequestHandler<GetPostBySlugQuery, Post>
{
    private readonly IRepository<PostEntity> _repo;
    private readonly IBlogCache _cache;
    private readonly IConfiguration _configuration;

    public GetPostBySlugQueryHandler(IRepository<PostEntity> repo, IBlogCache cache, IConfiguration configuration)
    {
        _repo = repo;
        _cache = cache;
        _configuration = configuration;
    }

    public async Task<Post> Handle(GetPostBySlugQuery request, CancellationToken ct)
    {
        var date = new DateTime(request.Slug.Year, request.Slug.Month, request.Slug.Day);

        // Try to find by checksum
        var slugCheckSum = Helper.ComputeCheckSum($"{request.Slug.Slug}#{date:yyyyMMdd}");
        ISpecification<PostEntity> spec = new PostSpec(slugCheckSum);

        var pid = await _repo.SelectFirstOrDefaultAsync(spec, p => p.Id);
        if (pid == Guid.Empty)
        {
            // Post does not have a checksum, fall back to old method
            spec = new PostSpec(date, request.Slug.Slug);
            pid = await _repo.SelectFirstOrDefaultAsync(spec, x => x.Id);

            if (pid == Guid.Empty) return null;

            // Post is found, fill it's checksum so that next time the query can be run against checksum
            var p = await _repo.GetAsync(pid, ct);
            p.HashCheckSum = slugCheckSum;

            await _repo.UpdateAsync(p, ct);
        }

        var psm = await _cache.GetOrCreateAsync(CacheDivision.Post, $"{pid}", async entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromMinutes(int.Parse(_configuration["CacheSlidingExpirationMinutes:Post"]));

            var post = await _repo.SelectFirstOrDefaultAsync(spec, Post.EntitySelector);
            return post;
        });

        return psm;
    }
}