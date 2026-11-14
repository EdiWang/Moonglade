using LiteBus.Events.Abstractions;
using LiteBus.Queries.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moonglade.Auth;
using Moonglade.Configuration;
using Moonglade.Data;
using Moonglade.Syndication;
using Moonglade.Web.Controllers;
using Moonglade.Web.Tests.TestSupport;
using Moq;
using System.Xml.Linq;

namespace Moonglade.Web.Tests.Caching;

public partial class PostCacheInvalidationTests
{
    [Theory]
    [InlineData(false, null)]
    [InlineData(true, null)]
    [InlineData(false, "tech")]
    [InlineData(true, "tech")]
    public async Task OwnerEmailChange_RefreshesWarmAndColdAtomAcrossCategories_WithoutChangingRss(bool warm, string? category)
    {
        var ct = TestContext.Current.CancellationToken;
        using var environment = await TestEnvironment.CreateAsync();
        using var scope = environment.Services.CreateScope();
        var accounts = scope.ServiceProvider.GetRequiredService<OwnerAccountStore>();
        const string password = "Synthetic123!";
        var pending = await accounts.PrepareLocalSetupAsync("owner@example.test", password, ct);
        var before = await accounts.CompleteLocalSetupAsync(pending, null, ct);
        if (warm) Assert.Contains("owner@example.test", await environment.ReadFeedAsync(true, category));
        var rss = await environment.ReadFeedAsync(false, category);
        var generation = environment.Invalidator.GetFeedCacheKey("atom");
        var article = environment.Invalidator.GetCacheKey("probe");
        var sitemap = environment.Invalidator.SiteMapCacheKey;

        var changed = await accounts.ChangeLocalEmailAsync(before.Id, before.Revision, password, "changed@example.test", ct);

        Assert.NotEqual(generation, environment.Invalidator.GetFeedCacheKey("atom"));
        Assert.Equal(article, environment.Invalidator.GetCacheKey("probe"));
        Assert.Equal(sitemap, environment.Invalidator.SiteMapCacheKey);
        XNamespace atom = "http://www.w3.org/2005/Atom";
        var document = XDocument.Parse(await environment.ReadFeedAsync(true, category));
        var entry = Assert.Single(document.Descendants(atom + "entry"));
        Assert.Equal("Old title", entry.Element(atom + "title")?.Value);
        Assert.Equal(changed!.Email, entry.Element(atom + "author")?.Element(atom + "email")?.Value);
        Assert.Equal(RssContract(rss), RssContract(await environment.ReadFeedAsync(false, category)));
        var next = environment.Invalidator.GetFeedCacheKey("atom");
        Assert.Equal(changed, await accounts.ChangeLocalEmailAsync(changed.Id, changed.Revision, password, $" {changed.Email} ", ct));
        Assert.Equal(next, environment.Invalidator.GetFeedCacheKey("atom"));
    }

    private static string RssContract(string xml)
    {
        var document = XDocument.Parse(xml);
        document.Root?.Element("channel")?.Element("pubDate")?.Remove();
        return document.ToString(SaveOptions.DisableFormatting);
    }

    [Fact]
    public async Task InitialSetup_RefreshesPreviouslyMissingOwnerAtom_AfterDurableCommit()
    {
        var ct = TestContext.Current.CancellationToken;
        using var environment = await TestEnvironment.CreateAsync();
        using var scope = environment.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();
        await db.OwnerAccount.ExecuteDeleteAsync(ct);
        XNamespace atom = "http://www.w3.org/2005/Atom";
        var before = XDocument.Parse(await environment.ReadFeedAsync(true, null));
        Assert.Empty(before.Descendants(atom + "email"));
        Assert.Single(before.Descendants(atom + "entry"));
        var generation = environment.Invalidator.GetFeedCacheKey("atom");
        var local = new OwnerAccountStore(db, new LocalAccountTotpService(),
            Options.Create(new AuthenticationSettings { Totp = new TotpAuthenticationSettings { Required = false } }),
            TimeProvider.System, scope.ServiceProvider.GetRequiredService<IEventMediator>());
        var pending = await local.PrepareLocalSetupAsync("setup@example.test", "Synthetic123!", ct);

        await local.CompleteLocalSetupAsync(pending, null, ct);

        Assert.NotEqual(generation, environment.Invalidator.GetFeedCacheKey("atom"));
        var after = XDocument.Parse(await environment.ReadFeedAsync(true, null));
        Assert.Equal("setup@example.test", Assert.Single(after.Descendants(atom + "email")).Value);
        Assert.Equal("Old title", Assert.Single(after.Descendants(atom + "entry")).Element(atom + "title")?.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("tech")]
    public async Task AtomCache_PinsOwnerForKeyAndQuery_OldInFlightCannotRefillNewIdentity(string? category)
    {
        var ct = TestContext.Current.CancellationToken;
        using var environment = await TestEnvironment.CreateAsync();
        var profiles = new TestOwnerProfileReader();
        var captured = profiles.Owner!;
        var config = environment.Services.GetRequiredService<IBlogConfig>();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var resume = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var queries = new Mock<IQueryMediator>();
        var observed = new List<GetAtomStringQuery>();
        queries.Setup(query => query.QueryAsync(It.IsAny<GetAtomStringQuery>(), It.IsAny<QueryMediationSettings>(), It.IsAny<CancellationToken>()))
            .Returns(async (GetAtomStringQuery query, QueryMediationSettings _, CancellationToken _) =>
            {
                observed.Add(query);
                if (query.Owner == captured)
                {
                    started.TrySetResult();
                    await resume.Task.WaitAsync(ct);
                }
                return $"<feed>{query.Owner!.Email}</feed>";
            });
        var controller = new SubscriptionController(config, environment.Cache, environment.Invalidator, queries.Object, profiles)
        { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };

        var old = controller.Atom(category);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(10), ct);
        profiles.Owner = captured with { Email = "new@example.test" };
        var fresh = Assert.IsType<ContentResult>(await controller.Atom(category));
        resume.TrySetResult();
        var retained = Assert.IsType<ContentResult>(await old);
        var cached = Assert.IsType<ContentResult>(await controller.Atom(category));

        Assert.Equal("<feed>owner@example.test</feed>", retained.Content);
        Assert.Equal("<feed>new@example.test</feed>", fresh.Content);
        Assert.Same(fresh, cached);
        Assert.Equal(2, observed.Count);
        Assert.Equal(captured, observed[0].Owner);
        Assert.Equal(profiles.Owner, observed[1].Owner);
        Assert.All(observed, query => Assert.Same(config.Capture(), query.Snapshot));
        Assert.Equal(3, profiles.Reads);
    }
}
