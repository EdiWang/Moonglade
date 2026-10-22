using Edi.CacheAside.InMemory;
using LiteBus.Commands;
using LiteBus.Commands.Abstractions;
using LiteBus.Events;
using LiteBus.Extensions.Microsoft.DependencyInjection;
using LiteBus.Messaging;
using LiteBus.Queries;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moonglade.Configuration;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Features.Category;
using Moonglade.Features.Post;
using Moonglade.Features.Tag;
using Moonglade.Syndication;
using Moonglade.Utils;
using Moonglade.Web.Controllers;
using Moonglade.Web.Filters;
using Moonglade.Web.Handlers;
using Moonglade.Web.Pages;

namespace Moonglade.Web.Tests;

public class PostCacheInvalidationTests
{
    [Theory]
    [InlineData("create")]
    [InlineData("update")]
    [InlineData("publish")]
    [InlineData("unpublish")]
    [InlineData("recycle")]
    [InlineData("restore")]
    [InlineData("delete")]
    [InlineData("empty")]
    [InlineData("cancel")]
    [InlineData("postpone")]
    [InlineData("scheduled-publish")]
    [InlineData("category-update")]
    [InlineData("category-delete")]
    [InlineData("tag-update")]
    [InlineData("tag-delete")]
    public async Task SuccessfulWrite_InvalidatesAllPostCachesAndPreservesUnrelatedCaches(string operation)
    {
        using var environment = await TestEnvironment.CreateAsync();
        using var scope = environment.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();
        var post = await db.Post.SingleAsync(TestContext.Current.CancellationToken);
        if (operation is "cancel" or "postpone" or "scheduled-publish")
        {
            post.PostStatus = PostStatus.Scheduled;
            post.ScheduledPublishTimeUtc = DateTime.UtcNow.AddMinutes(-1);
        }
        post.IsDeleted = operation is "restore" or "empty";
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var keys = environment.PrimeCaches();
        var mediator = scope.ServiceProvider.GetRequiredService<ICommandMediator>();
        var ct = TestContext.Current.CancellationToken;

        Task write = operation switch
        {
            "create" => mediator.SendAsync(new CreatePostCommand(environment.EditModel()), ct),
            "update" => mediator.SendAsync(new UpdatePostCommand(post.Id, environment.EditModel()), ct),
            "publish" => mediator.SendAsync(new PublishPostCommand(post.Id), ct),
            "unpublish" => mediator.SendAsync(new UnpublishPostCommand(post.Id), ct),
            "recycle" => mediator.SendAsync(new DeletePostCommand(post.Id, SoftDelete: true), ct),
            "restore" => mediator.SendAsync(new RestorePostCommand(post.Id), ct),
            "delete" => mediator.SendAsync(new DeletePostCommand(post.Id), ct),
            "empty" => mediator.SendAsync(new EmptyRecycleBinCommand(), ct),
            "cancel" => mediator.SendAsync(new CancelScheduleCommand(post.Id), ct),
            "postpone" => mediator.SendAsync(new PostponePostCommand(post.Id, 1), ct),
            "scheduled-publish" => mediator.SendAsync(new PublishScheduledPostCommand(), ct),
            "category-update" => mediator.SendAsync(new UpdateCategoryCommand
            {
                Id = environment.CategoryId, DisplayName = "Updated category", Slug = "updated"
            }, ct),
            "category-delete" => mediator.SendAsync(new DeleteCategoryCommand(environment.CategoryId), ct),
            "tag-update" => mediator.SendAsync(new UpdateTagCommand(environment.TagId, "updated"), ct),
            "tag-delete" => mediator.SendAsync(new DeleteTagCommand(environment.TagId), ct),
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        };
        await write;

        foreach (var (partition, key) in keys)
        {
            Assert.Equal("fresh", environment.Cache.GetOrCreate(partition, key, () => "fresh"));
        }
        Assert.Equal("1:probe", environment.Invalidator.GetCacheKey("probe"));
        foreach (var key in TestEnvironment.UnrelatedKeys)
        {
            Assert.Equal("preserved", environment.Cache.GetOrCreate("General", key, () => "missing"));
        }
        Assert.Equal("preserved", environment.Cache.GetOrCreate("Page", "about", () => "missing"));
    }

