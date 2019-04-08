using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Data.Spec
{
    public sealed class TagSpec : BaseSpecification<Tag>
    {
        public TagSpec(int top) : base(t => true)
        {
            ApplyPaging(0,top);
            ApplyOrderByDescending(p => p.PostTag.Count);
        }
    }
}
