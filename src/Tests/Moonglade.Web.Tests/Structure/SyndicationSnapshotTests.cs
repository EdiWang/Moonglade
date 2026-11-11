using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moonglade.Auth;
using Moonglade.Configuration;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Syndication;
using System.Data.Common;
using System.Xml.Linq;

namespace Moonglade.Web.Tests.Structure;

public class SyndicationSnapshotTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task FeedQuery_PinsMetadataSelectionAndFormattingAcrossPublication_AndRefreshesNextOperation(bool atom, bool category)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(ct);
        var barrier = new FeedReadBarrier();
        await using var db = new BlogDbContext(new DbContextOptionsBuilder<BlogDbContext>()
            .UseSqlite(connection).AddInterceptors(barrier).Options);
        await db.Database.EnsureCreatedAsync(ct);
        var cat = new CategoryEntity { Id = 100, DisplayName = "Tech", Slug = "tech" };
        db.Category.Add(cat);
        for (var i = 0; i < 3; i++)
        {
            db.Post.Add(new PostEntity
            {
                Id = Guid.NewGuid(),
                Title = $"Post {i}",
                Slug = $"post-{i}",
                Author = "Author",
                PostContent = "<p>Full marker</p><img src=\"/image/photo.png\">",
                ContentAbstract = "Summary marker",
                ContentLanguageCode = "en-us",
                ContentType = "html",
                IsFeedIncluded = true,
                PostStatus = PostStatus.Published,
                CreateTimeUtc = DateTime.UtcNow.AddDays(-2),
                PubDateUtc = DateTime.UtcNow.AddDays(-1),
                RouteLink = $"2026/1/1/post-{i}",
                PostCategory = [new PostCategoryEntity { CategoryId = cat.Id }]
            });
        }
        await db.SaveChangesAsync(ct);
        var owner = new BlogConfig(new BlogSettingsSnapshot
        {
            GeneralSettings = GeneralSettings.DefaultValue with { SiteTitle = "Before" },
            FeedSettings = new FeedSettings { FeedItemCount = 3, UseFullContent = true },
            ImageSettings = new ImageSettings { EnableCDNRedirect = true, CDNEndpoint = "https://before.example.com" }
        });
        var retained = owner.Capture();
        var profile = new OwnerProfile(Guid.NewGuid(), "before@example.com");
        var retainedProfile = profile;
        var reader = new CountingReader(owner);
        var context = new DefaultHttpContext();
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("blog.example.com");
        var accessor = new HttpContextAccessor { HttpContext = context };
        var source = new SyndicationDataSource(accessor, db);
        var rss = new GetRssStringQueryHandler(reader, source, accessor);
        var atomHandler = new GetAtomStringQueryHandler(reader, source, accessor);
        Task<string> Read() => atom
            ? atomHandler.HandleAsync(new GetAtomStringQuery(category ? "tech" : null, Owner: profile), ct)
            : rss.HandleAsync(new GetRssStringQuery(category ? "tech" : null), ct);
        barrier.Enabled = true;

        var pending = Read();
        await barrier.Started.Task.WaitAsync(TimeSpan.FromSeconds(10), ct);
        owner.Publish(owner.Capture() with
        {
            GeneralSettings = owner.Capture().GeneralSettings with { SiteTitle = "After" },
            FeedSettings = new FeedSettings { FeedItemCount = 1, UseFullContent = false },
            ImageSettings = new ImageSettings { EnableCDNRedirect = false }
        });
        profile = profile with { Email = "after@example.com" };
        barrier.Resume.SetResult();
        var first = await pending;

        Assert.Contains("Before", first);
        Assert.Contains("https://before.example.com/photo.png", first);
        Assert.Equal(3, EntryCount(first, atom));
        if (atom) Assert.Contains("before@example.com", first);
        Assert.Equal(1, reader.Captures);

        var second = await Read();

        Assert.Contains("After", second);
        Assert.Contains("Summary marker", second);
        Assert.DoesNotContain("Full marker", second);
        Assert.DoesNotContain("https://before.example.com", second);
        Assert.Equal(1, EntryCount(second, atom));
        if (atom) Assert.Contains("after@example.com", second);
        Assert.Equal(2, reader.Captures);

        var supplied = atom
            ? await atomHandler.HandleAsync(new GetAtomStringQuery(category ? "tech" : null, retained, retainedProfile), ct)
            : await rss.HandleAsync(new GetRssStringQuery(category ? "tech" : null, retained), ct);

        Assert.Contains("Before", supplied);
        Assert.Contains("https://before.example.com/photo.png", supplied);
        Assert.Equal(3, EntryCount(supplied, atom));
        if (atom) Assert.Contains("before@example.com", supplied);
        Assert.Equal(2, reader.Captures);
    }

    private static int EntryCount(string xml, bool atom) => XDocument.Parse(xml).Descendants()
        .Count(element => element.Name.LocalName == (atom ? "entry" : "item"));

    private sealed class CountingReader(BlogConfig owner) : IBlogConfig
    {
        public int Captures { get; private set; }
        public BlogSettingsSnapshot Capture()
        {
            Captures++;
            return owner.Capture();
        }
    }

    private sealed class FeedReadBarrier : DbCommandInterceptor
    {
        public bool Enabled { get; set; }
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Resume { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override async ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData,
            DbDataReader result, CancellationToken cancellationToken = default)
        {
            if (Enabled && command.CommandText.Contains("FROM \"Post\"", StringComparison.Ordinal))
            {
                Enabled = false;
                Started.SetResult();
                await Resume.Task.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
            }
            return result;
        }
    }
}
