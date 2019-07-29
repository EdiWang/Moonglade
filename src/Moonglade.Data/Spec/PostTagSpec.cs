using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Data.Spec
{
    public sealed class PostTagSpec : BaseSpecification<PostTagEntity>
    {
        public PostTagSpec(int tagId) : base(pt => pt.TagId == tagId 
                                                   && !pt.Post.PostPublish.IsDeleted)
        {
            
        }
    }
}
