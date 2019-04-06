using System;
using System.Collections.Generic;
using System.Text;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Data.Spec
{
    public class PostsByTagSpec : BaseSpecification<PostTag>
    {
        public PostsByTagSpec(string normalizedName) : 
            base(pt => pt.Tag.NormalizedName == normalizedName)
        {
        }
    }
}
