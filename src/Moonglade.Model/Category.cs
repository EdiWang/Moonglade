using System;

namespace Moonglade.Model
{
    public class Category
    {
        public Guid Id { get; set; }
        public string RouteName { get; set; }
        public string DisplayName { get; set; }
        public string Note { get; set; }
    }
}
