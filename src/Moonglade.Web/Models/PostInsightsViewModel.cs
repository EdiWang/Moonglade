using Moonglade.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Moonglade.Web.Models
{
    public class PostInsightsViewModel
    {
        public IReadOnlyList<PostMetaData> TopReadPosts { get; set; }

        public IReadOnlyList<PostMetaData> TopCommentedPosts { get; set; }
    }
}
