using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Data.Spec
{
    public sealed class GetPostSpec : BaseSpecification<Post>
    {
        public GetPostSpec(DateTime date, string slug)
            : base(p => p.Slug == slug &&
             p.PostPublish.IsPublished &&
             p.PostPublish.PubDateUtc.Value.Date == date &&
             !p.PostPublish.IsDeleted)
        {
            AddInclude(post => post.Include(p => p.PostPublish)
                .Include(p => p.PostExtension)
                .Include(p => p.Comment)
                .Include(p => p.PostTag).ThenInclude(pt => pt.Tag)
                .Include(p => p.PostCategory).ThenInclude(pc => pc.Category));
        }
    }
}
