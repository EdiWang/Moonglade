using Microsoft.Extensions.Logging;
using Moonglade.Data.Entities;

namespace Moonglade.Data;

public class Seed
{
    public static async Task SeedAsync(BlogDbContext dbContext, ILogger logger, int retry = 0)
    {
        var retryForAvailability = retry;

        try
        {
            logger.LogDebug("Adding themes data...");
            await dbContext.BlogTheme.AddRangeAsync(GetThemes());

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
                IsPublished = true,
                IsFeatured = true,
                IsFeedIncluded = true,
                LastModifiedUtc = DateTime.UtcNow,
                PubDateUtc = DateTime.UtcNow,
                ContentLanguageCode = "en-us",
                Tags = dbContext.Tag.ToList(),
                PostCategory = dbContext.PostCategory.ToList(),
                RouteLink = $"{DateTime.UtcNow:yyyy/M/d}/welcome-to-moonglade"
            };

            await dbContext.Post.AddAsync(post);
            await dbContext.SaveChangesAsync();
        }
        catch (Exception e)
        {
            if (retryForAvailability >= 10) throw;

            retryForAvailability++;

            logger.LogError(e.Message);
            await SeedAsync(dbContext, logger, retryForAvailability);
            throw;
        }
    }

    private static IEnumerable<BlogThemeEntity> GetThemes() =>
        new List<BlogThemeEntity>
        {
            new ()
            {
                ThemeName = "Word Blue", CssRules = "{\"--accent-color1\": \"#2a579a\",\"--accent-color2\": \"#1a365f\",\"--accent-color3\": \"#3e6db5\"}", ThemeType = 0
            },
            new ()
            {
                ThemeName = "Excel Green", CssRules = "{\"--accent-color1\": \"#165331\",\"--accent-color2\": \"#0E351F\",\"--accent-color3\": \"#0E703A\"}", ThemeType = 0
            },
            new ()
            {
                ThemeName = "PowerPoint Orange", CssRules = "{\"--accent-color1\": \"#983B22\",\"--accent-color2\": \"#622616\",\"--accent-color3\": \"#C43E1C\"}", ThemeType = 0
            },
            new ()
            {
                ThemeName = "OneNote Purple", CssRules = "{\"--accent-color1\": \"#663276\",\"--accent-color2\": \"#52285E\",\"--accent-color3\": \"#7719AA\"}", ThemeType = 0
            },
            new ()
            {
                ThemeName = "Outlook Blue", CssRules = "{\"--accent-color1\": \"#035AA6\",\"--accent-color2\": \"#032B51\",\"--accent-color3\": \"#006CBF\"}", ThemeType = 0
            },
            new ()
            {
                ThemeName = "Indian Curry", CssRules = "{\"--accent-color1\": \"rgb(128 84 3)\",\"--accent-color2\": \"rgb(95 62 0)\",\"--accent-color3\": \"rgb(208 142 19)\"}", ThemeType = 0
            },
            new ()
            {
                ThemeName = "Metal Blue", CssRules = "{\"--accent-color1\": \"#4E5967\",\"--accent-color2\": \"#333942\",\"--accent-color3\": \"#6e7c8e\"}", ThemeType = 0
            }
        };

    private static IEnumerable<CategoryEntity> GetCategories() =>
        new List<CategoryEntity>
        {
            new()
            {
                Id = Guid.Parse("b0c15707-dfc8-4b09-9aa0-5bfca744c50b"),
                DisplayName = "Default",
                Note = "Default Category",
                Slug = "default"
            }
        };

    private static IEnumerable<TagEntity> GetTags() =>
        new List<TagEntity>
        {
            new() { DisplayName = "Moonglade", NormalizedName = "moonglade" },
            new() { DisplayName = ".NET", NormalizedName = "dot-net" }
        };

    private static IEnumerable<FriendLinkEntity> GetFriendLinks() =>
        new List<FriendLinkEntity>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Edi.Wang",
                LinkUrl = "https://edi.wang"
            }
        };

    private static IEnumerable<PageEntity> GetPages() =>
        new List<PageEntity>
        {
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
        };
}