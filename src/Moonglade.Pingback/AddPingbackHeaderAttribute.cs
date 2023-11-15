using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Moonglade.Pingback;

public class AddPingbackHeaderAttribute(string pingbackEndpoint) : ResultFilterAttribute
{
    public override void OnResultExecuting(ResultExecutingContext context)
    {
        if (!context.HttpContext.Response.Headers.ContainsKey("x-pingback"))
        {
            context.HttpContext.Response.Headers.Append("x-pingback",
                new[]
                {
                    $"{context.HttpContext.Request.Scheme}://{context.HttpContext.Request.Host}/{pingbackEndpoint}"
                });
        }

        base.OnResultExecuting(context);
    }
}