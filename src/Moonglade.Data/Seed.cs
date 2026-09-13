using Microsoft.Extensions.Logging;
using Moonglade.Data.Entities;
using System.Globalization;

namespace Moonglade.Data;

public class Seed
{
    public static async Task SeedAsync(BlogDbContext dbContext, ILogger logger, int retry = 0)
    {
        var retryForAvailability = retry;

        try
        {
            logger.LogDebug("Adding categories data...");
            await dbContext.Category.AddRangeAsync(GetCategories());

            logger.LogDebug("Adding tags data...");
            await dbContext.Tag.AddRangeAsync(GetTags());

            logger.LogDebug("Adding widgets data...");
            await dbContext.Widget.AddRangeAsync(GetWidgets());

            logger.LogDebug("Adding pages data...");
            await dbContext.BlogPage.AddRangeAsync(GetPages());

            logger.LogDebug("Adding example post...");
            // Add example post
            var content = """
                <p dir="auto"><strong>Moonglade</strong> is a self-hosted personal blogging platform built for developers. It includes essential blogging tools such as posts, pages, comments, categories, tags, archives, themes, feeds, and an administration portal.</p>
                <h2>Getting Started</h2>
                <ul dir="auto">
                <li>Sign in to the administration portal at <code>/admin</code>.</li>
                <li>Review the settings for your site title, appearance, comments, images, and notifications.</li>
                <li>Update or remove this welcome post.</li>
                <li>Create your first post and publish it when you are ready.</li>
                </ul>
                <h2>Deployment</h2>
                <ul dir="auto">
                <li>Use a stable release from the <a href="https://github.com/EdiWang/Moonglade/releases" rel="nofollow">Moonglade releases page</a>.</li>
                <li>Enable HTTPS and HTTP/2 on your web server or reverse proxy.</li>
                <li>Keep the database and both image-storage directories on durable storage.</li>
                <li>Store connection strings, passwords, and other secrets outside source control.</li>
                <li>Use <code>/health</code> for liveness checks and <code>/health/ready</code> for database readiness.</li>
                <li>Moonglade can run in containers, on virtual machines, on-premises, or on any cloud platform that supports ASP.NET Core.</li>
                </ul>
                <h2>Documentation</h2>
                <p dir="auto">Configuration and deployment guidance is available in the <a href="https://github.com/EdiWang/Moonglade" rel="nofollow">project README</a>.</p>
                <blockquote><p>Moonglade must not be used to serve users in mainland China or to publish content prohibited by Chinese law or any applicable regulations.</p></blockquote>
                """;

            var post = new PostEntity
            {
                Id = Guid.NewGuid(),
                Title = "Welcome to Moonglade",
                Slug = "welcome-to-moonglade",
                Author = "admin",
                PostContent = content,
                CommentEnabled = true,
                CreateTimeUtc = DateTime.UtcNow,
                ContentAbstract = "Moonglade is a self-hosted personal blogging platform for developers, with support for posts, pages, comments, categories, tags, archives, themes, feeds, and more.",
                PostStatus = PostStatus.Published,
                IsFeatured = true,
                IsFeedIncluded = true,
                LastModifiedUtc = DateTime.UtcNow,
                PubDateUtc = DateTime.UtcNow,
                ContentLanguageCode = "en-us",
                Tags = [.. dbContext.Tag],
                PostCategory = [.. dbContext.PostCategory],
                RouteLink = $"{DateTime.UtcNow.ToString("yyyy/M/d", CultureInfo.InvariantCulture)}/welcome-to-moonglade"
            };

            await dbContext.Post.AddAsync(post);
            await dbContext.SaveChangesAsync();
        }
        catch (Exception e)
        {
            if (retryForAvailability >= 5) throw;

            retryForAvailability++;

            logger.LogError(e.Message);
            await SeedAsync(dbContext, logger, retryForAvailability);
            throw;
        }
    }

    private static IEnumerable<CategoryEntity> GetCategories() =>
        [
            new()
            {
                Id = Guid.Parse("b0c15707-dfc8-4b09-9aa0-5bfca744c50b"),
                DisplayName = "Default",
                Note = "Default Category",
                Slug = "default"
            }
        ];

    private static IEnumerable<TagEntity> GetTags() =>
        [
            new() { DisplayName = "Moonglade", NormalizedName = "moonglade" },
            new() { DisplayName = ".NET", NormalizedName = "dot-net" }
        ];

    private static IEnumerable<WidgetEntity> GetWidgets() =>
        [
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Friend Links",
                WidgetType = WidgetType.LinkList,
                ContentCode = "[{\"name\": \"Edi Wang\", \"url\": \"https://edi.wang\", \"openInNewTab\": true, \"order\": 0}]",
                ContentType = WidgetContentType.JSON,
                CreatedTimeUtc = DateTime.UtcNow,
                DisplayOrder = 0,
                IsEnabled = true
            }
        ];

    private static IEnumerable<PageEntity> GetPages() =>
        [
            new()
            {
                Id = Guid.NewGuid(),
                Title = "About",
                Slug = "about",
                MetaDescription = "An Empty About Page",
                HtmlContent = "<h3>An Empty About Page</h3>",
                HideSidebar = true,
                IsPublished = true,
                CreateTimeUtc = DateTime.UtcNow,
                UpdateTimeUtc = DateTime.UtcNow
            }
        ];
}
