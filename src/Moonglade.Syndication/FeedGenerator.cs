using Edi.SyndicationFeed.ReaderWriter;
using Edi.SyndicationFeed.ReaderWriter.Atom;
using Edi.SyndicationFeed.ReaderWriter.Rss;
using System.Text;
using System.Xml;

namespace Moonglade.Syndication;

public class FeedGenerator : IFeedGenerator, IRssGenerator, IAtomGenerator
{
    public FeedGenerator()
    {
        FeedItemCollection = new List<FeedEntry>();
    }

    public FeedGenerator(string hostUrl, string headTitle, string headDescription, string copyright, string generator, string trackBackUrl, string language)
    {
        HostUrl = hostUrl;
        HeadTitle = headTitle;
        HeadDescription = headDescription;
        Copyright = copyright;
        Generator = generator;
        TrackBackUrl = trackBackUrl;
        Language = language;

        FeedItemCollection = new List<FeedEntry>();
    }

    #region Properties

    public IEnumerable<FeedEntry> FeedItemCollection { get; set; }
    public string HostUrl { get; set; }
    public string HeadTitle { get; set; }
    public string HeadDescription { get; set; }
    public string Copyright { get; set; }
    public string Generator { get; set; }
    public string TrackBackUrl { get; set; }
    public string GeneratorVersion { get; set; }
    public string Language { get; set; }

    #endregion

    public async Task<string> WriteRssAsync()
    {
        var feed = GetItemCollection(FeedItemCollection);

        var sw = new StringWriterWithEncoding(Encoding.UTF8);
        await using (var xmlWriter = XmlWriter.Create(sw, new() { Async = true }))
        {
            var writer = new RssFeedWriter(xmlWriter);

            await writer.WriteTitle(HeadTitle);
            await writer.WriteDescription(HeadDescription);
            await writer.Write(new SyndicationLink(new(TrackBackUrl)));
            await writer.WritePubDate(DateTimeOffset.UtcNow);
            await writer.WriteCopyright(Copyright);
            await writer.WriteGenerator(Generator);
            await writer.WriteLanguage(new(Language));

            foreach (var item in feed)
            {
                await writer.Write(item);
            }

            await xmlWriter.FlushAsync();
            xmlWriter.Close();
        }

        var xml = sw.ToString();
        return xml;
    }

    public async Task<string> WriteAtomAsync()
    {
        var feed = GetItemCollection(FeedItemCollection);

        var sw = new StringWriterWithEncoding(Encoding.UTF8);
        await using (var xmlWriter = XmlWriter.Create(sw, new() { Async = true }))
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

        var xml = sw.ToString();
        return xml;
    }

    private static IEnumerable<SyndicationItem> GetItemCollection(IEnumerable<FeedEntry> itemCollection)
    {
        var synItemCollection = new List<SyndicationItem>();
        if (null == itemCollection) return synItemCollection;

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
            if (item.Categories is { Length: > 0 })
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

public class StringWriterWithEncoding : StringWriter
{
    public StringWriterWithEncoding(Encoding encoding)
    {
        Encoding = encoding;
    }

    public override Encoding Encoding { get; }
}