using Edi.CacheAside.InMemory;
using LiteBus.Queries.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moonglade.Auth;
using Moonglade.Configuration;
using Moonglade.Data.Entities;
using Moonglade.Features.Category;
using Moonglade.Features.Post;
using Moonglade.Features.Tag;
using Moonglade.Theme;
using Moonglade.Web.Controllers;
using Moonglade.Widgets;
using Moq;
using System.Globalization;
using System.Net;
using System.Reflection;
using System.Security.Claims;

namespace Moonglade.Web.Tests.Pages;

public class SearchPageTests
{
    [Fact]
    public async Task FrenchLanguage_BindsSearchLanguage()
    {
        var ct = TestContext.Current.CancellationToken;
        var scenario = new SearchPageScenario();
        using var app = await CreateTestApp(scenario);
        using var client = app.GetTestClient();
        client.DefaultRequestHeaders.Add("Cookie",
            $"{CookieRequestCultureProvider.DefaultCookieName}={CookieRequestCultureProvider.MakeCookieValue(new RequestCulture("fr-FR"))}");

        using var response = await client.GetAsync("/search?term=bonjour&language=fr-fr", ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("fr-fr", Assert.Single(scenario.SearchQueries).LanguageCode);
    }

    [Fact]
    public async Task Search_RetainsHandlerCaptureAcrossPublication_AndRefreshesOnNextRequest()
    {
        var ct = TestContext.Current.CancellationToken;
        var scenario = new SearchPageScenario
        {
            Started = new(TaskCreationOptions.RunContinuationsAsynchronously),
            Resume = new(TaskCreationOptions.RunContinuationsAsynchronously)
        };
        var config = CreateBlogConfig();
        using var app = await CreateTestApp(scenario, config);
        using var client = app.GetTestClient();
        var pending = client.GetStringAsync("/search?term=settings", ct);
        await scenario.Started.Task.WaitAsync(TimeSpan.FromSeconds(10), ct);
        config.Publish(config.Capture() with
        {
            ContentSettings = config.Capture().ContentSettings with { PostListPageSize = 7 }
        });
        scenario.Resume.SetResult();
        await pending;

        Assert.Equal(2, scenario.SearchQueries[0].PageSize);

        await client.GetStringAsync("/search?term=settings", ct);

        Assert.Equal(7, scenario.SearchQueries[1].PageSize);
    }

    [Fact]
    public async Task Search_BindsQueryStringStateAndReturnsNoIndexHeader()
    {
        var scenario = new SearchPageScenario
        {
            Result = new SearchPostQueryResult([], 7)
        };
        using var app = await CreateTestApp(scenario);
        using var client = app.GetTestClient();

        var response = await client.GetAsync(
            "/search?term=Azure%20Functions&sort=title-desc&category=dotnet&tag=azure&language=ja-jp&startDate=2026-01-02&endDate=2026-02-03",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("noindex, nofollow", response.Headers.GetValues("X-Robots-Tag").Single());

        var query = Assert.Single(scenario.SearchQueries);
        Assert.Equal("Azure Functions", query.Keyword);
        Assert.Equal(2, query.PageSize);
        Assert.Equal(1, query.PageIndex);
        Assert.Equal("dotnet", query.CategorySlug);
        Assert.Equal("azure", query.Tag);
        Assert.Equal("ja-jp", query.LanguageCode);
        Assert.Equal(new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc), query.StartDateUtc);
        Assert.Equal(new DateTime(2026, 2, 3, 0, 0, 0, DateTimeKind.Utc), query.EndDateUtc);
        Assert.Equal(SearchPostSort.TitleDescending, query.Sort);
    }

    [Fact]
    public async Task Search_WhenRequestedPageExceedsResultPages_RedirectsWithQueryStringState()
    {
        var scenario = new SearchPageScenario
        {
            Result = new SearchPostQueryResult([], 3)
        };
        using var app = await CreateTestApp(scenario);
        using var client = app.GetTestClient();

        var response = await client.GetAsync(
            "/search?term=Azure&p=9&sort=oldest&category=dotnet&tag=azure&language=en-us&startDate=2026-01-02&endDate=2026-02-03",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        var location = response.Headers.Location?.OriginalString;
        Assert.NotNull(location);
        Assert.Contains("term=Azure", location);
        Assert.Contains("p=2", location);
        Assert.Contains("sort=oldest", location);
        Assert.Contains("category=dotnet", location);
        Assert.Contains("tag=azure", location);
        Assert.Contains("language=en-us", location);
        Assert.Contains("startDate=2026-01-02", location);
        Assert.Contains("endDate=2026-02-03", location);

        var query = Assert.Single(scenario.SearchQueries);
        Assert.Equal(9, query.PageIndex);
        Assert.Equal(SearchPostSort.Oldest, query.Sort);
    }

    [Theory]
    [InlineData("")]
    [InlineData("/blog")]
    public async Task PublicAndAdminRequests_DoNotMutateSettingsSnapshot(string pathBase)
    {
        var config = CreateBlogConfig();
        config.Publish(config.Capture() with
        {
            GeneralSettings = config.Capture().GeneralSettings with
            {
                WidgetsProfile = true,
                SideBarOption = SideBarOption.Right
            }
        });
        var retained = config.Capture();
        using var app = await CreateTestApp(new SearchPageScenario(), config, pathBase);
        using var client = app.GetTestClient();
        foreach (var route in new[] { "/search?term=avatar", "/admin/settings/general" })
        {
            using var response = await client.GetAsync(route, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Same(retained, config.Capture());
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("/blog")]
    public async Task AdminDisplayClaims_AreHtmlEncoded(string pathBase)
    {
        const string name = "<Admin & Owner>";
        const string email = "owner&portal@example.test";
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("name", name), new Claim("email", email)], "TestAuth"));
        using var app = await CreateTestApp(new SearchPageScenario(), pathBase: pathBase, user: user);
        using var client = app.GetTestClient();

        var html = await client.GetStringAsync("/admin/settings/general", TestContext.Current.CancellationToken);

        Assert.Contains(WebUtility.HtmlEncode(name), html);
        Assert.DoesNotContain(name, html);
    }

    private static async Task<WebApplication> CreateTestApp(SearchPageScenario scenario, BlogConfig? config = null, string pathBase = "",
        ClaimsPrincipal? user = null)
    {
        var webRoot = FindWebProjectRoot();
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ApplicationName = typeof(Program).Assembly.GetName().Name,
            ContentRootPath = webRoot,
            WebRootPath = Path.Combine(webRoot, "wwwroot"),
            EnvironmentName = "Development"
        });

        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["WidgetCacheMinutes"] = "20"
        });

        var cultures = new[] { "en-US", "zh-Hans", "zh-Hant", "de-DE", "ja-JP", "fr-FR" }
            .Select(code => new CultureInfo(code))
            .ToList();

        builder.Services.AddRouting();
        builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
        builder.Services.Configure<RequestLocalizationOptions>(options =>
        {
            options.DefaultRequestCulture = new("en-US");
            options.SupportedCultures = cultures;
            options.SupportedUICultures = cultures;
        });
        builder.Services.AddControllers(options => options.Filters.Add<Moonglade.Web.Filters.BlogSettingsFilter>());
        builder.Services.AddRazorPages()
            .ConfigureApplicationPartManager(manager =>
            {
                manager.FeatureProviders.Add(new AvatarControllerFeatureProvider());
            });
        builder.Services.AddSingleton<IBlogConfig>(config ?? CreateBlogConfig());
        builder.Services.AddSingleton<IOwnerProfileReader>(new Moonglade.Web.Tests.TestSupport.TestOwnerProfileReader());
        builder.Services.AddInMemoryCacheAside();
        builder.Services.AddSingleton<ThemeCacheInvalidator>();
        builder.Services.AddSingleton(CreateQueryMediator(scenario).Object);

        var app = builder.Build();
        app.Use((context, next) =>
        {
            context.Request.PathBase = pathBase;
            if (user is not null)
                context.User = user;
            return next(context);
        });
        app.UseRequestLocalization(app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value);
        app.UseRouting();
        app.MapRazorPages();
        app.MapControllers();
        await app.StartAsync(TestContext.Current.CancellationToken);
        return app;
    }

    private static BlogConfig CreateBlogConfig() => new(new BlogSettingsSnapshot
    {
        GeneralSettings = new GeneralSettings
        {
            SiteTitle = "Moonglade",
            LogoText = "moonglade",
            Copyright = "[c] 2026",
            DefaultLanguageCode = "en-us",
            SideBarOption = SideBarOption.Disabled,
            HotTagAmount = 0,
            WidgetsCategoryList = false,
            WidgetsProfile = false,
            WidgetsSubscriptionButtons = false
        },
        ContentSettings = new ContentSettings
        {
            PostListPageSize = 2,
            MaximumPageNumbersToDisplay = 5
        },
        AdvancedSettings = new AdvancedSettings(),
        AppearanceSettings = new AppearanceSettings(),
        ImageSettings = new ImageSettings(),
        CustomMenuSettings = new CustomMenuSettings()
    });

    private static Mock<IQueryMediator> CreateQueryMediator(SearchPageScenario scenario)
    {
        var queryMediator = new Mock<IQueryMediator>();
        queryMediator.Setup(x => x.QueryAsync(It.IsAny<ListWidgetsQuery>(), It.IsAny<QueryMediationSettings>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        queryMediator
            .Setup(x => x.QueryAsync(It.IsAny<GetSiteThemeStyleSheetQuery>(), It.IsAny<QueryMediationSettings>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(string.Empty);
        queryMediator
            .Setup(x => x.QueryAsync(It.IsAny<ListCategoriesQuery>(), It.IsAny<QueryMediationSettings>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new CategoryEntity
                {
                    Id = 100,
                    DisplayName = ".NET",
                    Slug = "dotnet"
                }
            ]);
        queryMediator
            .Setup(x => x.QueryAsync(It.IsAny<ListTagsQuery>(), It.IsAny<QueryMediationSettings>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new TagEntity
                {
                    Id = 1,
                    DisplayName = "Azure",
                    NormalizedName = "azure"
                }
            ]);
        queryMediator
            .Setup(x => x.QueryAsync(It.IsAny<SearchPostQuery>(), It.IsAny<QueryMediationSettings>(), It.IsAny<CancellationToken>()))
            .Returns<SearchPostQuery, QueryMediationSettings, CancellationToken>(async (query, _, _) =>
            {
                scenario.SearchQueries.Add(query);
                if (scenario.Started is not null)
                {
                    scenario.Started.TrySetResult();
                    await scenario.Resume!.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
                }
                return scenario.Result;
            });

        return queryMediator;
    }

    private sealed class AvatarControllerFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
    {
        public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
        {
            feature.Controllers.Clear();
            feature.Controllers.Add(typeof(OwnerAvatarController).GetTypeInfo());
            feature.Controllers.Add(typeof(AuthController).GetTypeInfo());
        }
    }

    private static string FindWebProjectRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "src", "Moonglade.Web");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate src/Moonglade.Web.");
    }

    private sealed class SearchPageScenario
    {
        public SearchPostQueryResult Result { get; init; } = new([], 0);
        public List<SearchPostQuery> SearchQueries { get; } = [];
        public TaskCompletionSource? Started { get; init; }
        public TaskCompletionSource? Resume { get; init; }
    }
}
