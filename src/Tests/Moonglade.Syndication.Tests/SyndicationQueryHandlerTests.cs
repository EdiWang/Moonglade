using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moonglade.Auth;
using Moonglade.Configuration;
using Moonglade.Data;
using Moonglade.Data.Entities;
using System.Xml.Linq;

namespace Moonglade.Syndication.Tests;

public class SyndicationQueryHandlerTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Atom_SuppliedOwnerOrMissingProfile_PreservesItemsAndDoesNotAffectRss(bool category)
    {
        using var db = await CreateFeedDatabaseAsync();
        var config = CreateBlogConfig();
        var accessor = CreateHttpContextAccessor();
        var source = new SyndicationDataSource(accessor, db);
        var handler = new GetAtomStringQueryHandler(config, source, accessor);
        var rss = new GetRssStringQueryHandler(config, source, accessor);
        var ct = TestContext.Current.CancellationToken;
        var slug = category ? "tech" : null;
        var rssBefore = await rss.HandleAsync(new GetRssStringQuery(slug), ct);
        var missing = XDocument.Parse(await handler.HandleAsync(new GetAtomStringQuery(slug), ct));
        var configured = XDocument.Parse(await handler.HandleAsync(new GetAtomStringQuery(slug,
            Owner: new OwnerProfile(Guid.NewGuid(), "public@example.test")), ct));
        XNamespace atom = "http://www.w3.org/2005/Atom";

        Assert.Equal(category ? 1 : 2, missing.Descendants(atom + "entry").Count());
        Assert.Equal(missing.Descendants(atom + "title").Select(title => title.Value),
            configured.Descendants(atom + "title").Select(title => title.Value));
        Assert.Empty(missing.Descendants(atom + "email"));
        Assert.All(missing.Descendants(atom + "entry"), entry =>
            Assert.Equal("Author", entry.Element(atom + "author")?.Element(atom + "name")?.Value));
        Assert.All(configured.Descendants(atom + "entry"), entry =>
            Assert.Equal("public@example.test", entry.Element(atom + "author")?.Element(atom + "email")?.Value));
        var firstRss = XDocument.Parse(rssBefore);
        var nextRss = XDocument.Parse(await rss.HandleAsync(new GetRssStringQuery(slug), ct));
        Assert.Equal(category ? 1 : 2, firstRss.Descendants("item").Count());
        Assert.All(firstRss.Descendants("item"), item => Assert.Null(item.Element("author")));
        firstRss.Root?.Element("channel")?.Element("pubDate")?.Remove();
        nextRss.Root?.Element("channel")?.Element("pubDate")?.Remove();
        Assert.True(XNode.DeepEquals(firstRss, nextRss));
    }

    [Fact]
    public async Task GetRssStringQuery_DataSourceReturnsNull_ReturnsNull()
    {
        using var db = await CreateFeedDatabaseAsync();
        var config = CreateBlogConfig();
        var accessor = CreateHttpContextAccessor();
        var handler = new GetRssStringQueryHandler(config, new SyndicationDataSource(accessor, db), accessor);

        var result = await handler.HandleAsync(new GetRssStringQuery("missing"), TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetRssStringQuery_PassesCategoryNameAndUsesRequestHost()
    {
        using var db = await CreateFeedDatabaseAsync();
        var config = CreateBlogConfig();
        var accessor = CreateHttpContextAccessor("https", "blog.example.com");
        var handler = new GetRssStringQueryHandler(config, new SyndicationDataSource(accessor, db), accessor);

        var result = await handler.HandleAsync(new GetRssStringQuery("tech"), TestContext.Current.CancellationToken);

        var doc = XDocument.Parse(result);
        Assert.Equal("rss", doc.Root!.Name.LocalName);
        Assert.Equal("https://blog.example.com/", doc.Root.Element("channel")?.Element("link")?.Value);
        Assert.Equal("First Post", Assert.Single(doc.Descendants("item")).Element("title")?.Value);
        Assert.DoesNotContain("Other category post", result);
    }

    [Fact]
    public async Task GetAtomStringQuery_DataSourceReturnsNull_ReturnsNull()
    {
        using var db = await CreateFeedDatabaseAsync();
        var config = CreateBlogConfig();
        var accessor = CreateHttpContextAccessor();
        var handler = new GetAtomStringQueryHandler(config, new SyndicationDataSource(accessor, db), accessor);

        var result = await handler.HandleAsync(new GetAtomStringQuery("missing"), TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAtomStringQuery_PassesSlugAndSetsAuthorEmail()
    {
        using var db = await CreateFeedDatabaseAsync();
        var blogConfig = CreateBlogConfig();
        var accessor = CreateHttpContextAccessor("https", "blog.example.com");
        var handler = new GetAtomStringQueryHandler(blogConfig, new SyndicationDataSource(accessor, db), accessor);

        var result = await handler.HandleAsync(new GetAtomStringQuery("tech", Owner: new OwnerProfile(Guid.NewGuid(), "owner@example.com")),
            TestContext.Current.CancellationToken);

        var doc = XDocument.Parse(result);
        Assert.Equal("feed", doc.Root!.Name.LocalName);
        Assert.Contains("https://blog.example.com/", result);
        XNamespace atom = "http://www.w3.org/2005/Atom";
        var entry = Assert.Single(doc.Descendants(atom + "entry"));
        Assert.Equal("First Post", entry.Element(atom + "title")?.Value);
        Assert.Contains("owner@example.com", entry.ToString());
        Assert.DoesNotContain("Other category post", result);
    }

    private static BlogConfig CreateBlogConfig()
    {
        return new BlogConfig(new BlogSettingsSnapshot
        {
            GeneralSettings = new GeneralSettings
            {
                SiteTitle = "Test Blog",
                Copyright = "© Test",
                DefaultLanguageCode = "en-us"
            }
        });
    }

    private static IHttpContextAccessor CreateHttpContextAccessor(string scheme = "https", string host = "example.com")
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = scheme;
        context.Request.Host = new HostString(host);
        return new HttpContextAccessor { HttpContext = context };
    }

    private static async Task<BlogDbContext> CreateFeedDatabaseAsync()
    {
        var options = new DbContextOptionsBuilder<BlogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new BlogDbContext(options);
        var category = new CategoryEntity { Id = 100, Slug = "tech", DisplayName = "Tech" };
        var publishDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var post = new PostEntity
        {
            Id = Guid.NewGuid(),
            Title = "First Post",
            Slug = "first-post",
            Author = "Author",
            PostContent = "Description",
            ContentAbstract = "Description",
            ContentType = "html",
            ContentLanguageCode = "en-us",
            CreateTimeUtc = publishDate,
            PubDateUtc = publishDate,
            RouteLink = "2025/1/1/first-post",
            PostStatus = PostStatus.Published,
            IsFeedIncluded = true
        };
        post.PostCategory.Add(new PostCategoryEntity { PostId = post.Id, CategoryId = category.Id, Category = category });
        db.Post.Add(post);
        db.Post.Add(new PostEntity
        {
            Id = Guid.NewGuid(),
            Title = "Other category post",
            Slug = "other-post",
            Author = "Author",
            PostContent = "Other",
            ContentAbstract = "Other",
            ContentType = "html",
            ContentLanguageCode = "en-us",
            CreateTimeUtc = publishDate,
            PubDateUtc = publishDate,
            RouteLink = "2025/1/1/other-post",
            PostStatus = PostStatus.Published,
            IsFeedIncluded = true
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return db;
    }
}
