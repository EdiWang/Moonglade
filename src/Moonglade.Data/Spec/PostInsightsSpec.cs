using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Data.Spec;

public sealed class PostInsightsSpec : BaseSpecification<PostEntity>
{
    public PostInsightsSpec(int top) :
        base(p => !p.IsDeleted
                  && p.IsPublished
                  && p.PubDateUtc >= DateTime.UtcNow.AddYears(-1))
    {
        AddCriteria(p => p.Comments.Any(c => c.IsApproved));
        ApplyOrderByDescending(p => p.Comments.Count);

        ApplyPaging(0, top);
    }
}