using Microsoft.AspNetCore.Mvc.Filters;
using Moonglade.Utils;

namespace Moonglade.Web.Filters
{
    public class AppendAppVersion : ResultFilterAttribute
    {
        public override void OnResultExecuting(ResultExecutingContext context)
        {
            context.HttpContext.Response.Headers.Add("X-Moonglade-Version", Helper.AppVersion);
            base.OnResultExecuting(context);
        }
    }
}
