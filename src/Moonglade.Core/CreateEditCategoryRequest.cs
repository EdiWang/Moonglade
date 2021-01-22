using System;

namespace Moonglade.Core
{
    public class CreateCategoryRequest
    {
        public string RouteName { get; set; }
        public string DisplayName { get; set; }
        public string Note { get; set; }
    }

    public class EditCategoryRequest : CreateCategoryRequest
    {
        public Guid Id { get; }

        public EditCategoryRequest(Guid id)
        {
            Id = id;
        }
    }
}
