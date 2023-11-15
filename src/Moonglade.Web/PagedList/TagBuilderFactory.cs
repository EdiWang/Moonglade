using Microsoft.AspNetCore.Mvc.Rendering;

namespace Moonglade.Web.PagedList;

public sealed class TagBuilderFactory
{
    public TagBuilder Create(string tagName) => new(tagName);
}
