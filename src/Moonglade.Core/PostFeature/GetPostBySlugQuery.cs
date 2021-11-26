using MediatR;
using Microsoft.Extensions.Options;
using Moonglade.Caching;
using Moonglade.Configuration;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;
using Moonglade.Utils;

namespace Moonglade.Core.PostFeature;

public class GetPostBySlugQuery : IRequest<Post>
{
    public GetPostBySlugQuery(PostSlug slug)
    {
        Slug = slug;
    }

    public PostSlug Slug { get; set; }
}

public class GetPostBySlugQueryHandler : IRequestHandler<GetPostBySlugQuery, Post>
{
    private readonly IRepository<PostEntity> _postRepo;
    private readonly IBlogCache _cache;
    private readonly AppSettings _settings;

    public GetPostBySlugQueryHandler(IRepository<PostEntity> postRepo, IBlogCache cache, IOptions<AppSettings> settings)
    {
        _postRepo = postRepo;
        _cache = cache;
        _settings = settings.Value;
    }

    public async Task<Post> Handle(GetPostBySlugQuery request, CancellationToken cancellationToken)
    {
        var date = new DateTime(request.Slug.Year, request.Slug.Month, request.Slug.Day);

        // Try to find by checksum
        var slugCheckSum = Helper.ComputeCheckSum($"{request.Slug.Slug}#{date:yyyyMMdd}");
        ISpecification<PostEntity> spec = new PostSpec(slugCheckSum);

        var pid = await _postRepo.SelectFirstOrDefaultAsync(spec, p => p.Id);
        if (pid == Guid.Empty)
        {
            // Post does not have a checksum, fall back to old method
            spec = new PostSpec(date, request.Slug.Slug);
            pid = await _postRepo.SelectFirstOrDefaultAsync(spec, x => x.Id);

            if (pid == Guid.Empty) return null;

            // Post is found, fill it's checksum so that next time the query can be run against checksum
            var p = await _postRepo.GetAsync(pid);
            p.HashCheckSum = slugCheckSum;

            await _postRepo.UpdateAsync(p);
        }

        var psm = await _cache.GetOrCreateAsync(CacheDivision.Post, $"{pid}", async entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromMinutes(_settings.CacheSlidingExpirationMinutes["Post"]);

            var post = await _postRepo.SelectFirstOrDefaultAsync(spec, Post.EntitySelector);
            return post;
        });

        return psm;
    }
}