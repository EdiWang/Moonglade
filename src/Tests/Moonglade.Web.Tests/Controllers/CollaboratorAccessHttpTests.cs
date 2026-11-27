using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moonglade.ActivityLog;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Features.Category;
using Moonglade.Features.Post;
using Moonglade.Moderation;
using Moonglade.Web.Controllers;
using Moonglade.Web.Models;
using Moonglade.Web.Services;
using Moonglade.Web.Tests.TestSupport;
using Moq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Moonglade.Web.Tests.Controllers;

public class CollaboratorAccessHttpTests
{
    private const string Id = "575f4f24-7f4b-44e4-b2b5-8f38abec0dc3";

    private static readonly (string Method, string Path)[] ContentRoutes =
    [
        ("GET", "/admin/post"), ("GET", "/admin/post/draft"), ("GET", "/admin/post/scheduled"),
        ("GET", "/admin/post/edit"), ("GET", $"/admin/post/edit/{Id}"), ("GET", $"/admin/post/preview/{Id}"),
        ("GET", "/admin/post/recycle-bin"), ("GET", "/admin/page"), ("GET", "/admin/page/edit"),
        ("GET", $"/admin/page/edit/{Id}"), ("GET", $"/admin/page/preview/{Id}"),
        ("GET", "/admin/page/recycle-bin"), ("GET", "/admin/category"), ("GET", "/admin/tags"),
        ("GET", "/admin/comments"),
        ("GET", "/api/post/list"), ("GET", "/api/post/meta"), ("GET", "/api/post/drafts"), ("GET", $"/api/post/{Id}"),
        ("POST", "/api/post/createoredit"), ("PUT", $"/api/post/{Id}/publish"), ("PUT", $"/api/post/{Id}/unpublish"),
        ("DELETE", $"/api/post/{Id}/recycle"), ("POST", "/api/post/keep-alive"), ("GET", "/api/post/list/recyclebin"),
        ("GET", "/api/schedule/list"), ("PUT", $"/api/schedule/cancel/{Id}"), ("PUT", $"/api/schedule/postpone/{Id}"),
        ("POST", $"/api/post/{Id}/restore"), ("POST", $"/api/post/page/{Id}/restore"),
        ("GET", "/api/page/segment/list"), ("GET", $"/api/page/{Id}"), ("POST", "/api/page"),
        ("PUT", $"/api/page/{Id}"), ("DELETE", $"/api/page/{Id}"),
        ("GET", "/api/category/list"), ("GET", "/api/category/100"), ("POST", "/api/category"),
        ("PUT", "/api/category/100"), ("DELETE", "/api/category/100"),
        ("GET", "/api/tags/names"), ("GET", "/api/tags/list"), ("GET", "/api/tags/list/count"),
        ("POST", "/api/tags"), ("PUT", "/api/tags/100"), ("DELETE", "/api/tags/100"),
        ("GET", "/api/comment/list"), ("PUT", $"/api/comment/{Id}/approval/toggle"),
        ("POST", $"/api/comment/{Id}/reply"), ("DELETE", "/api/comment"), ("POST", "/image")
    ];

    private static readonly (string Method, string Path)[] OwnerRoutes =
    [
        ("GET", "/admin"), ("GET", "/admin/dashboard"), ("GET", "/admin/about"), ("GET", "/admin/account"),
        ("GET", "/admin/account/oidc"), ("GET", "/admin/account/oidc/verify"), ("POST", "/admin/account/oidc"),
        ("GET", "/admin/activitylogs"), ("GET", "/admin/mention"), ("GET", "/admin/menu"), ("GET", "/admin/widgets"),
        ("GET", "/admin/settings/general"), ("GET", "/admin/settings/advanced"), ("GET", "/admin/settings/appearance"),
        ("GET", "/admin/settings/comment"), ("GET", "/admin/settings/content"), ("GET", "/admin/settings/image"),
        ("GET", "/admin/settings/notification"), ("GET", "/admin/settings/seo"), ("GET", "/admin/settings/subscription"),
        ("GET", "/admin/settings/verification-files"),
        ("POST", "/api/settings/general"), ("POST", "/api/settings/content"), ("POST", "/api/settings/comment"),
        ("POST", "/api/settings/notification"), ("POST", "/api/settings/email/test"), ("POST", "/api/settings/seo"),
        ("POST", "/api/settings/subscription"), ("POST", "/api/settings/watermark"), ("POST", "/api/settings/advanced"),
        ("POST", "/api/settings/appearance"), ("PATCH", "/api/settings/general"),
        ("GET", "/api/settings/custom-menu"), ("POST", "/api/settings/custom-menu"),
        ("GET", "/api/widgets"), ("POST", "/api/widgets/preview"), ("PUT", "/api/widgets"),
        ("POST", "/api/theme"), ("DELETE", "/api/theme/100"),
        ("GET", "/api/mention/list"), ("DELETE", "/api/mention"), ("DELETE", "/api/mention/clear"),
        ("GET", "/api/activitylog/eventtypes"), ("GET", "/api/activitylog/list"),
        ("GET", "/api/activitylog/1/metadata"), ("DELETE", "/api/activitylog/clear"),
        ("GET", "/api/dataporting/export/posts"), ("GET", "/api/dataporting/export/pages"),
        ("GET", "/api/site-verification-files/list"), ("GET", $"/api/site-verification-files/{Id}"),
        ("POST", "/api/site-verification-files"), ("PUT", $"/api/site-verification-files/{Id}"),
        ("POST", $"/api/site-verification-files/{Id}/toggle"), ("DELETE", $"/api/site-verification-files/{Id}"),
        ("GET", "/siteicon"), ("POST", "/siteicon"), ("PUT", "/api/account/profile"), ("GET", "/api/account/email"),
        ("PUT", "/api/account/email"), ("GET", "/api/account/password/generate"), ("PUT", "/api/account/password/local"),
        ("PUT", "/api/account/totp/local/reset"), ("POST", "/avatar"), ("POST", "/api/account/avatar"),
        ("PUT", "/api/account/avatar/gravatar"), ("GET", "/auth/me"),
        ("DELETE", $"/api/post/{Id}/destroy"), ("DELETE", $"/api/post/page/{Id}/destroy"),
        ("DELETE", "/api/post/recyclebin"), ("DELETE", "/api/post/page/recyclebin")
    ];

