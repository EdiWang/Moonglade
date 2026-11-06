using Edi.CacheAside.InMemory;
using LiteBus.Queries.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moonglade.Configuration;
using Moonglade.Data.DTO;
using Moonglade.Data.Entities;
using Moonglade.Features.Category;
using Moonglade.Features.Post;
using Moonglade.Features.Tag;
using Moonglade.Theme;
using Moonglade.Widgets;
using Moonglade.Web.Controllers;
using Moq;
using System.Globalization;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Moonglade.Web.Tests.Pages;

public class SearchPageTests
{
    [Fact]
    public async Task RenderAndComponents_RetainHandlerCaptureAcrossPublication_AndRefreshOnNextRender()
    {
        var ct = TestContext.Current.CancellationToken;
        var scenario = new SearchPageScenario
        {
            Started = new(TaskCreationOptions.RunContinuationsAsynchronously),
            Resume = new(TaskCreationOptions.RunContinuationsAsynchronously)
        };
        var config = CreateBlogConfig();
        config.Publish(config.Capture() with
        {
            GeneralSettings = config.Capture().GeneralSettings with { SiteTitle = "Before" },
            CustomMenuSettings = new CustomMenuSettings { IsEnabled = true, Menus = [new Menu { Title = "Old menu", Url = "/old" }] }
        });
        using var app = await CreateTestApp(scenario, config);
        using var client = app.GetTestClient();
        var pending = client.GetStringAsync("/search?term=settings", ct);
        await scenario.Started.Task.WaitAsync(TimeSpan.FromSeconds(10), ct);
        config.Publish(config.Capture() with
        {
            GeneralSettings = config.Capture().GeneralSettings with { SiteTitle = "After" },
            ContentSettings = config.Capture().ContentSettings with { PostListPageSize = 7 },
            AppearanceSettings = new AppearanceSettings { ThemeId = 200 },
            CustomMenuSettings = new CustomMenuSettings { IsEnabled = true, Menus = [new Menu { Title = "New menu", Url = "/new" }] }
        });
        scenario.Resume.SetResult();
        var first = await pending;

        Assert.Contains("Before", first);
        Assert.Contains("Old menu", first);
        Assert.Contains("theme:100", first);
        Assert.DoesNotContain("New menu", first);
        Assert.Equal(2, scenario.SearchQueries[0].PageSize);

        var next = await client.GetStringAsync("/search?term=settings", ct);

        Assert.Contains("After", next);
        Assert.Contains("New menu", next);
        Assert.Contains("theme:200", next);
        Assert.DoesNotContain("theme:100", next);
        Assert.DoesNotContain("Old menu", next);
        Assert.Equal(7, scenario.SearchQueries[1].PageSize);
    }

