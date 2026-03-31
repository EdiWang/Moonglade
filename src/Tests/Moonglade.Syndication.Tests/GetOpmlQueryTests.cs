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
    public async Task HandleAsync_ProducesValidOpmlXml()
    {
        var handler = new GetOpmlQueryHandler();
        var query = new GetOpmlQuery(CreateSampleOpmlDoc());

        var result = await handler.HandleAsync(query, CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotEmpty(result);

        var doc = XDocument.Parse(result);
        Assert.Equal("opml", doc.Root.Name.LocalName);
    }

    [Fact]
    public async Task HandleAsync_ContainsVersionAttribute()
    {
        var handler = new GetOpmlQueryHandler();
        var query = new GetOpmlQuery(CreateSampleOpmlDoc());

        var result = await handler.HandleAsync(query, CancellationToken.None);
        var doc = XDocument.Parse(result);

        Assert.Equal("1.0", doc.Root.Attribute("version")?.Value);
    }

    [Fact]
    public async Task HandleAsync_ContainsHeadWithTitle()
    {
        var handler = new GetOpmlQueryHandler();
        var query = new GetOpmlQuery(CreateSampleOpmlDoc());

        var result = await handler.HandleAsync(query, CancellationToken.None);
        var doc = XDocument.Parse(result);

        var head = doc.Root.Element("head");
        Assert.NotNull(head);
        Assert.Equal("Test Blog", head.Element("title")?.Value);
    }

    [Fact]
    public async Task HandleAsync_ContainsAllPostsOutline()
    {
        var handler = new GetOpmlQueryHandler();
        var query = new GetOpmlQuery(CreateSampleOpmlDoc());

        var result = await handler.HandleAsync(query, CancellationToken.None);
        var doc = XDocument.Parse(result);

        var body = doc.Root.Element("body");
        Assert.NotNull(body);

        var outlines = body.Elements("outline").ToList();
        var allPosts = outlines.FirstOrDefault(o => o.Attribute("title")?.Value == "All Posts");
        Assert.NotNull(allPosts);
        Assert.Equal("rss", allPosts.Attribute("type")?.Value);
        Assert.Equal("https://example.com/rss", allPosts.Attribute("xmlUrl")?.Value);
        Assert.Equal("https://example.com", allPosts.Attribute("htmlUrl")?.Value);
    }

    [Fact]
    public async Task HandleAsync_ContainsCategoryOutlines()
    {
        var handler = new GetOpmlQueryHandler();
        var query = new GetOpmlQuery(CreateSampleOpmlDoc());

        var result = await handler.HandleAsync(query, CancellationToken.None);
        var doc = XDocument.Parse(result);

        var body = doc.Root.Element("body");
        var outlines = body.Elements("outline").ToList();

        // 1 "All Posts" + 2 categories = 3
        Assert.Equal(3, outlines.Count);

        var techOutline = outlines.FirstOrDefault(o => o.Attribute("title")?.Value == "Tech");
        Assert.NotNull(techOutline);
        Assert.Equal("rss", techOutline.Attribute("type")?.Value);
        Assert.Contains("tech", techOutline.Attribute("xmlUrl")?.Value);
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

        var result = await handler.HandleAsync(query, CancellationToken.None);
        var doc = XDocument.Parse(result);

        var body = doc.Root.Element("body");
        var outlines = body.Elements("outline").ToList();

        Assert.Single(outlines);
        Assert.Equal("All Posts", outlines[0].Attribute("title")?.Value);
    }

    [Fact]
    public async Task HandleAsync_CategoryUrlTemplateReplacement_IsCorrect()
    {
        var handler = new GetOpmlQueryHandler();
        var query = new GetOpmlQuery(CreateSampleOpmlDoc());

        var result = await handler.HandleAsync(query, CancellationToken.None);
        var doc = XDocument.Parse(result);

        var body = doc.Root.Element("body");
        var outlines = body.Elements("outline").ToList();

        var lifeOutline = outlines.FirstOrDefault(o => o.Attribute("title")?.Value == "Life");
        Assert.NotNull(lifeOutline);

        var xmlUrl = lifeOutline.Attribute("xmlUrl")?.Value;
        var htmlUrl = lifeOutline.Attribute("htmlUrl")?.Value;

        Assert.Equal("https://example.com/rss/category/life", xmlUrl);
        Assert.Equal("https://example.com/category/life", htmlUrl);
    }
}
