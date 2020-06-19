using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;
using Moonglade.Core;

namespace Moonglade.Web.Filters
{
    public class AppendMoongladeVersion : ResultFilterAttribute
    {
        public override void OnResultExecuting(ResultExecutingContext context)
        {
            context.HttpContext.Response.Headers.Add("X-Moonglade-Version", Utils.AppVersion);
            base.OnResultExecuting(context);
        }
    }
}
