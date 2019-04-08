using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Data.Spec
{
    public sealed class PostTagSpec : BaseSpecification<PostTag>
    {
        public PostTagSpec(Expression<Func<PostTag, bool>> criteria) : base(criteria)
        {

        }

        public PostTagSpec(int tagId) : base(pt => pt.TagId == tagId)
        {
            
        }
    }
}
