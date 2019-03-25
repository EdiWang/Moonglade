using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Edi.Blog.OpmlFileWriter;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Configuration;
using Moonglade.Core;
using Moonglade.Data;
using Moonglade.Model;
using Moonglade.Model.Settings;

namespace Moonglade.Web.Controllers
{
    [Route("opml")]
    public class OpmlController : MoongladeController
    {
        private readonly CategoryService _categoryService;
        private readonly BlogConfig _blogConfig;

        public OpmlController(MoongladeDbContext context,
            ILogger<OpmlController> logger,
            IOptions<AppSettings> settings,
            IConfiguration configuration,
            IHttpContextAccessor accessor,
            IMemoryCache memoryCache,
            CategoryService categoryService,
            BlogConfig blogConfig,
            BlogConfigurationService blogConfigurationService)
            : base(context, logger, settings, configuration, accessor, memoryCache)
        {
            _categoryService = categoryService;
            _blogConfig = blogConfig;
            _blogConfig.GetConfiguration(blogConfigurationService);
        }

        public async Task<IActionResult> Index()
        {
            var feedDirectoryPath = $@"{AppDomain.CurrentDomain.GetData(Constants.DataDirectory)}\feed";
            if (!Directory.Exists(feedDirectoryPath))
            {
                Directory.CreateDirectory(feedDirectoryPath);
            }

            var opmlDataFile = $@"{AppDomain.CurrentDomain.GetData(Constants.DataDirectory)}\{Constants.OpmlFileName}";
            if (!System.IO.File.Exists(opmlDataFile))
            {
                Logger.LogInformation($"OPML file not found, writing new file on {opmlDataFile}");

                var catResponse = _categoryService.GetAllCategories();
                if (!catResponse.IsSuccess)
                {
                    return ServerError("Unsuccessful response from _categoryService.GetAllCategories().");
                }

                var catInfos = catResponse.Item.Select(c => new OpmlCategoryInfo
                {
                    DisplayName = c.DisplayName,
                    Title = c.Title
                });

                var oi = new OpmlInfo
                {
                    SiteTitle = $"{_blogConfig.SiteTitle} - OPML",
                    CategoryInfo = catInfos,
                    HtmlUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}/post",
                    XmlUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}/rss",
                    CategoryXmlUrlTemplate = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}/rss/category/[catTitle]",
                    CategoryHtmlUrlTemplate = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}/category/list/[catTitle]"
                };

                await FileSystemOpmlWriter.WriteOpmlFileAsync(
                    $@"{AppDomain.CurrentDomain.GetData(Constants.DataDirectory)}\{Constants.OpmlFileName}", oi);
                Logger.LogInformation("OPML file write completed.");

                if (!System.IO.File.Exists(opmlDataFile))
                {
                    Logger.LogInformation("OPML file still not found, something just went very very wrong...");
                    return NotFound();
                }
            }

            string opmlContent = await System.IO.File.ReadAllTextAsync(opmlDataFile, Encoding.UTF8);
            if (opmlContent.Length > 0)
            {
                return Content(opmlContent, "text/xml");
            }

            return NotFound();
        }
    }
}