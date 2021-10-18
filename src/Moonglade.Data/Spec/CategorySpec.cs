using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Data.Spec;

public class CategorySpec : BaseSpecification<CategoryEntity>
{
    public CategorySpec(string routeName) : base(c => c.RouteName == routeName)
    {

    }

    public CategorySpec(Guid id) : base(c => c.Id == id)
    {

    }
}