using System;

namespace Moonglade.Model
{
    public class CreateMenuRequest
    {
        public string Title { get; set; }

        public string Url { get; set; }

        public string Icon { get; set; }

        public int DisplayOrder { get; set; }

        public bool IsOpenInNewTab { get; set; }
    }

    public class EditMenuRequest : CreateMenuRequest
    {
        public Guid Id { get; }

        public EditMenuRequest(Guid id)
        {
            Id = id;
        }
    }
}
