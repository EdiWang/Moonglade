using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.SyndicationFeed;
using Microsoft.SyndicationFeed.Atom;
using Microsoft.SyndicationFeed.Rss;

namespace Moonglade.Syndication
{
    public class FeedGenerator : IFeedGenerator, IRssGenerator, IAtomGenerator
    {
        public FeedGenerator()
        {
            FeedItemCollection = new List<FeedEntry>();
        }

        #region Properties

        public IEnumerable<FeedEntry> FeedItemCollection { get; set; }
        public int MaxContentLength { get; set; }
        public string HostUrl { get; set; }
        public string HeadTitle { get; set; }
        public string HeadDescription { get; set; }
        public string Copyright { get; set; }
        public string Generator { get; set; }
        public string TrackBackUrl { get; set; }
        public string GeneratorVersion { get; set; }

        #endregion

        public async Task WriteRssStreamAsync(Stream stream)
        {
            var feed = GetItemCollection(FeedItemCollection);
            var settings = new XmlWriterSettings
            {
                Async = true,
                Encoding = Encoding.UTF8
            };

            await using var xmlWriter = XmlWriter.Create(stream, settings);
            var writer = new RssFeedWriter(xmlWriter);

            await writer.WriteTitle(HeadTitle);
            await writer.WriteDescription(HeadDescription);
            await writer.Write(new SyndicationLink(new(TrackBackUrl)));
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

        public async Task WriteAtomStreamAsync(Stream stream)
        {
            var feed = GetItemCollection(FeedItemCollection);
            var settings = new XmlWriterSettings
            {
                Async = true,
                Encoding = Encoding.UTF8
            };

            await using var xmlWriter = XmlWriter.Create(stream, settings);
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

        private static IEnumerable<SyndicationItem> GetItemCollection(IEnumerable<FeedEntry> itemCollection)
        {
            var synItemCollection = new List<SyndicationItem>();
            foreach (var item in itemCollection)
            {
                // create rss item
                var sItem = new SyndicationItem
                {
                    Id = item.Id,
                    Title = item.Title,
                    Description = item.Description,
                    LastUpdated = item.PubDateUtc.ToUniversalTime(),
                    Published = item.PubDateUtc.ToUniversalTime()
                };

                sItem.AddLink(new SyndicationLink(new(item.Link)));

                // add author
                if (!string.IsNullOrWhiteSpace(item.Author) && !string.IsNullOrWhiteSpace(item.AuthorEmail))
                {
                    sItem.AddContributor(new SyndicationPerson(item.Author, item.AuthorEmail));
                }

                // add categories
                if (item.Categories is not null and { Length: > 0 })
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
    }
}
