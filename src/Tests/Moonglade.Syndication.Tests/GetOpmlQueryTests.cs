using System.Xml.Linq;

namespace Moonglade.Syndication.Tests;

public class GetOpmlQueryTests
{
    private static OpmlDoc CreateSampleOpmlDoc()
    {
        return new OpmlDoc
        {
            SiteTitle = "Test Blog",
            HtmlUrl = "https://example.com",
            XmlUrl = "https://example.com/rss",
            XmlUrlTemplate = "https://example.com/rss/category/[catTitle]",
            HtmlUrlTemplate = "https://example.com/category/[catTitle]",
            ContentInfo =
            [
                new("Tech", "tech"),
                new("Life", "life")
            ]
        };
    }

    [Fact]
    public async Task HandleAsync_ProducesOpmlWithMetadataAndAllOutlines()
    {
        var handler = new GetOpmlQueryHandler();
        var query = new GetOpmlQuery(CreateSampleOpmlDoc());

        var result = await handler.HandleAsync(query, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.NotEmpty(result);

        var doc = XDocument.Parse(result);
        Assert.NotNull(doc.Root);
        Assert.Equal("opml", doc.Root.Name.LocalName);
        Assert.Equal("1.0", doc.Root.Attribute("version")?.Value);
        Assert.Equal("Test Blog", doc.Root.Element("head")?.Element("title")?.Value);
        var body = doc.Root.Element("body");
        Assert.NotNull(body);
        Assert.Collection(body.Elements("outline"),
            allPosts =>
            {
                Assert.Equal("All Posts", allPosts.Attribute("title")?.Value);
                Assert.Equal("rss", allPosts.Attribute("type")?.Value);
                Assert.Equal("https://example.com/rss", allPosts.Attribute("xmlUrl")?.Value);
                Assert.Equal("https://example.com", allPosts.Attribute("htmlUrl")?.Value);
            },
            tech =>
            {
                Assert.Equal("Tech", tech.Attribute("title")?.Value);
                Assert.Equal("rss", tech.Attribute("type")?.Value);
                Assert.Equal("https://example.com/rss/category/tech", tech.Attribute("xmlUrl")?.Value);
                Assert.Equal("https://example.com/category/tech", tech.Attribute("htmlUrl")?.Value);
            },
            life =>
            {
                Assert.Equal("Life", life.Attribute("title")?.Value);
                Assert.Equal("rss", life.Attribute("type")?.Value);
                Assert.Equal("https://example.com/rss/category/life", life.Attribute("xmlUrl")?.Value);
                Assert.Equal("https://example.com/category/life", life.Attribute("htmlUrl")?.Value);
            });
    }

    [Fact]
    public async Task HandleAsync_EmptyCategories_OnlyAllPostsOutline()
    {
        var opmlDoc = new OpmlDoc
        {
            SiteTitle = "Empty Blog",
            HtmlUrl = "https://example.com",
            XmlUrl = "https://example.com/rss",
            XmlUrlTemplate = "https://example.com/rss/category/[catTitle]",
            HtmlUrlTemplate = "https://example.com/category/[catTitle]",
            ContentInfo = []
        };

        var handler = new GetOpmlQueryHandler();
        var query = new GetOpmlQuery(opmlDoc);

        var result = await handler.HandleAsync(query, TestContext.Current.CancellationToken);
        var doc = XDocument.Parse(result);

        var body = doc.Root.Element("body");
        var outlines = body.Elements("outline").ToList();

        Assert.Single(outlines);
        Assert.Equal("All Posts", outlines[0].Attribute("title")?.Value);
    }

}
