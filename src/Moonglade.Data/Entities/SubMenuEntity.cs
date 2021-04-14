using System;

namespace Moonglade.Data.Entities
{
    public class SubMenuEntity
    {
        public Guid Id { get; set; }

        public string Title { get; set; }

        public string Url { get; set; }

        public bool IsOpenInNewTab { get; set; }

        public Guid MenuId { get; set; }

        public virtual MenuEntity Menu { get; set; }
    }
}