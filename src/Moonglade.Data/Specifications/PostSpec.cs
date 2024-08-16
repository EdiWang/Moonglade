using Moonglade.Data.Entities;

namespace Moonglade.Data.Specifications;

public sealed class PostSpec : Specification<PostEntity>
{
    public PostSpec(Guid id, bool includeRelatedData = true)
    {
        Query.Where(p => p.Id == id);

        if (includeRelatedData)
        {
            Query.Include(p => p.Tags)
                 .Include(p => p.PostCategory)
                 .ThenInclude(pc => pc.Category);
        }
    }
}

public sealed class PostByIdForTitleDateSpec : SingleResultSpecification<PostEntity, (string Title, DateTime? PubDateUtc)>
{
    public PostByIdForTitleDateSpec(Guid id)
    {
        Query.Where(p => p.Id == id);
        Query.Select(p => new ValueTuple<string, DateTime?>(p.Title, p.PubDateUtc));
    }
}

public sealed class PostByStatusSpec : Specification<PostEntity>
{
    public PostByStatusSpec(PostStatus status)
    {
        switch (status)
        {
            case PostStatus.Draft:
                Query.Where(p => !p.IsPublished && !p.IsDeleted);
                break;
            case PostStatus.Published:
                Query.Where(p => p.IsPublished && !p.IsDeleted);
                break;
            case PostStatus.Deleted:
                Query.Where(p => p.IsDeleted);
                break;
            case PostStatus.Default:
                Query.Where(p => true);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(status), status, null);
        }

        Query.AsNoTracking();
    }
}

public sealed class FeaturedPostSpec : Specification<PostEntity>
{
    public FeaturedPostSpec()
    {
        Query.Where(p => p.IsFeatured && p.IsPublished && !p.IsDeleted);
    }
}

public sealed class PostByCatSpec : Specification<PostEntity>
{
    public PostByCatSpec(Guid? categoryId, int? top = null)
    {
        Query.Where(p =>
                    !p.IsDeleted &&
                    p.IsPublished &&
                    p.IsFeedIncluded &&
                    (categoryId == null || p.PostCategory.Any(c => c.CategoryId == categoryId.Value)));

        Query.OrderByDescending(p => p.PubDateUtc);

        if (top.HasValue)
        {
            Query.Skip(0).Take(top.Value);
        }
    }
}

public sealed class PostByYearMonthSpec : Specification<PostEntity>
{
    public PostByYearMonthSpec(int year, int month = 0)
    {
        Query.Where(p => p.PubDateUtc.Value.Year == year &&
                         (month == 0 || p.PubDateUtc.Value.Month == month));

        // Fix #313: Filter out unpublished posts
        Query.Where(p => p.IsPublished && !p.IsDeleted);

        Query.OrderByDescending(p => p.PubDateUtc);
        Query.AsNoTracking();
    }
}

public sealed class PostByDeletionFlagSpec : Specification<PostEntity>
{
    public PostByDeletionFlagSpec(bool isDeleted) => Query.Where(p => p.IsDeleted == isDeleted);
}

public sealed class PostByChecksumSpec : SingleResultSpecification<PostEntity>
{
    public PostByChecksumSpec(int hashCheckSum)
    {
        Query.Where(p => p.HashCheckSum == hashCheckSum && p.IsPublished && !p.IsDeleted);

        Query.Include(p => p.Comments)
             .Include(pt => pt.Tags)
             .Include(p => p.PostCategory)
                .ThenInclude(pc => pc.Category)
             .AsSplitQuery();
    }
}

public sealed class PostByRouteLinkSpec : SingleResultSpecification<PostEntity>
{
    public PostByRouteLinkSpec(string routeLink)
    {
        Query.Where(p => p.RouteLink == routeLink && p.IsPublished && !p.IsDeleted);

        Query.Include(p => p.Comments)
             .Include(pt => pt.Tags)
             .Include(p => p.PostCategory)
                .ThenInclude(pc => pc.Category)
             .AsSplitQuery();
    }
}

public sealed class PostByDateAndSlugSpec : Specification<PostEntity>
{
    public PostByDateAndSlugSpec(DateTime date, string slug, bool includeRelationData)
    {
        Query.Where(p =>
                    p.Slug == slug &&
                    p.IsPublished &&
                    p.PubDateUtc.Value.Date == date &&
                    !p.IsDeleted);

        if (includeRelationData)
        {
            Query.Include(p => p.Comments)
                 .Include(pt => pt.Tags)
                 .Include(p => p.PostCategory)
                    .ThenInclude(pc => pc.Category);
        }
    }
}

public sealed class PostByDateAndSlugForIdTitleSpec : SingleResultSpecification<PostEntity, (Guid Id, string Title)>
{
    public PostByDateAndSlugForIdTitleSpec(DateTime date, string slug)
    {
        Query.Where(p =>
            p.Slug == slug &&
            p.IsPublished &&
            p.PubDateUtc.Value.Date == date &&
            !p.IsDeleted);

        Query.Select(p => new ValueTuple<Guid, string>(p.Id, p.Title));
    }
}