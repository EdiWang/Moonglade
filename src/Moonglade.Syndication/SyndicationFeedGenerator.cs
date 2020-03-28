using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using Microsoft.SyndicationFeed;
using Microsoft.SyndicationFeed.Atom;
using Microsoft.SyndicationFeed.Rss;

namespace Edi.SyndicationFeedGenerator
{
    public class SyndicationFeedGenerator : ISyndicationFeedGenerator, IRssSyndicationGenerator, IAtomSyndicationGenerator
    {
        public SyndicationFeedGenerator()
        {
            FeedItemCollection = new List<SimpleFeedItem>();
        }

        #region Properties

        public IEnumerable<SimpleFeedItem> FeedItemCollection { get; set; }

        public int MaxContentLength { get; set; }

        public string HostUrl { get; set; }

        public string HeadTitle { get; set; }

        public string HeadDescription { get; set; }

        public string Copyright { get; set; }

        public string Generator { get; set; }

        public string TrackBackUrl { get; set; }

        public string GeneratorVersion { get; set; }

        #endregion

        private IEnumerable<SyndicationItem> GetSyndicationItemCollection(IEnumerable<SimpleFeedItem> itemCollection)
        {
            var synItemCollection = new List<SyndicationItem>();
            foreach (var item in itemCollection)
            {
                // create rss item
                var sItem = new SyndicationItem
                {
                    Id = item.Id,
                    Title = item.Title,
                    Description = HttpUtility.HtmlDecode(item.Description),
                    LastUpdated = item.PubDateUtc.ToUniversalTime(),
                    Published = item.PubDateUtc.ToUniversalTime(),
                };

                sItem.AddLink(new SyndicationLink(new Uri(item.Link)));

                // add author
                if (!string.IsNullOrWhiteSpace(item.Author) && !string.IsNullOrWhiteSpace(item.AuthorEmail))
                {
                    sItem.AddContributor(new SyndicationPerson(item.Author, item.AuthorEmail));
                }

                // add categories
                if (null != item.Categories && item.Categories.Any())
                {
                    foreach (var itemCategory in item.Categories)
                    {
                        sItem.AddCategory(new SyndicationCategory(itemCategory));
                    }
                }
                synItemCollection.Add(sItem);
            }
            return synItemCollection;
        }

        public async Task WriteRss20FileAsync(string absolutePath)
        {
            var feed = GetSyndicationItemCollection(FeedItemCollection);
            var settings = new XmlWriterSettings
            {
                Async = true,
                Encoding = Encoding.UTF8,
                Indent = true
            };

            using (var xmlWriter = XmlWriter.Create(absolutePath, settings))
            {
                var writer = new RssFeedWriter(xmlWriter);

                await writer.WriteTitle(HeadTitle);
                await writer.WriteDescription(HeadDescription);
                await writer.Write(new SyndicationLink(new Uri(TrackBackUrl)));
                await writer.WritePubDate(DateTimeOffset.UtcNow);
                await writer.WriteCopyright(Copyright);
                await writer.WriteGenerator(Generator);

                foreach (var item in feed)
                {
                    await writer.Write(item);
                }

                await xmlWriter.FlushAsync();
                xmlWriter.Close();
            }
        }

        public async Task WriteAtom10FileAsync(string absolutePath)
        {
            var feed = GetSyndicationItemCollection(FeedItemCollection);
            var settings = new XmlWriterSettings
            {
                Async = true,
                Encoding = Encoding.UTF8,
                Indent = true
            };

            using (var xmlWriter = XmlWriter.Create(absolutePath, settings))
            {
                var writer = new AtomFeedWriter(xmlWriter);

                await writer.WriteTitle(HeadTitle);
                await writer.WriteSubtitle(HeadDescription);
                await writer.WriteRights(Copyright);
                await writer.WriteUpdated(DateTime.UtcNow);
                await writer.WriteGenerator(Generator, HostUrl, GeneratorVersion);

                foreach (var item in feed)
                {
                    await writer.Write(item);
                }

                await xmlWriter.FlushAsync();
                xmlWriter.Close();
            }
        }
    }
}
