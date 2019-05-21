using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Data.Spec
{
    public sealed class TagSpec : BaseSpecification<TagEntity>
    {
        public TagSpec(int top) : base(t => true)
        {
            ApplyPaging(0,top);
            ApplyOrderByDescending(p => p.PostTag.Count);
        }
    }
}
