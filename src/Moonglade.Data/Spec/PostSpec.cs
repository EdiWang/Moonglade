using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Data.Spec;

public sealed class PostSpec : BaseSpecification<PostEntity>
{
    public PostSpec(Guid? categoryId, int? top = null) :
        base(p => !p.IsDeleted &&
                  p.IsPublished &&
                  p.IsFeedIncluded &&
                  (categoryId == null || p.PostCategory.Any(c => c.CategoryId == categoryId.Value)))
    {
        ApplyOrderByDescending(p => p.PubDateUtc);

        if (top.HasValue)
        {
            ApplyPaging(0, top.Value);
        }
    }

    public PostSpec(int year, int month = 0) :
        base(p => p.PubDateUtc.Value.Year == year &&
                  (month == 0 || p.PubDateUtc.Value.Month == month))
    {
        // Fix #313: Filter out unpublished posts
        AddCriteria(p => p.IsPublished && !p.IsDeleted);

        ApplyOrderByDescending(p => p.PubDateUtc);
    }

    public PostSpec(string slug, DateTime pubDateUtc) :
        base(p =>
        p.Slug == slug &&
        p.PubDateUtc != null
        && p.PubDateUtc.Value.Year == pubDateUtc.Year
        && p.PubDateUtc.Value.Month == pubDateUtc.Month
        && p.PubDateUtc.Value.Day == pubDateUtc.Day)
    {

    }

    public PostSpec(DateTime date, string slug)
        : base(p => p.Slug == slug &&
                    p.IsPublished &&
                    p.PubDateUtc.Value.Date == date &&
                    !p.IsDeleted)
    {
        AddInclude(post => post
            .Include(p => p.Comments)
            .Include(pt => pt.Tags)
            .Include(p => p.PostCategory).ThenInclude(pc => pc.Category));
    }

    public PostSpec(Guid id, bool includeRelatedData = true) : base(p => p.Id == id)
    {
        if (includeRelatedData)
        {
            AddInclude(post => post
                .Include(p => p.Tags)
                .Include(p => p.PostCategory)
                .ThenInclude(pc => pc.Category));
        }
    }

    public PostSpec(PostStatus status)
    {
        switch (status)
        {
            case PostStatus.Draft:
                AddCriteria(p => !p.IsPublished && !p.IsDeleted);
                break;
            case PostStatus.Published:
                AddCriteria(p => p.IsPublished && !p.IsDeleted);
                break;
            case PostStatus.Deleted:
                AddCriteria(p => p.IsDeleted);
                break;
            case PostStatus.Default:
                AddCriteria(p => true);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(status), status, null);
        }
    }
}

public class PostByDeletionFlagSpec : Specification<PostEntity>
{
    public PostByDeletionFlagSpec(bool isDeleted)
    {
        Query.Where(p => p.IsDeleted == isDeleted);
    }
}

public class PostByChecksumSpec : Specification<PostEntity>
{
    public PostByChecksumSpec(int hashCheckSum)
    {
        Query.Where(p => p.HashCheckSum == hashCheckSum && p.IsPublished && !p.IsDeleted);

        Query.Include(p => p.Comments)
             .Include(pt => pt.Tags)
             .Include(p => p.PostCategory).ThenInclude(pc => pc.Category);
    }
}

public class PostByDateAndSlugSpec : Specification<PostEntity>
{
    public PostByDateAndSlugSpec(DateTime date, string slug)
    {
        Query.Where(p => p.Slug == slug &&
                         p.IsPublished &&
                         p.PubDateUtc.Value.Date == date &&
                         !p.IsDeleted);

        Query.Include(p => p.Comments)
             .Include(pt => pt.Tags)
             .Include(p => p.PostCategory).ThenInclude(pc => pc.Category);
    }
}