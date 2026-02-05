using Moonglade.Data.DTO;
using Moonglade.Data.Entities;

namespace Moonglade.Data.Specifications;

public sealed class PostByStatusSpec : Specification<PostEntity>
{
    public PostByStatusSpec(PostStatus status)
    {
        switch (status)
        {
            case PostStatus.Draft:
                Query.Where(p => p.PostStatus == PostStatus.Draft && !p.IsDeleted);
                break;
            case PostStatus.Scheduled:
                Query.Where(p => p.PostStatus == PostStatus.Scheduled && !p.IsDeleted);
                break;
            case PostStatus.Published:
                Query.Where(p => p.PostStatus == PostStatus.Published && !p.IsDeleted);
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
