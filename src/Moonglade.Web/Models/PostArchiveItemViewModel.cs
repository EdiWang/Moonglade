using System;

namespace Moonglade.Web.Models
{
    public class PostArchiveItemViewModel
    {
        public string Title { get; set; }

        public string Slug { get; set; }

        public DateTime PubDateUtc { get; set; }
    }
}
