using System;
using System.Collections.Generic;

namespace Moonglade.Model
{
    public class PostListEntry
    {
        public DateTime PubDateUtc { get; set; }

        public string Title { get; set; }

        public string Slug { get; set; }

        public string ContentAbstract { get; set; }

        public string LangCode { get; set; }

        public IEnumerable<Tag> Tags { get; set; }
    }
}