    [Theory]
    [InlineData("unpublish")]
    [InlineData("recycle")]
    [InlineData("delete")]
    [InlineData("rename")]
    public async Task PublicPost_AfterRemovalOrSlugChange_OldUrlReturnsNotFound(string operation)
    {
        using var environment = await TestEnvironment.CreateAsync();
        Assert.IsType<PageResult>(await environment.ReadPostAsync());
        using var scope = environment.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<ICommandMediator>();
        var ct = TestContext.Current.CancellationToken;

        Task write = operation switch
        {
            "unpublish" => mediator.SendAsync(new UnpublishPostCommand(environment.PostId), ct),
            "recycle" => mediator.SendAsync(new DeletePostCommand(environment.PostId, SoftDelete: true), ct),
            "delete" => mediator.SendAsync(new DeletePostCommand(environment.PostId), ct),
            "rename" => mediator.SendAsync(new UpdatePostCommand(environment.PostId, environment.EditModel()), ct),
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        };
        await write;

        Assert.IsType<NotFoundResult>(await environment.ReadPostAsync());
    }

    [Theory]
    [InlineData(false, null)]
    [InlineData(true, null)]
    [InlineData(false, "tech")]
    [InlineData(true, "tech")]
    public async Task Feed_AfterPostUpdate_ContainsNewTitle(bool atom, string? category)
    {
        using var environment = await TestEnvironment.CreateAsync();
        Assert.Contains("Old title", await environment.ReadFeedAsync(atom, category));
        using var scope = environment.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<ICommandMediator>()
            .SendAsync(new UpdatePostCommand(environment.PostId, environment.EditModel()), TestContext.Current.CancellationToken);

        var xml = await environment.ReadFeedAsync(atom, category);

        Assert.Contains("New title", xml);
        Assert.DoesNotContain("Old title", xml);
    }

    [Fact]
    public async Task ScheduledBatch_PublishesBothPostsAndInvalidatesSitemapOnce()
    {
        using var environment = await TestEnvironment.CreateAsync();
        using var scope = environment.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();
        var post = await db.Post.SingleAsync(TestContext.Current.CancellationToken);
        post.PostStatus = PostStatus.Scheduled;
        post.ScheduledPublishTimeUtc = DateTime.UtcNow.AddMinutes(-1);
        post.RouteLink = null;
        var secondPost = TestEnvironment.CreatePost(Guid.NewGuid(), environment.PublishDateUtc);
        secondPost.PostStatus = PostStatus.Scheduled;
        secondPost.Slug = "second-post";
        secondPost.ScheduledPublishTimeUtc = post.ScheduledPublishTimeUtc;
        secondPost.RouteLink = null;
        db.Post.Add(secondPost);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        environment.PrimeCaches();

        await scope.ServiceProvider.GetRequiredService<ICommandMediator>()
            .SendAsync(new PublishScheduledPostCommand(), TestContext.Current.CancellationToken);

        Assert.Equal(2, await db.Post.CountAsync(p => p.PostStatus == PostStatus.Published, TestContext.Current.CancellationToken));
        Assert.Equal("1:probe", environment.Invalidator.GetCacheKey("probe"));
        Assert.Contains("Old title", await environment.ReadFeedAsync(false, null));
        var sitemap = await SiteMapMapHandler.Handle(new DefaultHttpContext(),
            environment.Services.GetRequiredService<IBlogConfig>(), environment.Cache, environment.Invalidator, db);
        Assert.Contains("second-post", Assert.IsAssignableFrom<Microsoft.AspNetCore.Http.HttpResults.ContentHttpResult>(sitemap).ResponseContent);
    }

