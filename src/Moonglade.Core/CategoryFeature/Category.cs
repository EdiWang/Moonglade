using Moonglade.Data.Entities;
using System.Linq.Expressions;

namespace Moonglade.Core.CategoryFeature
{
    public class Category
    {
        public Guid Id { get; set; }
        public string RouteName { get; set; }
        public string DisplayName { get; set; }
        public string Note { get; set; }

        public static readonly Expression<Func<CategoryEntity, Category>> EntitySelector = c => new()
        {
            Id = c.Id,
            DisplayName = c.DisplayName,
            RouteName = c.RouteName,
            Note = c.Note
        };
    }
}
