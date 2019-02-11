using System;
using System.Collections.Generic;
using System.Text;

namespace Moonglade.Data.Entities
{
    public class BlogConfiguration
    {
        public int Id { get; set; }

        public string CfgKey { get; set; }

        public string CfgValue { get; set; }

        public DateTime? LastModifiedTimeUtc { get; set; }
    }
}
