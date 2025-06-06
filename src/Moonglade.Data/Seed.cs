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

            logger.LogDebug("Adding friend links data...");
            await dbContext.FriendLink.AddRangeAsync(GetFriendLinks());

            logger.LogDebug("Adding pages data...");
            await dbContext.CustomPage.AddRangeAsync(GetPages());

            logger.LogDebug("Adding example post...");
            // Add example post
            var content = "Moonglade is the blog system for https://edi.wang. Powered by .NET 8 and runs on Microsoft Azure, the best cloud on the planet.";

            var post = new PostEntity
            {
                Id = Guid.NewGuid(),
                Title = "Welcome to Moonglade",
                Slug = "welcome-to-moonglade",
                Author = "admin",
                PostContent = content,
                CommentEnabled = true,
                CreateTimeUtc = DateTime.UtcNow,
                ContentAbstract = content,
                PostStatus = PostStatusConstants.Published,
                IsFeatured = true,
                IsFeedIncluded = true,
                LastModifiedUtc = DateTime.UtcNow,
                PubDateUtc = DateTime.UtcNow,
                ContentLanguageCode = "en-us",
                Tags = dbContext.Tag.ToList(),
                PostCategory = dbContext.PostCategory.ToList(),
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

    private static IEnumerable<FriendLinkEntity> GetFriendLinks() =>
        [
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Edi.Wang",
                LinkUrl = "https://edi.wang"
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