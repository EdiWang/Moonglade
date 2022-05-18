using Microsoft.AspNetCore.Mvc.Filters;
using UAParser;

namespace Moonglade.Web.Filters;

public class DisallowSpiderUA : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var userAgent = context.HttpContext.Request.Headers["User-Agent"];
        if (!string.IsNullOrWhiteSpace(userAgent) && IsMachineUA(userAgent))
        {
            context.Result = new ForbidResult();
        }

        base.OnActionExecuting(context);
    }

    private static bool IsMachineUA(string userAgent)
    {
        var uaParser = Parser.GetDefault();
        var c = uaParser.Parse(userAgent);
        return c.Device.IsSpider;
    }
}