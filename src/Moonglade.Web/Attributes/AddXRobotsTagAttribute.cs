using Microsoft.AspNetCore.Mvc.Filters;

namespace Moonglade.Web.Attributes;

public class AddXRobotsTagAttribute : ResultFilterAttribute
{
    private readonly string _content;

    public AddXRobotsTagAttribute(string content)
    {
        _content = content;
    }

    public override void OnResultExecuting(ResultExecutingContext context)
    {
        if (!context.HttpContext.Response.Headers.ContainsKey("X-Robots-Tag"))
        {
            context.HttpContext.Response.Headers.Add("X-Robots-Tag", _content);
        }

        base.OnResultExecuting(context);
    }
}