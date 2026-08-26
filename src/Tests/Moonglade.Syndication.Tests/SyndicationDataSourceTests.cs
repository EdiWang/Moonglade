using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moonglade.Configuration;
using Moonglade.Data;
using Moonglade.Data.Entities;

namespace Moonglade.Syndication.Tests;

public class SyndicationDataSourceTests
{
    [Theory]
    [InlineData("html", "<p><img src=\"/image/photo.png\" alt=\"Photo\"></p>")]
    [InlineData("markdown", "![Photo](/image/photo.png)")]
    public async Task GetFeedDataAsync_WhenCdnIsEnabled_EmitsDirectCdnImageUrl(string contentType, string content)
    {
        var options = new DbContextOptionsBuilder<BlogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new BlogDbContext(options);
        db.Post.Add(new PostEntity
        {
            Id = Guid.NewGuid(),
            Title = "CDN post",
            Slug = "cdn-post",
            Author = "Author",
            PostContent = content,
            ContentAbstract = content,
            ContentLanguageCode = "en-us",
            ContentType = contentType,
            CreateTimeUtc = new DateTime(2026, 8, 23, 0, 0, 0, DateTimeKind.Utc),
            PubDateUtc = new DateTime(2026, 8, 23, 1, 0, 0, DateTimeKind.Utc),
            IsFeedIncluded = true,
            PostStatus = PostStatus.Published,
            RouteLink = "2026/08/23/cdn-post"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var blogConfig = new BlogConfig
        {
            FeedSettings = new FeedSettings { UseFullContent = true },
            ImageSettings = new ImageSettings
            {
                EnableCDNRedirect = true,
                CDNEndpoint = "https://cdn.example.com/images"
            }
        };
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("blog.example.com");
        var dataSource = new SyndicationDataSource(
            blogConfig,
            new HttpContextAccessor { HttpContext = httpContext },
            db);

        var entries = await dataSource.GetFeedDataAsync();

        var entry = Assert.Single(entries);
        Assert.Contains("src=\"https://cdn.example.com/images/photo.png\"", entry.Description, StringComparison.Ordinal);
        Assert.DoesNotContain("src=\"/image/photo.png\"", entry.Description, StringComparison.Ordinal);
    }
}
