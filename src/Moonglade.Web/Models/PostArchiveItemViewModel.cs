using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Moonglade.Web.Models
{
    public class PostArchiveItemViewModel
    {
        public string Title { get; set; }

        public string Slug { get; set; }

        public DateTime PubDateUtc { get; set; }
    }
}