    private static readonly (string Method, string Path)[] PublicRoutes =
    [
        ("GET", "/"), ("GET", "/post/2026/11/26/sample"), ("GET", "/page/sample"),
        ("GET", "/rss"), ("GET", "/atom"), ("GET", "/opml"), ("GET", "/auth/signin"),
        ("GET", "/avatar"), ("GET", "/favicon.ico"), ("GET", "/image/sample.svg"),
        ("HEAD", "/image/sample.svg"), ("POST", $"/api/comment/{Id}"), ("POST", "/webmention"),
        ("POST", "/api/postview"), ("POST", "/api/language/set")
    ];

    [Fact]
    public async Task DirectRoutes_EnforceContentAndOwnerPermissionsWithValidAntiforgery()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fixture = await ActivityLogHttpFixture.CreateAsync(ct: ct, includeSettings: true,
            configureServices: services => services.Configure<MvcOptions>(options => options.Filters.Add(new RouteProbeFilter())),
            enableAntiforgeryEndpoint: true, additionalControllers:
            [
                typeof(AccountController), typeof(AssetsController), typeof(AuthController), typeof(CategoryController),
                typeof(CommentController), typeof(DataPortingController), typeof(ImageController), typeof(LanguageController),
                typeof(MentionController), typeof(OwnerAvatarController), typeof(PageController), typeof(PostController),
                typeof(PostViewController), typeof(RecycleBinController), typeof(ScheduleController),
                typeof(SiteVerificationFilesController), typeof(SubscriptionController), typeof(TagsController),
                typeof(ThemeController), typeof(WidgetsController)
            ]);

