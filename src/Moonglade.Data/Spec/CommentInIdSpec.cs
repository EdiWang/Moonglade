using System;
using System.Linq;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Data.Spec
{
    public class CommentInIdSpec : BaseSpecification<Comment>
    {
        public CommentInIdSpec(Guid[] ids) : base(c => ids.Contains(c.Id))
        {

        }
    }
}
