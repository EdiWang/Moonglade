using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Data.Spec;

public class StyleSheetByFriendlyNameSpec : BaseSpecification<StyleSheetEntity>
{
    public StyleSheetByFriendlyNameSpec(string friendlyName) : base(p => p.FriendlyName == friendlyName)
    {
    }
}