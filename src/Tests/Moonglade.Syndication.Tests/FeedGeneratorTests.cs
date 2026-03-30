using Moonglade.Data.DTO;
using System.Xml.Linq;

namespace Moonglade.Syndication.Tests;

public class FeedGeneratorTests
{
    private static FeedGenerator CreateGenerator()
    {
        return new FeedGenerator(
            "https://example.com",
            "Test Blog",
            "A test blog description",
            "© 2025 Test",
            "Moonglade vTest",
            "https://example.com",
            "en-us");
    }

    private static List<FeedEntry> CreateSampleEntries()
    {
        return
        [
            new()
            {
                Id = Guid.NewGuid().ToString(),
                Title = "First Post",
                Description = "First post description",
                PubDateUtc = new DateTime(2025, 1, 15, 10, 0, 0, DateTimeKind.Utc),
                Link = "https://example.com/post/first-post",
                Author = "Test Author",
                AuthorEmail = "test@example.com",
                LangCode = "en-us",
                Categories = ["Tech", "CSharp"]
            },
            new()
            {
                Id = Guid.NewGuid().ToString(),
                Title = "Second Post",
                Description = "Second post description",
                PubDateUtc = new DateTime(2025, 2, 20, 12, 0, 0, DateTimeKind.Utc),
                Link = "https://example.com/post/second-post",
                Author = "Test Author",
                AuthorEmail = "test@example.com",
                LangCode = "en-us",
                Categories = []
            }
        ];
    }

    [Fact]
    public void Constructor_Parameterless_InitializesEmptyCollection()
    {
        var gen = new FeedGenerator();
        Assert.NotNull(gen.FeedItemCollection);
        Assert.Empty(gen.FeedItemCollection);
    }

    [Fact]
    public void Constructor_WithParameters_SetsProperties()
    {
        var gen = CreateGenerator();

        Assert.Equal("https://example.com", gen.HostUrl);
        Assert.Equal("Test Blog", gen.HeadTitle);
        Assert.Equal("A test blog description", gen.HeadDescription);
        Assert.Equal("© 2025 Test", gen.Copyright);
        Assert.Equal("Moonglade vTest", gen.Generator);
        Assert.Equal("https://example.com", gen.TrackBackUrl);
        Assert.Equal("en-us", gen.Language);
    }

    [Fact]
    public async Task WriteRssAsync_EmptyItems_ProducesValidXml()
    {
        var gen = CreateGenerator();
        gen.FeedItemCollection = [];

        var xml = await gen.WriteRssAsync();

        Assert.NotNull(xml);
        Assert.NotEmpty(xml);

        var doc = XDocument.Parse(xml);
        var rss = doc.Root;
        Assert.NotNull(rss);
        Assert.Equal("rss", rss.Name.LocalName);
    }

    [Fact]
    public async Task WriteRssAsync_WithItems_ContainsItemElements()
    {
        var gen = CreateGenerator();
        gen.FeedItemCollection = CreateSampleEntries();

        var xml = await gen.WriteRssAsync();
        var doc = XDocument.Parse(xml);

        var channel = doc.Root.Element("channel");
        Assert.NotNull(channel);

        var items = channel.Elements("item").ToList();
        Assert.Equal(2, items.Count);
    }

    [Fact]
    public async Task WriteRssAsync_ContainsTitleAndDescription()
    {
        var gen = CreateGenerator();
        gen.FeedItemCollection = [];

        var xml = await gen.WriteRssAsync();
        var doc = XDocument.Parse(xml);
        var channel = doc.Root.Element("channel");

        Assert.Equal("Test Blog", channel.Element("title")?.Value);
        Assert.Equal("A test blog description", channel.Element("description")?.Value);
    }

    [Fact]
    public async Task WriteAtomAsync_EmptyItems_ProducesValidXml()
    {
        var gen = CreateGenerator();
        gen.FeedItemCollection = [];

        var xml = await gen.WriteAtomAsync();

        Assert.NotNull(xml);
        Assert.NotEmpty(xml);

        var doc = XDocument.Parse(xml);
        Assert.NotNull(doc.Root);
        Assert.Equal("feed", doc.Root.Name.LocalName);
    }

    [Fact]
    public async Task WriteAtomAsync_WithItems_ContainsEntryElements()
    {
        var gen = CreateGenerator();
        gen.FeedItemCollection = CreateSampleEntries();

        var xml = await gen.WriteAtomAsync();
        var doc = XDocument.Parse(xml);

        XNamespace atom = "http://www.w3.org/2005/Atom";
        var entries = doc.Root.Elements(atom + "entry").ToList();
        Assert.Equal(2, entries.Count);
    }

    [Fact]
    public async Task WriteAtomAsync_ContainsTitleElement()
    {
        var gen = CreateGenerator();
        gen.FeedItemCollection = [];

        var xml = await gen.WriteAtomAsync();
        var doc = XDocument.Parse(xml);

        XNamespace atom = "http://www.w3.org/2005/Atom";
        Assert.Equal("Test Blog", doc.Root.Element(atom + "title")?.Value);
    }

    [Fact]
    public async Task WriteRssAsync_ItemWithCategories_IncludesCategories()
    {
        var gen = CreateGenerator();
        gen.FeedItemCollection =
        [
            new()
            {
                Id = Guid.NewGuid().ToString(),
                Title = "Cat Post",
                Description = "Post with categories",
                PubDateUtc = new DateTime(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc),
                Link = "https://example.com/post/cat-post",
                Author = "Author",
                AuthorEmail = "a@b.com",
                Categories = ["Tech", "CSharp"]
            }
        ];

        var xml = await gen.WriteRssAsync();
        var doc = XDocument.Parse(xml);
        var item = doc.Root.Element("channel")?.Elements("item").First();

        var categories = item?.Elements("category").Select(c => c.Value).ToList();
        Assert.NotNull(categories);
        Assert.Contains("Tech", categories);
        Assert.Contains("CSharp", categories);
    }

    [Fact]
    public async Task WriteRssAsync_ItemWithNullAuthor_DoesNotThrow()
    {
        var gen = CreateGenerator();
        gen.FeedItemCollection =
        [
            new()
            {
                Id = Guid.NewGuid().ToString(),
                Title = "No Author",
                Description = "Post without author",
                PubDateUtc = new DateTime(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc),
                Link = "https://example.com/post/no-author",
                Author = null,
                AuthorEmail = null,
                Categories = null
            }
        ];

        var xml = await gen.WriteRssAsync();
        Assert.NotNull(xml);

        var doc = XDocument.Parse(xml);
        var items = doc.Root.Element("channel")?.Elements("item").ToList();
        Assert.Single(items);
    }

    [Fact]
    public async Task WriteRssAsync_NullFeedItemCollection_ProducesValidXml()
    {
        var gen = CreateGenerator();
        gen.FeedItemCollection = null;

        var xml = await gen.WriteRssAsync();
        Assert.NotNull(xml);

        var doc = XDocument.Parse(xml);
        var channel = doc.Root.Element("channel");
        var items = channel?.Elements("item").ToList();
        Assert.Empty(items);
    }
}
