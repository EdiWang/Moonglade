using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Moonglade.Model;

namespace Moonglade.Web.Filters
{
    public class DeleteSiteMapCache : ActionFilterAttribute
    {
        protected readonly ILogger<DeleteSiteMapCache> Logger;

        public DeleteSiteMapCache(ILogger<DeleteSiteMapCache> logger)
        {
            Logger = logger;
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            base.OnActionExecuted(context);

            try
            {
                var path = Path.Join($"{AppDomain.CurrentDomain.GetData(Constants.DataDirectory)}", Constants.SiteMapFileName);
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error Delete sitemap.xml");
            }
        }
    }
}
