using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Data.Spec
{
    public sealed class PageSpec : BaseSpecification<PageEntity>
    {
        public PageSpec(int top) : base(p => p.IsPublished)
        {
            ApplyOrderByDescending(p => p.CreateTimeUtc);
            ApplyPaging(0, top);
        }
    }
}
