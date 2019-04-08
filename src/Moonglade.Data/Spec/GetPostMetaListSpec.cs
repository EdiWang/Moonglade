using System;
using System.Collections.Generic;
using System.Text;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Data.Spec
{
    public class GetPostMetaListSpec : BaseSpecification<Post>
    {
        public GetPostMetaListSpec(bool isDeleted, bool isPublished) :
            base(p => p.PostPublish.IsDeleted == isDeleted && p.PostPublish.IsPublished == isPublished)
        {

        }

        public GetPostMetaListSpec() :
            base(p => p.PostPublish.IsDeleted)
        {

        }
    }
}
