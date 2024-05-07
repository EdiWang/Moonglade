using Moonglade.Data.Entities;
using Moonglade.Data.Specifications;
using System.Globalization;
using System.Xml;

namespace Moonglade.Web.Middleware;

public class SiteMapMiddleware(RequestDelegate next)
{
    public async Task Invoke(
        HttpContext httpContext,
        IBlogConfig blogConfig,
        ICacheAside cache,
        MoongladeRepository<PostEntity> postRepo,
        MoongladeRepository<PageEntity> pageRepo)
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

    private static async Task<string> GetSiteMapData(
        string siteRootUrl,
        MoongladeRepository<PostEntity> postRepo,
        MoongladeRepository<PageEntity> pageRepo,
        CancellationToken ct)
    {
        var sb = new StringBuilder();

        var writerSettings = new XmlWriterSettings { Encoding = Encoding.UTF8, Async = true };
        await using (var writer = XmlWriter.Create(sb, writerSettings))
        {
            await writer.WriteStartDocumentAsync();
            writer.WriteStartElement("urlset", "http://www.sitemaps.org/schemas/sitemap/0.9");

            // Posts
            var spec = new PostSiteMapSpec();
            var posts = await postRepo.ListAsync(spec, ct);

            foreach (var item in posts.OrderByDescending(p => p.UpdateTimeUtc))
            {
                var pubDate = item.CreateTimeUtc;

                writer.WriteStartElement("url");
                writer.WriteElementString("loc", $"{siteRootUrl}/post/{pubDate.Year}/{pubDate.Month}/{pubDate.Day}/{item.Slug.ToLower()}");
                writer.WriteElementString("lastmod", pubDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                writer.WriteElementString("changefreq", GetChangeFreq(pubDate, item.UpdateTimeUtc));
                await writer.WriteEndElementAsync();
            }

            // Pages
            var pages = await pageRepo.ListAsync(new PageSitemapSpec(), ct);
            foreach (var page in pages)
            {
                writer.WriteStartElement("url");
                writer.WriteElementString("loc", $"{siteRootUrl}/page/{page.Slug.ToLower()}");
                writer.WriteElementString("lastmod", page.CreateTimeUtc.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                writer.WriteElementString("changefreq", GetChangeFreq(page.CreateTimeUtc, page.UpdateTimeUtc));
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