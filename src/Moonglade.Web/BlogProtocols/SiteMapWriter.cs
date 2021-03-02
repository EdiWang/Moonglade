using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Extensions.Options;
using Moonglade.Configuration.Settings;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;

namespace Moonglade.Web.BlogProtocols
{
    public interface ISiteMapWriter
    {
        Task<byte[]> GetSiteMapData(string siteRootUrl);
    }

    public class SiteMapWriter : ISiteMapWriter
    {
        private readonly AppSettings _settings;
        private readonly IRepository<PostEntity> _postRepo;
        private readonly IRepository<PageEntity> _pageRepo;

        public SiteMapWriter(
            IOptions<AppSettings> settings,
            IRepository<PostEntity> postRepo,
            IRepository<PageEntity> pageRepo)
        {
            _postRepo = postRepo;
            _pageRepo = pageRepo;
            _settings = settings.Value;
        }

        public async Task<byte[]> GetSiteMapData(string siteRootUrl)
        {
            await using var ms = new MemoryStream();
            var writerSettings = new XmlWriterSettings { Encoding = Encoding.UTF8, Async = true };
            await using (var writer = XmlWriter.Create(ms, writerSettings))
            {
                await writer.WriteStartDocumentAsync();
                writer.WriteStartElement("urlset", _settings.SiteMap.UrlSetNamespace);

                // Posts
                var spec = new PostSitePageSpec();
                var posts = await _postRepo.SelectAsync(spec, p => new
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
                    writer.WriteElementString("changefreq", _settings.SiteMap.ChangeFreq["Posts"]);
                    await writer.WriteEndElementAsync();
                }

                // Pages
                var pages = await _pageRepo.SelectAsync(page => new
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
                    writer.WriteElementString("changefreq", _settings.SiteMap.ChangeFreq["Pages"]);
                    await writer.WriteEndElementAsync();
                }

                // Tag
                writer.WriteStartElement("url");
                writer.WriteElementString("loc", $"{siteRootUrl}/tags");
                writer.WriteElementString("lastmod", DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                writer.WriteElementString("changefreq", _settings.SiteMap.ChangeFreq["Default"]);
                await writer.WriteEndElementAsync();

                // Archive
                writer.WriteStartElement("url");
                writer.WriteElementString("loc", $"{siteRootUrl}/archive");
                writer.WriteElementString("lastmod", DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                writer.WriteElementString("changefreq", _settings.SiteMap.ChangeFreq["Default"]);
                await writer.WriteEndElementAsync();

                await writer.WriteEndElementAsync();
            }
            await ms.FlushAsync();
            return ms.ToArray();
        }
    }
}
