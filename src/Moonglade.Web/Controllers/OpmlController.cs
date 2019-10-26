using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Edi.Blog.OpmlFileWriter;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moonglade.Configuration.Abstraction;
using Moonglade.Core;
using Moonglade.Model;

namespace Moonglade.Web.Controllers
{
    [Route("opml")]
    public class OpmlController : MoongladeController
    {
        private readonly CategoryService _categoryService;
        private readonly IBlogConfig _blogConfig;
        private readonly IFileSystemOpmlWriter _fileSystemOpmlWriter;

        public OpmlController(
            ILogger<OpmlController> logger,
            CategoryService categoryService,
            IBlogConfig blogConfig,
            IFileSystemOpmlWriter fileSystemOpmlWriter)
            : base(logger)
        {
            _categoryService = categoryService;
            _blogConfig = blogConfig;
            _fileSystemOpmlWriter = fileSystemOpmlWriter;
        }

        public async Task<IActionResult> Index()
        {
            var feedDirectoryPath = $@"{AppDomain.CurrentDomain.GetData(Constants.DataDirectory)}\feed";
            if (!Directory.Exists(feedDirectoryPath))
            {
                Directory.CreateDirectory(feedDirectoryPath);
                Logger.LogInformation($"Created directory '{feedDirectoryPath}'");
            }

            var opmlDataFile = $@"{AppDomain.CurrentDomain.GetData(Constants.DataDirectory)}\{Constants.OpmlFileName}";
            if (!System.IO.File.Exists(opmlDataFile))
            {
                Logger.LogInformation($"OPML file not found, writing new file on {opmlDataFile}");

                var catResponse = await _categoryService.GetAllCategoriesAsync();
                if (!catResponse.IsSuccess)
                {
                    return ServerError("Unsuccessful response from _categoryService.GetAllCategoriesAsync().");
                }

                var catInfos = catResponse.Item.Select(c => new OpmlCategoryInfo
                {
                    DisplayName = c.DisplayName,
                    Title = c.Name
                });

                var oi = new OpmlInfo
                {
                    SiteTitle = $"{_blogConfig.GeneralSettings.SiteTitle} - OPML",
                    CategoryInfo = catInfos,
                    HtmlUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}/post",
                    XmlUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}/rss",
                    CategoryXmlUrlTemplate = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}/rss/category/[catTitle]",
                    CategoryHtmlUrlTemplate = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}/category/list/[catTitle]"
                };

                await _fileSystemOpmlWriter.WriteOpmlFileAsync(
                    $@"{AppDomain.CurrentDomain.GetData(Constants.DataDirectory)}\{Constants.OpmlFileName}", oi);
                Logger.LogInformation("OPML file write completed.");

                if (!System.IO.File.Exists(opmlDataFile))
                {
                    Logger.LogInformation("OPML file still not found, something just went very very wrong...");
                    return NotFound();
                }
            }

            if (System.IO.File.Exists(opmlDataFile))
            {
                return PhysicalFile(opmlDataFile, "text/xml");
            }

            return NotFound();
        }
    }
}