using Ardalis.Specification.EntityFrameworkCore;
using LiteBus.Queries.Abstractions;
using Microsoft.EntityFrameworkCore;
using Moonglade.Data.DTO;
using Moonglade.Data.Specifications;

namespace Moonglade.Features.Post;

public record GetArchiveQuery : IQuery<List<Archive>>;

public class GetArchiveQueryHandler(BlogDbContext dbContext) : IQueryHandler<GetArchiveQuery, List<Archive>>
{
    public async Task<List<Archive>> HandleAsync(GetArchiveQuery request, CancellationToken ct)
    {
        var spec = new PostByStatusSpec(PostStatus.Published);

        if (!await dbContext.Post
            .AsNoTracking()
            .WithSpecification(spec)
            .AnyAsync(ct))
        {
            return [];
        }

        var list = await dbContext.Post
            .AsNoTracking()
            .WithSpecification(spec)
            .GroupBy(post => new { post.PubDateUtc!.Value.Year, post.PubDateUtc.Value.Month })
            .Select(p => new Archive(p.Key.Year, p.Key.Month, p.Count()))
            .ToListAsync(ct);

        return list;
    }
}