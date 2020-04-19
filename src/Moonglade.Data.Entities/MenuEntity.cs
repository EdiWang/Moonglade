using System;
using System.Collections.Generic;
using System.Text;

namespace Moonglade.Data.Entities
{
    public class MenuEntity
    {
        public Guid Id { get; set; }

        public string Title { get; set; }

        public string Url { get; set; }

        public string Icon { get; set; }

        public int DisplayOrder { get; set; }

        public bool IsOpenInNewTab { get; set; }
    }
}
