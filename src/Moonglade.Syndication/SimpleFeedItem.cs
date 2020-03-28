using System;
using System.Collections.Generic;

namespace Edi.SyndicationFeedGenerator
{
    public class SimpleFeedItem
    {
        public string Id { get; set; }
        public DateTime PubDateUtc { get; set; }
        public string Title { get; set; }
        public string Link { get; set; }
        public string Description { get; set; }
        public string Author { get; set; }
        public string AuthorEmail { get; set; }
        public IList<string> Categories { get; set; }
    }
}