    [Fact]
    public async Task MissingObjectsEmptyBatchesAndViewCounts_KeepCaches()
    {
        using var environment = await TestEnvironment.CreateAsync();
        using var scope = environment.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<ICommandMediator>();
        var ct = TestContext.Current.CancellationToken;
        var missing = Guid.NewGuid();
        environment.PrimeCaches();

        await mediator.SendAsync(new PublishPostCommand(missing), ct);
        await mediator.SendAsync(new UnpublishPostCommand(missing), ct);
        await mediator.SendAsync(new DeletePostCommand(missing), ct);
        await mediator.SendAsync(new RestorePostCommand(missing), ct);
        await mediator.SendAsync(new CancelScheduleCommand(environment.PostId), ct);
        await mediator.SendAsync(new PostponePostCommand(environment.PostId, 1), ct);
        await mediator.SendAsync(new DeleteCategoryCommand(missing), ct);
        await mediator.SendAsync(new UpdateCategoryCommand { Id = missing }, ct);
        await mediator.SendAsync(new DeleteTagCommand(int.MaxValue), ct);
        await mediator.SendAsync(new UpdateTagCommand(int.MaxValue, "missing"), ct);
        await mediator.SendAsync(new EmptyRecycleBinCommand(), ct);
        await mediator.SendAsync(new PublishScheduledPostCommand(), ct);
        await mediator.SendAsync(new AddRequestCountCommand(environment.PostId), ct);
        await mediator.SendAsync(new AddViewCountCommand(environment.PostId), ct);

        Assert.Equal("0:probe", environment.Invalidator.GetCacheKey("probe"));
        Assert.Equal("cached", environment.Cache.GetOrCreate("General", environment.Invalidator.GetCacheKey("rss"), () => "missing"));
    }

    [Fact]
    public async Task DatabaseWriteFailure_LeavesCachedPublicPostIntact()
    {
        using var environment = await TestEnvironment.CreateAsync();
        Assert.IsType<PageResult>(await environment.ReadPostAsync());
        using var scope = environment.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();
        await db.Database.ExecuteSqlRawAsync(
            "CREATE TRIGGER reject_post_write BEFORE UPDATE ON Post BEGIN SELECT RAISE(ABORT, 'Write rejected'); END;",
            TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<DbUpdateException>(() => scope.ServiceProvider.GetRequiredService<ICommandMediator>()
            .SendAsync(new UnpublishPostCommand(environment.PostId), TestContext.Current.CancellationToken));

        Assert.Equal("0:probe", environment.Invalidator.GetCacheKey("probe"));
        Assert.IsType<PageResult>(await environment.ReadPostAsync());
    }

    [Fact]
    public async Task CallerCancelledAfterCommit_StillInvalidatesCache()
    {
        using var cancellation = new CancellationTokenSource();
        var interceptor = new CancelAfterSaveInterceptor(cancellation);
        using var environment = await TestEnvironment.CreateAsync(interceptor);
        environment.PrimeCaches();
        using var scope = environment.Services.CreateScope();
        interceptor.Enabled = true;

        var handler = ActivatorUtilities.CreateInstance<UnpublishPostCommandHandler>(scope.ServiceProvider);
        await handler.HandleAsync(new UnpublishPostCommand(environment.PostId), cancellation.Token);

        Assert.True(cancellation.IsCancellationRequested);
        Assert.Equal("1:probe", environment.Invalidator.GetCacheKey("probe"));
        Assert.IsType<NotFoundResult>(await environment.ReadPostAsync());
    }

    [Fact]
    public async Task PageSiteMapFilter_InvalidatesSiteMapAndPreservesArticleCaches()
    {
        using var environment = await TestEnvironment.CreateAsync();
        environment.PrimeCaches();
        var oldKey = environment.Invalidator.SiteMapCacheKey;
        var filter = new ClearBlogCache(BlogCacheType.SiteMap, environment.Cache, environment.Invalidator);

        filter.OnActionExecuted(new ActionExecutedContext(
            new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()), [], new object()));

        Assert.NotEqual(oldKey, environment.Invalidator.SiteMapCacheKey);
        Assert.Equal("fresh", environment.Cache.GetOrCreate("General", oldKey, () => "fresh"));
        Assert.Equal("cached", environment.Cache.GetOrCreate("General", environment.Invalidator.GetCacheKey("rss"), () => "missing"));
        Assert.Equal("cached", environment.Cache.GetOrCreate("Post", environment.Invalidator.GetCacheKey("old-post"), () => "missing"));
    }