        foreach (var user in new[] { "admin", "collaborator", "reader", null })
        {
            using var client = await CreateClientAsync(fixture, user, ct);
            var denied = user is null ? HttpStatusCode.Unauthorized : HttpStatusCode.Forbidden;
            await AssertRoutes(client, user, ContentRoutes,
                user is "admin" or "collaborator" ? HttpStatusCode.NoContent : denied, ct);
            await AssertRoutes(client, user, OwnerRoutes, user == "admin" ? HttpStatusCode.NoContent : denied, ct);
            await AssertRoutes(client, user, PublicRoutes, HttpStatusCode.NoContent, ct);
        }
    }

    [Fact]
    public async Task CollaboratorWrites_KeepPublicAuthorAndPersistActorSnapshotForOwnerHistory()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fixture = await ActivityLogHttpFixture.CreateAsync(ct: ct,
            configureServices: services =>
            {
                services.AddSingleton(Mock.Of<IModeratorService>());
                services.AddSingleton<CommentSubmissionGuard>();
            },
            enableAntiforgeryEndpoint: true,
            additionalControllers: [typeof(PostController), typeof(CategoryController), typeof(CommentController), typeof(RecycleBinController)]);
        using var collaborator = await CreateClientAsync(fixture, "collaborator", ct);
        using var owner = await CreateClientAsync(fixture, "admin", ct);
        var post = new PostEditRequest
        {
            Title = "Shared draft",
            Slug = "shared-draft",
            Author = "Public byline",
            EditorContent = "Body",
            Abstract = "Summary",
            PostStatus = PostStatus.Draft,
            ContentType = "markdown",
            LanguageCode = "en-US",
            SelectedCatIds = [],
            EnableComment = true
        };
        using var saved = await owner.PostAsJsonAsync("/api/post/createoredit", post, ct);
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
        var postId = (await saved.Content.ReadFromJsonAsync<SavePostResponse>(ct))!.PostId;
        post.PostId = postId;
        post.Title = "Updated shared draft";
        using var updated = await collaborator.PostAsJsonAsync("/api/post/createoredit", post, ct);
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        using var deniedDestroy = await collaborator.DeleteAsync($"/api/post/{postId}/destroy", ct);
        Assert.Equal(HttpStatusCode.Forbidden, deniedDestroy.StatusCode);
        var category = new CreateCategoryCommand { DisplayName = "Shared category", Slug = "shared", Note = "Description" };
        using var created = await collaborator.PostAsJsonAsync("/api/category", category, ct);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        using (var scope = fixture.App.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();
            var storedPost = await db.Post.SingleAsync(row => row.Id == postId, ct);
            Assert.Equal(post.Title, storedPost.Title);
            Assert.Equal("Public byline", storedPost.Author);
            db.Comment.Add(new CommentEntity
            {
                Id = Guid.NewGuid(),
                PostId = postId,
                Username = "Visitor",
                Email = "visitor@example.test",
                IPAddress = "192.0.2.13",
                CommentContent = "Comment text",
                CreateTimeUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync(ct);
        }
        using var comments = JsonDocument.Parse(await collaborator.GetStringAsync("/api/comment/list", ct));
        var comment = Assert.Single(comments.RootElement.GetProperty("items").EnumerateArray());
        Assert.Equal("visitor@example.test", comment.GetProperty("email").GetString());
        Assert.Equal("192.0.2.13", comment.GetProperty("ipAddress").GetString());

        var rows = await fixture.Store.ReadLogsAsync(ct);
        Assert.Equal(2, rows.Count(row => row.ActorKind == ActivityActorKind.Collaborator));
        var ownerRow = Assert.Single(rows, row => row.EventKey == ActivityEvents.PostCreated && row.ActorAccountId == ActivityLogHttpFixture.OwnerId);
        Assert.Equal(ActivityActorKind.Administrator, ownerRow.ActorKind);
        foreach (var eventKey in new[] { ActivityEvents.PostUpdated, ActivityEvents.CategoryCreated })
        {
            var row = Assert.Single(rows, row => row.ActorKind == ActivityActorKind.Collaborator && row.EventKey == eventKey);
            Assert.Equal(ActivityLogHttpFixture.CollaboratorId, row.ActorAccountId);
            Assert.Equal("Collaborator display name", row.ActorDisplayName);
            using var detail = JsonDocument.Parse(await owner.GetStringAsync($"/api/activitylog/{row.Id}/metadata", ct));
            Assert.Equal(ActivityLogHttpFixture.CollaboratorId, detail.RootElement.GetProperty("actorAccountId").GetGuid());
            Assert.Equal("Collaborator", detail.RootElement.GetProperty("actorKind").GetString());
        }

        var legacy = rows.First(row => row.ActorDisplayName == "historical-admin");
        using var legacyDetail = JsonDocument.Parse(await owner.GetStringAsync($"/api/activitylog/{legacy.Id}/metadata", ct));
        Assert.False(legacyDetail.RootElement.TryGetProperty("actorAccountId", out _));
        using var denied = await collaborator.DeleteAsync("/api/activitylog/clear", ct);
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        Assert.Equal(rows.Select(row => row.Id), (await fixture.Store.ReadLogsAsync(ct)).Select(row => row.Id));
    }

    private static async Task<HttpClient> CreateClientAsync(ActivityLogHttpFixture fixture, string? user, CancellationToken ct)
    {
        var client = fixture.App.GetTestClient();
        if (user is not null) client.DefaultRequestHeaders.Add("X-B1-User", user);
        using var token = await client.GetAsync("/test/antiforgery", ct);
        token.EnsureSuccessStatusCode();
        client.DefaultRequestHeaders.Add("Cookie", token.Headers.GetValues("Set-Cookie").Single().Split(';')[0]);
        client.DefaultRequestHeaders.Add("XSRF-TOKEN", await token.Content.ReadAsStringAsync(ct));
        return client;
    }

    private static async Task AssertRoutes(HttpClient client, string? user,
        IEnumerable<(string Method, string Path)> routes, HttpStatusCode status, CancellationToken ct)
    {
        foreach (var (method, path) in routes)
        {
            using var request = new HttpRequestMessage(new HttpMethod(method), path);
            using var response = await client.SendAsync(request, ct);
            Assert.True(response.StatusCode == status,
                $"{user ?? "anonymous"} {method} {path}: expected {status}, received {response.StatusCode}.");
        }
    }

    // Stop after routing, permission checks and antiforgery so the matrix can cover write routes without performing those writes.
    private sealed class RouteProbeFilter : IResourceFilter
    {
        public void OnResourceExecuting(ResourceExecutingContext context) => context.Result = new NoContentResult();

        public void OnResourceExecuted(ResourceExecutedContext context) { }
    }
}
