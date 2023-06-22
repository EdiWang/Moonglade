using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;
using System.Globalization;
using System.Xml;

namespace Moonglade.Web.Middleware;

public class SiteMapMiddleware
{
    private readonly RequestDelegate _next;

    public SiteMapMiddleware(RequestDelegate next) => _next = next;

    public async Task Invoke(
        HttpContext httpContext,
        IBlogConfig blogConfig,
        ICacheAside cache,
        IRepository<PostEntity> postRepo,
        IRepository<PageEntity> pageRepo)
    {
        if (blogConfig.AdvancedSettings.EnableSiteMap && httpContext.Request.Path == "/sitemap.xml")
        {
            var xml = await cache.GetOrCreateAsync(BlogCachePartition.General.ToString(), "sitemap", async _ =>
            {
                var url = Helper.ResolveRootUrl(httpContext, blogConfig.GeneralSettings.CanonicalPrefix, true, true);
                var data = await GetSiteMapData(url, postRepo, pageRepo, httpContext.RequestAborted);
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
        IRepository<PostEntity> postRepo,
        IRepository<PageEntity> pageRepo,
        CancellationToken ct)
    {
        var sb = new StringBuilder();

        var writerSettings = new XmlWriterSettings { Encoding = Encoding.UTF8, Async = true };
        await using (var writer = XmlWriter.Create(sb, writerSettings))
        {
            await writer.WriteStartDocumentAsync();
            writer.WriteStartElement("urlset", "http://www.sitemaps.org/schemas/sitemap/0.9");

            // Posts
            var spec = new PostSitePageSpec();
            var posts = await postRepo
                .SelectAsync(spec, p => new Tuple<string, DateTime?, DateTime?>(p.Slug, p.PubDateUtc, p.LastModifiedUtc), ct);

            foreach (var (slug, pubDateUtc, lastModifyUtc) in posts.OrderByDescending(p => p.Item2))
            {
                var pubDate = pubDateUtc.GetValueOrDefault();

                writer.WriteStartElement("url");
                writer.WriteElementString("loc", $"{siteRootUrl}/post/{pubDate.Year}/{pubDate.Month}/{pubDate.Day}/{slug.ToLower()}");
                writer.WriteElementString("lastmod", pubDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                writer.WriteElementString("changefreq", GetChangeFreq(pubDateUtc.GetValueOrDefault(), lastModifyUtc));
                await writer.WriteEndElementAsync();
            }

            // Pages
            var pages = await pageRepo.SelectAsync(page => new Tuple<DateTime, DateTime?, string, bool>(
                page.CreateTimeUtc,
                page.UpdateTimeUtc,
                page.Slug,
                page.IsPublished)
            );

            foreach (var (createdTimeUtc, updateTimeUtc, slug, isPublished) in pages.Where(p => p.Item4))
            {
                writer.WriteStartElement("url");
                writer.WriteElementString("loc", $"{siteRootUrl}/page/{slug.ToLower()}");
                writer.WriteElementString("lastmod", createdTimeUtc.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                writer.WriteElementString("changefreq", GetChangeFreq(createdTimeUtc, updateTimeUtc));
                await writer.WriteEndElementAsync();
            }

            // Tag
            writer.WriteStartElement("url");
            writer.WriteElementString("loc", $"{siteRootUrl}/tags");
            writer.WriteElementString("lastmod", DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            writer.WriteElementString("changefreq", "weekly");
            await writer.WriteEndElementAsync();

            // Archive
            writer.WriteStartElement("url");
            writer.WriteElementString("loc", $"{siteRootUrl}/archive");
            writer.WriteElementString("lastmod", DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            writer.WriteElementString("changefreq", "monthly");
            await writer.WriteEndElementAsync();

            await writer.WriteEndElementAsync();
        }

        var xml = sb.ToString();
        return xml;
    }

    private static string GetChangeFreq(DateTime pubDate, DateTime? modifyDate)
    {
        if (modifyDate == null || modifyDate == pubDate) return "monthly";

        var lastModifyFromNow = (DateTime.UtcNow - modifyDate.Value).Days;
        switch (lastModifyFromNow)
        {
            case <= 60:
                {
                    var interval = Math.Abs((modifyDate.Value - pubDate).Days);

                    return interval switch
                    {
                        < 7 => "daily",
                        >= 7 and <= 14 => "weekly",
                        > 14 => "monthly"
                    };
                }
            case > 60:
                return "yearly";
        }
    }
}