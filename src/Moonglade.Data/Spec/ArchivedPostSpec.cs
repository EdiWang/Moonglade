using System;
using System.Collections.Generic;
using System.Text;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Data.Spec
{
    public sealed class ArchivedPostSpec : BaseSpecification<Post>
    {
        public ArchivedPostSpec(int year, int month = 0) :
            base(p => p.PostPublish.PubDateUtc.Value.Year == year &&
                (month == 0 || p.PostPublish.PubDateUtc.Value.Month == month))
        {
            AddInclude(p => p.PostPublish);
            ApplyOrderByDescending(p => p.PostPublish.PubDateUtc);
        }
    }
}
