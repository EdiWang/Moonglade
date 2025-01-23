using Microsoft.AspNetCore.Mvc.Filters;

namespace Moonglade.Web.Filters;

public class ReadonlyModeAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (Helper.GetAppDomainData<bool>("IsReadonlyMode"))
        {
            context.Result = new ForbidResult();
        }
    }
}