using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
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
    public class SearchController : MoongladeController
    {
        private readonly BlogConfig _blogConfig;

        private readonly PostService _postService;

        public SearchController(MoongladeDbContext context,
            ILogger<OpmlController> logger,
            IOptions<AppSettings> settings,
            IConfiguration configuration,
            IHttpContextAccessor accessor, 
            IMemoryCache memoryCache, 
            PostService postService, 
            BlogConfig blogConfig,
            BlogConfigurationService blogConfigurationService)
            : base(context, logger, settings, configuration, accessor, memoryCache)
        {
            _postService = postService;
            _blogConfig = blogConfig;
            _blogConfig.GetConfiguration(blogConfigurationService);
        }

        [Route("opensearch")]
        public async Task<IActionResult> OpenSearch()
        {
            var openSearchDataFile = $@"{AppDomain.CurrentDomain.GetData(Constants.DataDirectory)}\{Constants.OpenSearchFileName}";
            if (!System.IO.File.Exists(openSearchDataFile))
            {
                Logger.LogInformation($"OpenSearch file not found, writing new file on {openSearchDataFile}");

                await WriteOpenSearchFileAsync(HttpContext);
                if (!System.IO.File.Exists(openSearchDataFile))
                {
                    Logger.LogError("OpenSearch file still not found, what the heck?!");
                    return NotFound();
                }
            }

            string openSearchContent = await System.IO.File.ReadAllTextAsync(openSearchDataFile, Encoding.UTF8);
            if (openSearchContent.Length > 0)
            {
                return Content(openSearchContent, "text/xml");
            }

            return NotFound();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("search")]
        public IActionResult Index(string term)
        {
            if (!string.IsNullOrWhiteSpace(term))
            {
                return RedirectToAction(nameof(SearchGet), new {term});
            }
            return RedirectToAction("Index", "Post");
        }

        [Route("search/{term}")]
        public IActionResult SearchGet(string term)
        {
            if (!string.IsNullOrWhiteSpace(term))
            {
                Logger.LogInformation("Searching post for keyword: " + term);

                ViewBag.TitlePrefix = term;

                var resultList = new List<SearchResult>();
                var response = _postService.SearchPost(term);
                if (response.IsSuccess)
                {
                    resultList = response.Item.ToList();
                }
                return View("Index", resultList);
            }
            return RedirectToAction("Index", "Post");
        }

        private async Task WriteOpenSearchFileAsync(HttpContext ctx)
        {
            var openSearchDataFile = $@"{AppDomain.CurrentDomain.GetData(Constants.DataDirectory)}\{Constants.OpenSearchFileName}";

            using (var fs = new FileStream(openSearchDataFile, FileMode.Create,
                FileAccess.Write, FileShare.None, 4096, true))
            {
                var writerSettings = new XmlWriterSettings { Encoding = Encoding.UTF8, Indent = true };
                using (var writer = XmlWriter.Create(fs, writerSettings))
                {
                    writer.WriteStartDocument();
                    writer.WriteStartElement("OpenSearchDescription", "http://a9.com/-/spec/opensearch/1.1/");
                    writer.WriteAttributeString("xmlns", "http://a9.com/-/spec/opensearch/1.1/");

                    writer.WriteElementString("ShortName", _blogConfig.FeedSettings.RssTitle);
                    writer.WriteElementString("Description", _blogConfig.FeedSettings.RssDescription);

                    writer.WriteStartElement("Image");
                    writer.WriteAttributeString("height", "16");
                    writer.WriteAttributeString("width", "16");
                    writer.WriteAttributeString("type", "image/vnd.microsoft.icon");
                    writer.WriteValue($"{ctx.Request.Scheme}://{ctx.Request.Host}/favicon.ico");
                    writer.WriteEndElement();

                    writer.WriteStartElement("Url");
                    writer.WriteAttributeString("type", "text/html");
                    writer.WriteAttributeString("template", $"{ctx.Request.Scheme}://{ctx.Request.Host}/search/{{searchTerms}}");
                    writer.WriteEndElement();

                    writer.WriteEndElement();
                }
                await fs.FlushAsync();
            }
        }
    }
}