    [Theory]
    [InlineData("Post", "old-post")]
    [InlineData("RssCategory", "tech")]
    [InlineData("AtomCategory", "tech")]
    [InlineData("General", "rss")]
    [InlineData("General", "atom")]
    [InlineData("General", "sitemap")]
    public async Task InvalidationDuringCacheFill_OldQueryCannotPopulateCurrentVersion(string partition, string key)
    {
        using var environment = await TestEnvironment.CreateAsync();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var finish = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        string CurrentKey() => key == "sitemap" ? environment.Invalidator.SiteMapCacheKey : environment.Invalidator.GetCacheKey(key);
        var pending = environment.Cache.GetOrCreateAsync(partition, CurrentKey(), async () =>
        {
            started.SetResult();
            await finish.Task;
            return "stale";
        }, TimeSpan.FromMinutes(1));
        await started.Task.WaitAsync(TestContext.Current.CancellationToken);

        environment.Invalidator.Invalidate();
        finish.SetResult();
        await pending;

        Assert.Equal("fresh", await environment.Cache.GetOrCreateAsync(partition,
            CurrentKey(), () => Task.FromResult("fresh")));
    }

    private sealed class TestEnvironment(SqliteConnection connection, ServiceProvider services) : IDisposable
    {
        public static readonly string[] UnrelatedKeys = ["theme", "avatar", "widgets", "site-verification:test.txt"];
        public ServiceProvider Services { get; } = services;
        public ICacheAside Cache => Services.GetRequiredService<ICacheAside>();
        public PostCacheInvalidator Invalidator => Services.GetRequiredService<PostCacheInvalidator>();
        public Guid PostId { get; } = Guid.NewGuid();
        public Guid CategoryId { get; } = Guid.NewGuid();
        public int TagId { get; private set; }
        public DateTime PublishDateUtc { get; } = DateTime.UtcNow.AddDays(-1);

