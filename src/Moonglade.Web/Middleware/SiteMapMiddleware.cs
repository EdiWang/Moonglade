using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Moonglade.Caching;
using Moonglade.Configuration;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;
using Moonglade.Utils;
using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

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
            IConfiguration configuration,
            IRepository<PostEntity> postRepo,
            IRepository<PageEntity> pageRepo)
        {
            if (httpContext.Request.Path == "/sitemap.xml")
            {
                var xml = await cache.GetOrCreateAsync(CacheDivision.General, "sitemap", async _ =>
                {
                    var url = Helper.ResolveRootUrl(httpContext, blogConfig.GeneralSettings.CanonicalPrefix, true, true);
                    var data = await GetSiteMapData(url, configuration.GetSection("SiteMap"), postRepo, pageRepo);
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
            IConfigurationSection siteMapSection,
            IRepository<PostEntity> postRepo,
            IRepository<PageEntity> pageRepo)
        {
            var sb = new StringBuilder();

            var writerSettings = new XmlWriterSettings { Encoding = Encoding.UTF8, Async = true };
            await using (var writer = XmlWriter.Create(sb, writerSettings))
            {
                await writer.WriteStartDocumentAsync();
                writer.WriteStartElement("urlset", siteMapSection["UrlSetNamespace"]);

                // Posts
                var spec = new PostSitePageSpec();
                var posts = await postRepo.SelectAsync(spec, p => new Tuple<string, DateTime?>(p.Slug, p.PubDateUtc));

                foreach (var (slug, pubDateUtc) in posts.OrderByDescending(p => p.Item2))
                {
                    var pubDate = pubDateUtc.GetValueOrDefault();

                    writer.WriteStartElement("url");
                    writer.WriteElementString("loc", $"{siteRootUrl}/post/{pubDate.Year}/{pubDate.Month}/{pubDate.Day}/{slug.ToLower()}");
                    writer.WriteElementString("lastmod", pubDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                    writer.WriteElementString("changefreq", siteMapSection["ChangeFreq:Posts"]);
                    await writer.WriteEndElementAsync();
                }

                // Pages
                var pages = await pageRepo.SelectAsync(page => new Tuple<DateTime, string, bool>(
                    page.CreateTimeUtc,
                    page.Slug,
                    page.IsPublished)
                );

                foreach (var (createdTimeUtc, slug, isPublished) in pages.Where(p => p.Item3))
                {
                    writer.WriteStartElement("url");
                    writer.WriteElementString("loc", $"{siteRootUrl}/page/{slug.ToLower()}");
                    writer.WriteElementString("lastmod", createdTimeUtc.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                    writer.WriteElementString("changefreq", siteMapSection["ChangeFreq:Pages"]);
                    await writer.WriteEndElementAsync();
                }

                // Tag
                writer.WriteStartElement("url");
                writer.WriteElementString("loc", $"{siteRootUrl}/tags");
                writer.WriteElementString("lastmod", DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                writer.WriteElementString("changefreq", siteMapSection["ChangeFreq:Default"]);
                await writer.WriteEndElementAsync();

                // Archive
                writer.WriteStartElement("url");
                writer.WriteElementString("loc", $"{siteRootUrl}/archive");
                writer.WriteElementString("lastmod", DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                writer.WriteElementString("changefreq", siteMapSection["ChangeFreq:Default"]);
                await writer.WriteEndElementAsync();

                await writer.WriteEndElementAsync();
            }

            var xml = sb.ToString();
            return xml;
        }
    }
}
