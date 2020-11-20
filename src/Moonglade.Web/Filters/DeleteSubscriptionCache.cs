using System;
using System.IO;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Moonglade.Caching;
using Moonglade.Model;

namespace Moonglade.Web.Filters
{
    public class DeleteSubscriptionCache : ActionFilterAttribute
    {
        protected readonly ILogger<DeleteSubscriptionCache> Logger;
        private readonly IBlogCache _cache;

        public DeleteSubscriptionCache(ILogger<DeleteSubscriptionCache> logger, IBlogCache cache)
        {
            Logger = logger;
            _cache = cache;
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            base.OnActionExecuted(context);
            DeleteSubscriptionFiles();
        }

        private void DeleteSubscriptionFiles()
        {
            try
            {
                _cache.Remove(CacheDivision.General, "atom");

                var path = Path.Join($"{AppDomain.CurrentDomain.GetData(Constants.DataDirectory)}", "feed");
                var files = Directory.GetFiles(path);
                foreach (var file in files)
                {
                    File.Delete(file);
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error Delete Subscription Files");
            }
        }
    }
}
