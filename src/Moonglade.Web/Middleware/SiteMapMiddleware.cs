using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moonglade.Caching;
using Moonglade.Configuration;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;
using Moonglade.Utils;
using Moonglade.Web.Models;

namespace Moonglade.Web.Middleware
{
    public class SiteMapMiddleware
    {
        private readonly RequestDelegate _next;

        public SiteMapMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(
            HttpContext httpContext,
            IBlogConfig blogConfig,
            IBlogCache cache,
            IOptions<SiteMapSettings> settings,
            IRepository<PostEntity> postRepo,
            IRepository<PageEntity> pageRepo)
        {
            if (httpContext.Request.Path == "/sitemap.xml")
            {
                var xml = await cache.GetOrCreateAsync(CacheDivision.General, "sitemap", async _ =>
                {
                    var url = Helper.ResolveRootUrl(httpContext, blogConfig.GeneralSettings.CanonicalPrefix, true);
                    var data = await GetSiteMapData(url, settings.Value, postRepo, pageRepo);
                    return data;
                });

                httpContext.Response.ContentType = "text/xml";
                await httpContext.Response.WriteAsync(xml, httpContext.RequestAborted);
            }
            else
            {
                await _next(httpContext);
            }
        }

        private static async Task<string> GetSiteMapData(
            string siteRootUrl,
            SiteMapSettings settings,
            IRepository<PostEntity> postRepo,
            IRepository<PageEntity> pageRepo)
        {
            var sb = new StringBuilder();

            var writerSettings = new XmlWriterSettings { Encoding = Encoding.UTF8, Async = true };
            await using (var writer = XmlWriter.Create(sb, writerSettings))
            {
                await writer.WriteStartDocumentAsync();
                writer.WriteStartElement("urlset", settings.UrlSetNamespace);

                // Posts
                var spec = new PostSitePageSpec();
                var posts = await postRepo.SelectAsync(spec, p => new
                {
                    p.Slug,
                    p.PubDateUtc
                });

                foreach (var item in posts.OrderByDescending(p => p.PubDateUtc))
                {
                    var pubDate = item.PubDateUtc.GetValueOrDefault();

                    writer.WriteStartElement("url");
                    writer.WriteElementString("loc", $"{siteRootUrl}/post/{pubDate.Year}/{pubDate.Month}/{pubDate.Day}/{item.Slug.ToLower()}");
                    writer.WriteElementString("lastmod", pubDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                    writer.WriteElementString("changefreq", settings.ChangeFreq["Posts"]);
                    await writer.WriteEndElementAsync();
                }

                // Pages
                var pages = await pageRepo.SelectAsync(page => new
                {
                    page.CreateTimeUtc,
                    page.Slug,
                    page.IsPublished
                });

                foreach (var item in pages.Where(p => p.IsPublished))
                {
                    writer.WriteStartElement("url");
                    writer.WriteElementString("loc", $"{siteRootUrl}/page/{item.Slug.ToLower()}");
                    writer.WriteElementString("lastmod", item.CreateTimeUtc.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                    writer.WriteElementString("changefreq", settings.ChangeFreq["Pages"]);
                    await writer.WriteEndElementAsync();
                }

                // Tag
                writer.WriteStartElement("url");
                writer.WriteElementString("loc", $"{siteRootUrl}/tags");
                writer.WriteElementString("lastmod", DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                writer.WriteElementString("changefreq", settings.ChangeFreq["Default"]);
                await writer.WriteEndElementAsync();

                // Archive
                writer.WriteStartElement("url");
                writer.WriteElementString("loc", $"{siteRootUrl}/archive");
                writer.WriteElementString("lastmod", DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                writer.WriteElementString("changefreq", settings.ChangeFreq["Default"]);
                await writer.WriteEndElementAsync();

                await writer.WriteEndElementAsync();
            }

            var xml = sb.ToString();
            return xml;
        }
    }
}
