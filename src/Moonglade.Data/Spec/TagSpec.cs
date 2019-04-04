using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Data.Spec
{
    public class TagSpec : BaseSpecification<Tag>
    {
        public TagSpec(Expression<Func<Tag, bool>> criteria) : base(criteria)
        {
        }
    }
}
