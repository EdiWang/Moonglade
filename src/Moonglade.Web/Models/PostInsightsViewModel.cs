using Moonglade.Model;
using System.Collections.Generic;

namespace Moonglade.Web.Models
{
    public class PostInsightsViewModel
    {
        public IReadOnlyList<PostMetaData> TopReadPosts { get; set; }

        public IReadOnlyList<PostMetaData> TopCommentedPosts { get; set; }
    }
}
