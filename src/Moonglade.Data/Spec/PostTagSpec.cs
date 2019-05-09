using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Data.Spec
{
    public sealed class PostTagSpec : BaseSpecification<PostTag>
    {
        public PostTagSpec(int tagId) : base(pt => pt.TagId == tagId)
        {
            
        }
    }
}
