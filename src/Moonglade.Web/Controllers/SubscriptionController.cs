using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Core;
using Moonglade.Data;
using Moonglade.Model;
using Moonglade.Model.Settings;

namespace Moonglade.Web.Controllers
{
    public class SubscriptionController : MoongladeController
    {
        private readonly SyndicationFeedService _syndicationFeedService;

        public SubscriptionController(MoongladeDbContext context,
            ILogger<SubscriptionController> logger,
            IOptions<AppSettings> settings,
            IConfiguration configuration,
            IHttpContextAccessor accessor,
            IMemoryCache memoryCache, SyndicationFeedService syndicationFeedService)
            : base(context, logger, settings, configuration, accessor, memoryCache)
        {
            _syndicationFeedService = syndicationFeedService;
        }

        [Route("rss/{categoryName?}")]
        public async Task<IActionResult> Rss(string categoryName = null)
        {
            var rssDataFile = string.IsNullOrWhiteSpace(categoryName) ? 
                $@"{AppDomain.CurrentDomain.GetData(Constants.DataDirectory)}\feed\posts.xml" :
                $@"{AppDomain.CurrentDomain.GetData(Constants.DataDirectory)}\feed\posts-category-{categoryName}.xml";

            if (!System.IO.File.Exists(rssDataFile))
            {
                Logger.LogInformation($"RSS file not found, writing new file on {rssDataFile}");

                if (string.IsNullOrWhiteSpace(categoryName))
                {
                    await _syndicationFeedService.RefreshFeedFileForPostAsync(false);
                }
                else
                {
                    await _syndicationFeedService.RefreshRssFilesForCategoryAsync(categoryName.ToLower());
                }

                if (!System.IO.File.Exists(rssDataFile))
                {
                    return NotFound();
                }
            }

            string rssContent = await System.IO.File.ReadAllTextAsync(rssDataFile, Encoding.UTF8);
            if (rssContent.Length > 0)
            {
                return Content(rssContent, "text/xml");
            }

            return NotFound();
        }

        [Route("atom")]
        public async Task<IActionResult> Atom()
        {
            var atomDataFile = $@"{AppDomain.CurrentDomain.GetData(Constants.DataDirectory)}\feed\posts-atom.xml";
            if (!System.IO.File.Exists(atomDataFile))
            {
                Logger.LogInformation($"Atom file not found, writing new file on {atomDataFile}");

                await _syndicationFeedService.RefreshFeedFileForPostAsync(true);

                if (!System.IO.File.Exists(atomDataFile))
                {
                    return NotFound();
                }
            }

            string atomContent = await System.IO.File.ReadAllTextAsync(atomDataFile, Encoding.UTF8);
            if (atomContent.Length > 0)
            {
                return Content(atomContent, "text/xml");
            }

            return NotFound();
        }
    }
}