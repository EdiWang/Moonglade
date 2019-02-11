using System;

namespace Moonglade.Model
{
    public class SearchResult
    {
        public string Title { get; set; }
        public DateTime PubDateUtc { get; set; }
        public string Summary { get; set; }
        public string Slug { get; set; }
    }
}
