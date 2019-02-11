using System;

namespace Moonglade.Web.Models
{
    public class PostGridModel
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public DateTime? PubDateUtc { get; set; }
        public DateTime CreateOnUtc { get; set; }
        public int? Revision { get; set; }
        public bool IsPublished { get; set; }
        public int Hits { get; set; }
        public bool IsDeleted { get; set; }
    }
}
