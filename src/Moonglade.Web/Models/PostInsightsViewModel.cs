using System.Collections.Generic;
using Moonglade.Model;

namespace Moonglade.Web.Models
{
    public class PostInsightsViewModel
    {
        public IReadOnlyList<PostSegment> TopReadPosts { get; set; }

        public IReadOnlyList<PostSegment> TopCommentedPosts { get; set; }
    }
}