    [Fact]
    public async Task Search_RendersResultsAndPreservesQueryStringState()
    {
        var scenario = new SearchPageScenario
        {
            Result = new SearchPostQueryResult(
                [
                    new PostDigest
                    {
                        Title = "Azure Functions on .NET",
                        Slug = "azure-functions",
                        ContentAbstract = "Building Azure Functions with Moonglade",
                        LangCode = "ja-jp",
                        PubDateUtc = new DateTime(2026, 2, 1, 12, 0, 0, DateTimeKind.Utc),
                        Tags =
                        [
                            new Tag { DisplayName = "Azure", NormalizedName = "azure" }
                        ]
                    }
                ],
                7)
        };
        using var app = await CreateTestApp(scenario);
        using var client = app.GetTestClient();

        var response = await client.GetAsync(
            "/search?term=Azure%20Functions&sort=title-desc&category=dotnet&tag=azure&language=ja-jp&startDate=2026-01-02&endDate=2026-02-03",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("noindex, nofollow", response.Headers.GetValues("X-Robots-Tag").Single());

        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("<h3 class=\"mb-4\">", html);
        Assert.Contains("<input id=\"search-term\" name=\"term\" value=\"Azure Functions\"", html);
        Assert.Contains("value=\"title-desc\" selected=\"selected\"", html);
        Assert.Contains("value=\"dotnet\" selected=\"selected\"", html);
        Assert.Contains("value=\"azure\" selected=\"selected\"", html);
        Assert.Contains("value=\"ja-jp\" selected=\"selected\"", html);
        Assert.Contains("id=\"startDate\" name=\"startDate\" value=\"2026-01-02\"", html);
        Assert.Contains("id=\"endDate\" name=\"endDate\" value=\"2026-02-03\"", html);
        Assert.Contains("7 results", html);
        Assert.Contains("<mark>Azure</mark> <mark>Functions</mark> on .NET", html);

        var pageTwoLink = GetHrefForPage(html, 2);
        Assert.Contains("term=Azure%20Functions", pageTwoLink);
        Assert.Contains("p=2", pageTwoLink);
        Assert.Contains("sort=title-desc", pageTwoLink);
        Assert.Contains("category=dotnet", pageTwoLink);
        Assert.Contains("tag=azure", pageTwoLink);
        Assert.Contains("language=ja-jp", pageTwoLink);
        Assert.Contains("startDate=2026-01-02", pageTwoLink);
        Assert.Contains("endDate=2026-02-03", pageTwoLink);

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
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("https://cdn.example.com/avatar.png", "")]
    [InlineData(null, "/blog")]
    [InlineData("", "/blog")]
    [InlineData("https://cdn.example.com/avatar.png", "/blog")]
    public async Task PublicAndAdminAvatarRendering_AlwaysUsesLocalEndpointWithoutChangingLegacySettings(string? avatar, string pathBase)
    {
        var config = CreateBlogConfig();
        config.Publish(config.Capture() with
        {
            GeneralSettings = config.Capture().GeneralSettings with
            {
                AvatarUrl = avatar,
                WidgetsProfile = true,
                SideBarOption = SideBarOption.Right
            }
        });
        var retained = config.Capture();
        using var app = await CreateTestApp(new SearchPageScenario(), config, pathBase);
        using var client = app.GetTestClient();
        var expected = $"{pathBase}/avatar";

        foreach (var route in new[] { "/search?term=avatar", "/admin/settings/general" })
        {
            var html = await client.GetStringAsync(route, TestContext.Current.CancellationToken);

            Assert.Contains($"src=\"{expected}\"", html);
            if (!string.IsNullOrEmpty(avatar)) Assert.DoesNotContain($"src=\"{avatar}\"", html);
            Assert.Same(retained, config.Capture());
            Assert.Equal(avatar, config.Capture().GeneralSettings.AvatarUrl);
        }
    }

    private static async Task<WebApplication> CreateTestApp(SearchPageScenario scenario, BlogConfig? config = null, string pathBase = "")
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

        var cultures = new[] { "en-US", "zh-Hans", "zh-Hant", "de-DE", "ja-JP" }
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
        builder.Services.AddInMemoryCacheAside();
        builder.Services.AddSingleton<ThemeCacheInvalidator>();
        builder.Services.AddSingleton(CreateQueryMediator(scenario).Object);

        var app = builder.Build();
        app.Use((context, next) =>
        {
            context.Request.PathBase = pathBase;
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
            Description = "Test blog",
            MetaKeyword = "moonglade",
            OwnerName = "Admin",
            OwnerEmail = "admin@example.com",
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
            .ReturnsAsync((GetSiteThemeStyleSheetQuery query, QueryMediationSettings _, CancellationToken _) => $"/* theme:{query.Id} */");
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
            feature.Controllers.Add(typeof(AssetsController).GetTypeInfo());
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

    private static string GetHrefForPage(string html, int pageNumber)
    {
        var match = Regex.Match(
            html,
            $"""<a class="page-link" href="(?<href>[^"]+)">{pageNumber}</a>""");

        Assert.True(match.Success, $"Could not find a page link for page {pageNumber}.");
        return WebUtility.HtmlDecode(match.Groups["href"].Value);
    }

    private sealed class SearchPageScenario
    {
        public SearchPostQueryResult Result { get; init; } = new([], 0);
        public List<SearchPostQuery> SearchQueries { get; } = [];
        public TaskCompletionSource? Started { get; init; }
        public TaskCompletionSource? Resume { get; init; }
    }
}
