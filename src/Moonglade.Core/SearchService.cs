using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Configuration.Abstraction;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Model;
using Moonglade.Model.Settings;

namespace Moonglade.Core
{
    public class SearchService : BlogService
    {
        private readonly IRepository<PostEntity> _postRepository;
        private readonly IBlogConfig _blogConfig;

        public SearchService(
            ILogger<PostService> logger,
            IOptions<AppSettings> settings,
            IRepository<PostEntity> postRepository,
            IBlogConfig blogConfig) : base(logger, settings)
        {
            _postRepository = postRepository;
            _blogConfig = blogConfig;
        }

        public async Task<IReadOnlyList<PostListEntry>> SearchAsync(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                throw new ArgumentNullException(keyword);
            }

            var postList = SearchByKeyword(keyword);

            var resultList = await postList.Select(p => new PostListEntry
            {
                Title = p.Title,
                Slug = p.Slug,
                ContentAbstract = p.ContentAbstract,
                PubDateUtc = p.PubDateUtc.GetValueOrDefault(),
                Tags = p.PostTag.Select(pt => new Tag
                {
                    NormalizedName = pt.Tag.NormalizedName,
                    DisplayName = pt.Tag.DisplayName
                })
            }).ToListAsync();

            return resultList;
        }

        public async Task WriteOpenSearchFileAsync(string siteRootUrl, string siteDataDirectory)
        {
            var openSearchDataFile = Path.Join($"{siteDataDirectory}", $"{Constants.OpenSearchFileName}");

            await using var fs = new FileStream(openSearchDataFile, FileMode.Create,
                FileAccess.Write, FileShare.None, 4096, true);
            var writerSettings = new XmlWriterSettings { Encoding = Encoding.UTF8, Indent = true, Async = true };
            using (var writer = XmlWriter.Create(fs, writerSettings))
            {
                await writer.WriteStartDocumentAsync();
                writer.WriteStartElement("OpenSearchDescription", "http://a9.com/-/spec/opensearch/1.1/");
                writer.WriteAttributeString("xmlns", "http://a9.com/-/spec/opensearch/1.1/");

                writer.WriteElementString("ShortName", _blogConfig.FeedSettings.RssTitle);
                writer.WriteElementString("Description", _blogConfig.FeedSettings.RssDescription);

                writer.WriteStartElement("Image");
                writer.WriteAttributeString("height", "16");
                writer.WriteAttributeString("width", "16");
                writer.WriteAttributeString("type", "image/vnd.microsoft.icon");
                writer.WriteValue($"{siteRootUrl}/favicon.ico");
                await writer.WriteEndElementAsync();

                writer.WriteStartElement("Url");
                writer.WriteAttributeString("type", "text/html");
                writer.WriteAttributeString("template", $"{siteRootUrl}/search/{{searchTerms}}");
                await writer.WriteEndElementAsync();

                await writer.WriteEndElementAsync();
            }
            await fs.FlushAsync();
        }

        private IQueryable<PostEntity> SearchByKeyword(string keyword)
        {
            var query = _postRepository.GetAsQueryable()
                                       .Where(p => !p.IsDeleted && p.IsPublished).AsNoTracking();

            var str = Regex.Replace(keyword, @"\s+", " ");
            var rst = str.Split(' ');
            if (rst.Length > 1)
            {
                // keyword: "dot  net rocks"
                // search for post where Title containing "dot && net && rocks"
                var result = rst.Aggregate(query, (current, s) => current.Where(p => p.Title.Contains(s)));
                return result;
            }
            else
            {
                // keyword: "dotnetrocks"
                var k = rst.First();
                var result = query.Where(p => p.Title.Contains(k) ||
                                              p.PostTag.Select(pt => pt.Tag).Select(t => t.DisplayName).Contains(k));
                return result;
            }
        }
    }
}