        public static async Task<TestEnvironment> CreateAsync(params IInterceptor[] interceptors)
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDbContext<BlogDbContext>(options => options.UseSqlite(connection).AddInterceptors(interceptors));
            services.AddSingleton(TimeProvider.System);
            services.AddInMemoryCacheAside();
            services.AddSingleton<PostCacheInvalidator>();
            services.AddSingleton<IBlogConfig>(new BlogConfig
            {
                GeneralSettings = new GeneralSettings
                {
                    SiteTitle = "Test blog", Description = "Test", Copyright = "Test", DefaultLanguageCode = "en-us",
                    OwnerEmail = "owner@example.com"
                },
                ContentSettings = new ContentSettings { EnableViewCount = false },
                FeedSettings = new FeedSettings { FeedItemCount = 20 }
            });
            services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string?> { ["PostCacheMinutes"] = "60" }).Build());
            services.AddHttpContextAccessor();
            services.AddSyndication();
            services.AddLiteBus(bus =>
            {
                bus.AddMessaging(_ => { });
                bus.AddCommands(module => module.RegisterFromAssembly(typeof(CreatePostCommand).Assembly));
                bus.AddQueries(module =>
                {
                    module.RegisterFromAssembly(typeof(GetPostBySlugQuery).Assembly);
                    module.RegisterFromAssembly(typeof(GetRssStringQuery).Assembly);
                });
                bus.AddEvents(module => module.RegisterFromAssembly(typeof(PostChangedEventHandler).Assembly));
            });
            var environment = new TestEnvironment(connection, services.BuildServiceProvider());
            using var scope = environment.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();
            await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
            var post = CreatePost(environment.PostId, environment.PublishDateUtc);
            var category = new CategoryEntity { Id = environment.CategoryId, DisplayName = "Tech", Slug = "tech" };
            var tag = new TagEntity { DisplayName = "original", NormalizedName = "original" };
            post.PostCategory.Add(new PostCategoryEntity { Category = category, PostId = post.Id, CategoryId = category.Id });
            post.Tags.Add(tag);
            db.Post.Add(post);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            environment.TagId = tag.Id;
            return environment;
        }

        public (string Partition, string Key)[] PrimeCaches()
        {
            (string, string)[] keys =
            [
                ("Post", Invalidator.GetCacheKey("old-post")),
                ("General", Invalidator.GetCacheKey("rss")),
                ("General", Invalidator.GetCacheKey("atom")),
                ("General", Invalidator.SiteMapCacheKey),
                ("RssCategory", Invalidator.GetCacheKey("tech")),
                ("AtomCategory", Invalidator.GetCacheKey("tech"))
            ];
            foreach (var (partition, key) in keys) Cache.GetOrCreate(partition, key, () => "cached");
            foreach (var key in UnrelatedKeys) Cache.GetOrCreate("General", key, () => "preserved");
            Cache.GetOrCreate("Page", "about", () => "preserved");
            return keys;
        }

        public async Task<IActionResult> ReadPostAsync()
        {
            using var scope = Services.CreateScope();
            var model = ActivatorUtilities.CreateInstance<PostModel>(scope.ServiceProvider);
            model.PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext(),
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
            };
            return await model.OnGetAsync(PublishDateUtc.Year, PublishDateUtc.Month, PublishDateUtc.Day, "old-post");
        }

        public async Task<string> ReadFeedAsync(bool atom, string? category)
        {
            using var scope = Services.CreateScope();
            var context = new DefaultHttpContext();
            context.Request.Scheme = "https";
            context.Request.Host = new HostString("blog.example.com");
            Services.GetRequiredService<IHttpContextAccessor>().HttpContext = context;
            var controller = ActivatorUtilities.CreateInstance<SubscriptionController>(scope.ServiceProvider);
            var result = atom ? await controller.Atom(category) : await controller.Rss(category);
            return Assert.IsType<ContentResult>(result).Content!;
        }

        public PostEditModel EditModel() => new()
        {
            PostId = PostId, Title = "New title", Slug = "new-post", Author = "Test author", EditorContent = "New content", Abstract = "New abstract",
            PostStatus = PostStatus.Published, ContentType = "html", FeedIncluded = true, LanguageCode = "en-us",
            SelectedCatIds = [CategoryId], Tags = "original"
        };

        public static PostEntity CreatePost(Guid id, DateTime publishDateUtc) => new()
        {
            Id = id, Title = "Old title", Slug = "old-post", Author = "Test author", PostContent = "Old content", ContentAbstract = "Old abstract",
            PostStatus = PostStatus.Published, PubDateUtc = publishDateUtc, CreateTimeUtc = publishDateUtc,
            LastModifiedUtc = publishDateUtc, RouteLink = UrlHelper.GenerateRouteLink(publishDateUtc, "old-post"),
            IsFeedIncluded = true, ContentLanguageCode = "en-us", ContentType = "html"
        };

        public void Dispose()
        {
            Services.Dispose();
            connection.Dispose();
        }
    }

    private sealed class CancelAfterSaveInterceptor(CancellationTokenSource cancellation) : SaveChangesInterceptor
    {
        public bool Enabled { get; set; }

        public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result,
            CancellationToken cancellationToken = default)
        {
            if (Enabled) cancellation.Cancel();
            return ValueTask.FromResult(result);
        }
    }
}
