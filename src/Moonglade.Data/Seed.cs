using Microsoft.Extensions.Logging;
using Moonglade.Data.DTO;
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
            await dbContext.CustomPage.AddRangeAsync(GetPages());

            logger.LogDebug("Adding example post...");
            // Add example post
            var content = "<p dir=\"auto\"><strong>Moonglade</strong> is a personal blogging platform built for developers, optimized for seamless deployment on <a href=\"https://azure.microsoft.com/en-us/\" rel=\"nofollow\"><strong>Microsoft Azure</strong></a>. It features essential blogging tools: posts, comments, categories, tags, archives, and pages.</p><h2>🚀 Deployment</h2><hr /><ul dir=\"auto\">\r\n<li><strong>Stable Code:</strong> Always use the <a href=\"https://github.com/EdiWang/Moonglade/releases\">Release</a> branch. Avoid deploying from <code>master</code>.</li>\r\n<li><strong>Security:</strong> Enable <strong>HTTPS</strong> and <strong>HTTP/2</strong> on your web server for optimal security and performance.</li>\r\n<li><strong>Deployment Options:</strong> While Azure is recommended, Moonglade can run on any cloud provider or on-premises.</li>\r\n<li><strong>China Regulation:</strong> In China, Moonglade runs in <strong>read-only</strong> mode due to local regulations. If you are in China, please consider alternative platforms.</li>\r\n</ul><h3>Full Azure Deployment</h3><p dir=\"auto\">This mirrors how <a href=\"https://edi.wang\" rel=\"nofollow\">edi.wang</a> is deployed, utilizing a variety of Azure services for maximum speed and security. <strong>No automated script is provided</strong>—manual resource creation is required.</p><p><img src=\"https://camo.githubusercontent.com/7962a0a9554e8f5effa92383b175acc0c723b603ac19432f73984ecfff4450c4/68747470733a2f2f63646e2e6564692e77616e672f7765622d6173736574732f65646977616e672d617a7572652d617263682d766973696f2d6f6374323032342e737667\" alt=\"Azure Architecture\" data-canonical-src=\"https://cdn.edi.wang/web-assets/ediwang-azure-arch-visio-oct2024.svg\" style=\"max-width: 100%;\"></p><h3>Quick Azure Deploy (App Service on Linux)</h3><p dir=\"auto\">Get started in 10 minutes with minimal Azure resources using our <a href=\"https://github.com/EdiWang/Moonglade/wiki/Quick-Deploy-on-Azure\">automated deployment script</a>.</p>";

            var post = new PostEntity
            {
                Id = Guid.NewGuid(),
                Title = "Welcome to Moonglade",
                Slug = "welcome-to-moonglade",
                Author = "admin",
                PostContent = content,
                CommentEnabled = true,
                CreateTimeUtc = DateTime.UtcNow,
                ContentAbstract = "Moonglade is a personal blogging platform built for developers, optimized for seamless deployment on Microsoft Azure. It features essential blogging tools: posts, comments, categories, tags, archives, and pages.",
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