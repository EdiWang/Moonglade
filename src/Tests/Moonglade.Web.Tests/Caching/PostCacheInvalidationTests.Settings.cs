using LiteBus.Commands.Abstractions;
using LiteBus.Queries.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moonglade.BackgroundServices;
using Moonglade.Configuration;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Theme;
using Moonglade.Web.Handlers;
using System.Xml.Linq;

namespace Moonglade.Web.Tests.Caching;

public partial class PostCacheInvalidationTests
{
    [Fact]
    public async Task EnablingScheduler_WithRealQuery_PublishesOverduePostWithoutPollingDelay()
    {
        var ct = TestContext.Current.CancellationToken;
        var publication = new ScheduledPublicationSignal();
        using var environment = await TestEnvironment.CreateAsync(publication);
        await SaveSetting(environment, new AdvancedSettings { EnablePostScheduler = false });
        using (var scope = environment.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();
            var post = await db.Post.SingleAsync(ct);
            post.PostStatus = PostStatus.Scheduled;
            post.PubDateUtc = null;
            post.RouteLink = null;
            post.ScheduledPublishTimeUtc = DateTime.UtcNow.AddMinutes(-1);
            await db.SaveChangesAsync(ct);
        }
        publication.Enabled = true;
        using var worker = new ScheduledPublishService(environment.Services, NullLogger<ScheduledPublishService>.Instance,
            environment.Services.GetRequiredService<ScheduledPublishWakeUp>(), environment.Services.GetRequiredService<IBlogConfig>());
        await worker.StartAsync(ct);
        try
        {
            await SaveSetting(environment, new AdvancedSettings { EnablePostScheduler = true });
            await publication.Published.Task.WaitAsync(TimeSpan.FromSeconds(10), ct);
        }
        finally
        {
            await worker.StopAsync(ct);
        }
        using var verification = environment.Services.CreateScope();
        var saved = await verification.ServiceProvider.GetRequiredService<BlogDbContext>().Post.SingleAsync(ct);
        Assert.Equal(PostStatus.Published, saved.PostStatus);
        Assert.NotNull(saved.PubDateUtc);
    }

