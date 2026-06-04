using Microsoft.AspNetCore.Http;
using Moonglade.Configuration;
using Moonglade.Data.DTO;
using Moq;
using System.Xml.Linq;

namespace Moonglade.Syndication.Tests;

public class SyndicationQueryHandlerTests
{
    [Fact]
    public async Task GetRssStringQuery_DataSourceReturnsNull_ReturnsNull()
    {
        var dataSource = new Mock<ISyndicationDataSource>();
        dataSource.Setup(x => x.GetFeedDataAsync("missing")).ReturnsAsync((List<FeedEntry>)null!);
        var handler = new GetRssStringQueryHandler(CreateBlogConfig(), dataSource.Object, CreateHttpContextAccessor());

        var result = await handler.HandleAsync(new GetRssStringQuery("missing"), TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetRssStringQuery_PassesCategoryNameAndUsesRequestHost()
    {
        var dataSource = new Mock<ISyndicationDataSource>();
        dataSource.Setup(x => x.GetFeedDataAsync("tech")).ReturnsAsync(CreateFeedEntries());
        var handler = new GetRssStringQueryHandler(CreateBlogConfig(), dataSource.Object, CreateHttpContextAccessor("https", "blog.example.com"));

        var result = await handler.HandleAsync(new GetRssStringQuery("tech"), TestContext.Current.CancellationToken);

        dataSource.Verify(x => x.GetFeedDataAsync("tech"), Times.Once);
        var doc = XDocument.Parse(result);
        Assert.Equal("rss", doc.Root!.Name.LocalName);
        Assert.Equal("https://blog.example.com/", doc.Root.Element("channel")?.Element("link")?.Value);
    }

    [Fact]
    public async Task GetAtomStringQuery_DataSourceReturnsNull_ReturnsNull()
    {
        var dataSource = new Mock<ISyndicationDataSource>();
        dataSource.Setup(x => x.GetFeedDataAsync("missing")).ReturnsAsync((List<FeedEntry>)null!);
        var handler = new GetAtomStringQueryHandler(CreateBlogConfig(), dataSource.Object, CreateHttpContextAccessor());

        var result = await handler.HandleAsync(new GetAtomStringQuery("missing"), TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAtomStringQuery_PassesSlugAndSetsAuthorEmail()
    {
        var feedEntries = CreateFeedEntries();
        var dataSource = new Mock<ISyndicationDataSource>();
        dataSource.Setup(x => x.GetFeedDataAsync("tech")).ReturnsAsync(feedEntries);
        var blogConfig = CreateBlogConfig();
        blogConfig.GeneralSettings.OwnerEmail = "owner@example.com";
        var handler = new GetAtomStringQueryHandler(blogConfig, dataSource.Object, CreateHttpContextAccessor("https", "blog.example.com"));

        var result = await handler.HandleAsync(new GetAtomStringQuery("tech"), TestContext.Current.CancellationToken);

        dataSource.Verify(x => x.GetFeedDataAsync("tech"), Times.Once);
        Assert.All(feedEntries, item => Assert.Equal("owner@example.com", item.AuthorEmail));
        var doc = XDocument.Parse(result);
        Assert.Equal("feed", doc.Root!.Name.LocalName);
        Assert.Contains("https://blog.example.com/", result);
    }

    private static BlogConfig CreateBlogConfig()
    {
        return new BlogConfig
        {
            GeneralSettings = new GeneralSettings
            {
                SiteTitle = "Test Blog",
                Description = "Test Description",
                Copyright = "© Test",
                DefaultLanguageCode = "en-us",
                OwnerEmail = "test@example.com"
            }
        };
    }

    private static IHttpContextAccessor CreateHttpContextAccessor(string scheme = "https", string host = "example.com")
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = scheme;
        context.Request.Host = new HostString(host);
        return new HttpContextAccessor { HttpContext = context };
    }

    private static List<FeedEntry> CreateFeedEntries()
    {
        return
        [
            new()
            {
                Id = Guid.NewGuid().ToString(),
                Title = "First Post",
                Description = "Description",
                PubDateUtc = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                Link = "https://blog.example.com/post/first-post",
                Author = "Author",
                LangCode = "en-us",
                Categories = ["Tech"],
                ContentType = "html"
            }
        ];
    }
}
