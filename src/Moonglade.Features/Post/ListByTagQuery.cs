using LiteBus.Queries.Abstractions;
using Moonglade.Data.DTO;
using Moonglade.Utils;

namespace Moonglade.Features.Post;

public record ListByTagQuery(int TagId, int PageSize, int PageIndex) : IQuery<List<PostDigest>>;

public class ListByTagQueryHandler(BlogDbContext db) : IQueryHandler<ListByTagQuery, List<PostDigest>>
{
    public Task<List<PostDigest>> HandleAsync(ListByTagQuery request, CancellationToken ct)
    {
        if (request.TagId <= 0) throw new ArgumentOutOfRangeException(nameof(request.TagId));
        Helper.ValidatePagingParameters(request.PageSize, request.PageIndex);

        var startRow = (request.PageIndex - 1) * request.PageSize;

        return db.PostTag
            .AsNoTracking()
            .Where(pt =>
                pt.TagId == request.TagId
                && !pt.Post.IsDeleted
                && pt.Post.PostStatus == PostStatus.Published)
            .OrderByDescending(pt => pt.Post.PubDateUtc)
            .Skip(startRow).Take(request.PageSize)
            .Select(pt => new PostDigest
            {
                Title = pt.Post.Title,
                Slug = pt.Post.Slug,
                ContentAbstract = pt.Post.ContentAbstract,
                PubDateUtc = pt.Post.PubDateUtc.GetValueOrDefault(),
                LangCode = pt.Post.ContentLanguageCode,
                IsFeatured = pt.Post.IsFeatured,
                Tags = pt.Post.Tags.Select(tag => new Moonglade.Data.DTO.Tag
                {
                    NormalizedName = tag.NormalizedName,
                    DisplayName = tag.DisplayName
                })
            })
            .ToListAsync(ct);
    }
}