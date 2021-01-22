using System.Collections.Generic;
using Moonglade.Core;

namespace Moonglade.Web.Models
{
    public class PostInsightsViewModel
    {
        public IReadOnlyList<PostSegment> TopReadPosts { get; set; }

        public IReadOnlyList<PostSegment> TopCommentedPosts { get; set; }
    }
}