    [Fact]
    public async Task SitemapCache_UsesCapturedCanonicalPrefixBeforePostCommitInvalidation()
    {
        using var environment = await TestEnvironment.CreateAsync();
        using var scope = environment.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();
        var owner = environment.Services.GetRequiredService<BlogConfig>();
        var http = new DefaultHttpContext();
        http.Request.Scheme = "https";
        http.Request.Host = new HostString("before.example.com");
        owner.Publish(owner.Capture() with
        {
            SEOSettings = owner.Capture().SEOSettings with { CanonicalPrefix = "https://before.example.com" }
        });
        var first = Assert.IsType<ContentHttpResult>(await SiteMapMapHandler.Handle(http, owner, environment.Cache, environment.Invalidator, db));
        var generation = environment.Invalidator.SiteMapCacheKey;

        owner.Publish(owner.Capture() with
        {
            SEOSettings = owner.Capture().SEOSettings with { CanonicalPrefix = "https://after.example.com" }
        });
        var next = Assert.IsType<ContentHttpResult>(await SiteMapMapHandler.Handle(http, owner, environment.Cache, environment.Invalidator, db));

        Assert.Equal(generation, environment.Invalidator.SiteMapCacheKey);
        Assert.Contains("before.example.com", first.ResponseContent);
        Assert.Contains("after.example.com", next.ResponseContent);
        Assert.DoesNotContain("before.example.com", next.ResponseContent);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FeedCache_UsesCapturedSettingsBeforePostCommitInvalidation(bool atom)
    {
        using var environment = await TestEnvironment.CreateAsync();
        var before = await environment.ReadFeedAsync(atom, null);
        Assert.Contains("Old abstract", before);
        var owner = environment.Services.GetRequiredService<BlogConfig>();
        var generation = environment.Invalidator.GetFeedCacheKey("rss");

        owner.Publish(owner.Capture() with
        {
            GeneralSettings = owner.Capture().GeneralSettings with { SiteTitle = "New metadata" },
            FeedSettings = new FeedSettings { UseFullContent = true }
        });
        var after = await environment.ReadFeedAsync(atom, null);

        Assert.Equal(generation, environment.Invalidator.GetFeedCacheKey("rss"));
        Assert.Contains("New metadata", after);
        Assert.Contains("Old content", after);
        Assert.DoesNotContain("Old abstract", after);
    }

    [Theory]
    [InlineData(false, null)]
    [InlineData(true, null)]
    [InlineData(false, "tech")]
    [InlineData(true, "tech")]
    public async Task SettingsSave_RefreshesWarmFeedsWithCountAndRealFullContent(bool atom, string? category)
    {
        using var environment = await TestEnvironment.CreateAsync();
        using (var scope = environment.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();
            var first = await db.Post.SingleAsync(TestContext.Current.CancellationToken);
            first.PostContent = "<p>Full body marker</p>";
            var second = TestEnvironment.CreatePost(Guid.NewGuid(), environment.PublishDateUtc.AddDays(-1));
            second.Slug = "second-post";
            second.RouteLink = "2026/01/01/second-post";
            second.PostCategory.Add(new PostCategoryEntity { CategoryId = environment.CategoryId, PostId = second.Id });
            db.Post.Add(second);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        var before = await environment.ReadFeedAsync(atom, category);
        Assert.Equal(2, FeedCount(before, atom));
        Assert.DoesNotContain("Full body marker", before);
        var postKey = environment.Invalidator.GetCacheKey("probe");
        var sitemapKey = environment.Invalidator.SiteMapCacheKey;

        await SaveSetting(environment, new FeedSettings { FeedItemCount = 1, UseFullContent = true });

        var after = await environment.ReadFeedAsync(atom, category);
        Assert.Single(XDocument.Parse(after).Descendants(), element => element.Name.LocalName == (atom ? "entry" : "item"));
        Assert.Contains("Full body marker", after);
        Assert.Equal(postKey, environment.Invalidator.GetCacheKey("probe"));
        Assert.Equal(sitemapKey, environment.Invalidator.SiteMapCacheKey);
    }

    [Fact]
    public async Task GeneralImageAndSeoSaves_RefreshWarmFeedMetadataSitemapAndCdnContent()
    {
        using var environment = await TestEnvironment.CreateAsync();
        using (var scope = environment.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();
            (await db.Post.SingleAsync(TestContext.Current.CancellationToken)).PostContent = "<img src=\"/image/photo.png\">";
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        await SaveSetting(environment, new FeedSettings { UseFullContent = true });
        foreach (var atom in new[] { false, true })
            foreach (var category in new string?[] { null, "tech" })
                Assert.Contains("/image/photo.png", await environment.ReadFeedAsync(atom, category));
        using var readScope = environment.Services.CreateScope();
        var readDb = readScope.ServiceProvider.GetRequiredService<BlogDbContext>();
        var config = environment.Services.GetRequiredService<IBlogConfig>();
        var context = new DefaultHttpContext();
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("blog.example.com");
        var sitemapBefore = Assert.IsType<ContentHttpResult>(await SiteMapMapHandler.Handle(context, config, environment.Cache, environment.Invalidator, readDb));
        Assert.Contains("/post/", sitemapBefore.ResponseContent);
        Assert.DoesNotContain("canonical.example.com", sitemapBefore.ResponseContent);
        await SaveSetting(environment, config.Capture().GeneralSettings with
        {
            SiteTitle = "Updated blog"
        });
        foreach (var atom in new[] { false, true })
        {
            var feed = await environment.ReadFeedAsync(atom, null);
            Assert.Contains("Updated blog", feed);
            Assert.Contains("/image/photo.png", feed);
            Assert.DoesNotContain("https://cdn.example.com/photo.png", feed);
        }
        await SaveSetting(environment, ImageSettings.DefaultValue with
        {
            EnableCDNRedirect = true,
            CDNEndpoint = "https://cdn.example.com"
        });

        Assert.Equal("3:rss", environment.Invalidator.GetFeedCacheKey("rss"));
        foreach (var atom in new[] { false, true })
            foreach (var category in new string?[] { null, "tech" })
            {
                var feed = await environment.ReadFeedAsync(atom, category);
                Assert.Contains("Updated blog", feed);
                Assert.Contains("https://cdn.example.com/photo.png", feed);
                if (atom) Assert.Contains("owner@example.test", feed);
            }
        var sitemapAfter = Assert.IsType<ContentHttpResult>(await SiteMapMapHandler.Handle(context, config, environment.Cache, environment.Invalidator, readDb));
        Assert.DoesNotContain("canonical.example.com", sitemapAfter.ResponseContent);
        await SaveSetting(environment, config.Capture().SEOSettings with { CanonicalPrefix = "https://canonical.example.com" });
        sitemapAfter = Assert.IsType<ContentHttpResult>(await SiteMapMapHandler.Handle(context, config, environment.Cache, environment.Invalidator, readDb));
        Assert.Contains("https://canonical.example.com/post/", sitemapAfter.ResponseContent);
        Assert.DoesNotContain("blog.example.com", sitemapAfter.ResponseContent);
    }

    [Fact]
    public async Task AppearanceSave_RefreshesThemeWhileOtherSettingsPreserveIt()
    {
        using var environment = await TestEnvironment.CreateAsync();
        var themes = environment.Services.GetRequiredService<ThemeCacheInvalidator>();
        var before = await ReadTheme(environment);
        var postKey = environment.Invalidator.GetCacheKey("probe");
        var feedKey = environment.Invalidator.GetFeedCacheKey("rss");
        await SaveSetting(environment, new AppearanceSettings { ThemeId = 101 });
        Assert.NotEqual(before, await ReadTheme(environment));
        var themeKey = themes.CacheKey;
        await SaveSetting(environment, new CommentSettings { EnableComments = false });
        Assert.Equal(themeKey, themes.CacheKey);
        Assert.Equal(postKey, environment.Invalidator.GetCacheKey("probe"));
        Assert.Equal(feedKey, environment.Invalidator.GetFeedCacheKey("rss"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SettingsSave_OldInFlightFeedOrThemeCannotRefillActiveGeneration(bool theme)
    {
        using var environment = await TestEnvironment.CreateAsync();
        var themes = environment.Services.GetRequiredService<ThemeCacheInvalidator>();
        string Key() => theme
            ? themes.GetCacheKey(environment.Services.GetRequiredService<IBlogConfig>().Capture().AppearanceSettings.ThemeId)
            : environment.CacheKey("General", "rss");
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var finish = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var old = environment.Cache.GetOrCreateAsync("General", Key(), () =>
        {
            started.SetResult();
            return finish.Task;
        });
        await started.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        try
        {
            if (theme) await SaveSetting(environment, new AppearanceSettings { ThemeId = 101 });
            else await SaveSetting(environment, new FeedSettings { FeedItemCount = 1 });
        }
        finally
        {
            finish.TrySetResult("stale");
        }
        Assert.Equal("stale", await old);
        Assert.Equal("fresh", await environment.Cache.GetOrCreateAsync("General", Key(), () => Task.FromResult("fresh")));
    }

    [Fact]
    public async Task AdvancedSave_WakesSchedulerAndLeavesEndpointAndContentCachesAlone()
    {
        using var environment = await TestEnvironment.CreateAsync();
        var wakeUp = environment.Services.GetRequiredService<ScheduledPublishWakeUp>();
        var token = wakeUp.GetWakeToken();
        var feedKey = environment.Invalidator.GetFeedCacheKey("rss");
        var sitemapKey = environment.Invalidator.SiteMapCacheKey;
        await SaveSetting(environment, new AdvancedSettings { EnablePostScheduler = false, EnableFoaf = true });
        Assert.True(token.IsCancellationRequested);
        Assert.Equal(feedKey, environment.Invalidator.GetFeedCacheKey("rss"));
        Assert.Equal(sitemapKey, environment.Invalidator.SiteMapCacheKey);
    }

    [Fact]
    public async Task SettingsSaveFailure_DoesNotInvalidateWarmOutputs()
    {
        var failure = new SettingsSaveFailure();
        using var environment = await TestEnvironment.CreateAsync(failure);
        var before = await environment.ReadFeedAsync(false, null);
        var key = environment.Invalidator.GetFeedCacheKey("rss");
        failure.Enabled = true;
        await Assert.ThrowsAsync<DbUpdateException>(() => SaveSetting(environment, new FeedSettings { FeedItemCount = 1 }));
        Assert.Equal(key, environment.Invalidator.GetFeedCacheKey("rss"));
        Assert.Equal(before, await environment.ReadFeedAsync(false, null));
    }

    private static int FeedCount(string xml, bool atom) => XDocument.Parse(xml).Descendants()
        .Count(element => element.Name.LocalName == (atom ? "entry" : "item"));



    [Fact]
    public async Task SettingsCancellationAfterCommit_StillInvalidates()
    {
        using var cancellation = new CancellationTokenSource();
        var interceptor = new CancelAfterSaveInterceptor(cancellation);
        using var environment = await TestEnvironment.CreateAsync(interceptor);
        var key = environment.Invalidator.GetFeedCacheKey("rss");
        interceptor.Enabled = true;
        using var scope = environment.Services.CreateScope();

        await scope.ServiceProvider.GetRequiredService<ICommandMediator>()
            .SendAsync(UpdateConfigurationCommand.Create(new FeedSettings { FeedItemCount = 1 }), cancellation.Token);

        Assert.True(cancellation.IsCancellationRequested);
        Assert.NotEqual(key, environment.Invalidator.GetFeedCacheKey("rss"));
    }

    private static async Task SaveSetting<T>(TestEnvironment environment, T value) where T : IBlogSettings<T>
    {
        using var scope = environment.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<ICommandMediator>()
            .SendAsync(UpdateConfigurationCommand.Create(value), TestContext.Current.CancellationToken);
    }

    private static async Task<string> ReadTheme(TestEnvironment environment)
    {
        using var scope = environment.Services.CreateScope();
        var settings = environment.Services.GetRequiredService<IBlogConfig>().Capture();
        return Assert.IsType<string>(await environment.Cache.GetOrCreateAsync("General", environment.Services.GetRequiredService<ThemeCacheInvalidator>().GetCacheKey(settings.AppearanceSettings.ThemeId),
            () => scope.ServiceProvider.GetRequiredService<IQueryMediator>()
                .QueryAsync(new GetSiteThemeStyleSheetQuery(settings.AppearanceSettings.ThemeId), TestContext.Current.CancellationToken), TimeSpan.FromMinutes(20)));
    }

    private sealed class SettingsSaveFailure : SaveChangesInterceptor
    {
        public bool Enabled { get; set; }
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default) =>
            Enabled ? throw new DbUpdateException("Injected settings save failure.") : ValueTask.FromResult(result);
    }

    private sealed class ScheduledPublicationSignal : SaveChangesInterceptor
    {
        public bool Enabled { get; set; }
        public TaskCompletionSource Published { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result,
            CancellationToken cancellationToken = default)
        {
            if (Enabled && eventData.Context!.ChangeTracker.Entries<PostEntity>().Any(entry => entry.Entity.PostStatus == PostStatus.Published))
                Published.TrySetResult();
            return ValueTask.FromResult(result);
        }
    }
}
