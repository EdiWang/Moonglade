using Microsoft.AspNetCore.Mvc.Filters;
using UAParser;

namespace Moonglade.Web.Filters;

public class DisallowSpiderUA : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var userAgent = context.HttpContext.Request.Headers["User-Agent"];
        if (!string.IsNullOrWhiteSpace(userAgent))
        {
            if (IsMachineUA(userAgent))
            {
                context.Result = new ForbidResult();
            }
            else
            {
                base.OnActionExecuting(context);
            }
        }
        else
        {
            context.Result = new BadRequestResult();
        }
    }

    private static bool IsMachineUA(string userAgent)
    {
        var uaParser = Parser.GetDefault();
        var c = uaParser.Parse(userAgent);
        return c.Device.IsSpider;
    }
}