using System;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Moonglade.Caching;

namespace Moonglade.Web.Filters
{
    public class DeleteSiteMapCache : ActionFilterAttribute
    {
        private readonly ILogger<DeleteSiteMapCache> _logger;
        private readonly IBlogCache _cache;

        public DeleteSiteMapCache(ILogger<DeleteSiteMapCache> logger, IBlogCache cache)
        {
            _logger = logger;
            _cache = cache;
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            base.OnActionExecuted(context);

            try
            {
                _cache.Remove(CacheDivision.General, "sitemap");
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error Delete sitemap cache");
            }
        }
    }
}
