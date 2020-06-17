using Moonglade.Model;
using System.Collections.Generic;

namespace Moonglade.Web.Models
{
    public class PostInsightsViewModel
    {
        public IReadOnlyList<PostSegment> TopReadPosts { get; set; }

        public IReadOnlyList<PostSegment> TopCommentedPosts { get; set; }
    }
}
