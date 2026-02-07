using Moonglade.Data.DTO;
using Moonglade.Data.Entities;

namespace Moonglade.Data.Specifications;

public class PostCategorySpec : Specification<PostCategoryEntity>
{
    public PostCategorySpec(Guid catId)
    {
        Query.Where(
            pc => pc.CategoryId == catId
            && pc.Post.PostStatus == PostStatus.Published
            && !pc.Post.IsDeleted);

        // Query.Include(pc => pc.Post);
    }
}
