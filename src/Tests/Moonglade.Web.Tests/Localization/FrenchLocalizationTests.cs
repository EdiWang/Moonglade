using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moonglade.Auth;
using Moonglade.Web.Extensions;
using Moonglade.Web.Tests.TestSupport;
using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Text.RegularExpressions;

namespace Moonglade.Web.Tests.Localization;

public class FrenchLocalizationTests
{
    private static readonly ResourceManager Resources =
        new("Moonglade.Web.Resources.Program", typeof(Program).Assembly);

    [Fact]
    public void ResourceCatalog_ContainsAllKeysAndPreservesFormatArguments()
    {
        var reference = GetResourceEntries("zh-Hans");
        var french = GetResourceEntries("fr-FR");

        Assert.NotEmpty(reference);
        Assert.Equal(reference.Keys.Order(StringComparer.Ordinal), french.Keys.Order(StringComparer.Ordinal));

        foreach (var (key, value) in french)
        {
            Assert.False(string.IsNullOrWhiteSpace(value), $"Empty French translation: {key}");
            Assert.Equal(CompositeFormat.Parse(key).MinimumArgumentCount,
                CompositeFormat.Parse(value).MinimumArgumentCount);
            Assert.Equal(GetArgumentIndices(key), GetArgumentIndices(value));
        }
    }

    [Theory]
    [InlineData("header", "fr-FR|fr-FR|12,50|09/11/2026")]
    [InlineData("cookie", "fr-FR|fr-FR|12,50|09/11/2026")]
    [InlineData("query", "fr-FR|fr-FR|12,50|09/11/2026")]
    [InlineData("default", "en-US|en-US|12.50|11/9/2026")]
    public async Task ProductionPipeline_SelectsFrenchAndPreservesEnglishDefault(string source, string expected)
    {
        var ct = TestContext.Current.CancellationToken;
        var getCultures = typeof(Program).GetMethod("GetSupportedCultures", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(getCultures);
        var cultures = Assert.IsType<List<CultureInfo>>(getCultures.Invoke(null, null));
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });
        builder.Configuration.Sources.Clear();
        builder.Logging.ClearProviders();
        builder.WebHost.UseTestServer();
        builder.Services.AddRouting();
        builder.Services.AddProblemDetails();
        builder.Services.AddAuthentication();
        builder.Services.AddAuthorization();
        builder.Services.AddRateLimiter();
        builder.Services.AddResponseCompression();
        builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
        await using var accountFixture = new LocalAccountPageFixture(required: false);
        await accountFixture.InitializeAsync();
        builder.Services.AddSingleton(accountFixture.Accounts);
        builder.Services.Configure<AuthenticationSettings>(options => options.Totp.Required = false);
        await using var app = builder.Build();
        app.UseMoongladeRequestPipeline(cultures);
        app.Run(context =>
        {
            return context.Response.WriteAsync(
                $"{CultureInfo.CurrentCulture.Name}|{CultureInfo.CurrentUICulture.Name}|{12.5m:N2}|{new DateTime(2026, 11, 9):d}", ct);
        });
        await app.StartAsync(ct);
        using var client = app.GetTestClient();
        client.BaseAddress = new Uri("https://localhost");
        if (source == "header") client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("fr-FR");
        if (source == "cookie")
        {
            client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US");
            client.DefaultRequestHeaders.Add("Cookie",
                $"{CookieRequestCultureProvider.DefaultCookieName}={CookieRequestCultureProvider.MakeCookieValue(new RequestCulture("fr-FR"))}");
        }

        var path = source == "query" ? "/?culture=fr-FR&ui-culture=fr-FR" : "/";
        Assert.Equal(expected, await client.GetStringAsync(path, ct));
    }

    [Fact]
    public async Task AdminMenu_LocalizedLabelsRemainDataRatherThanJavaScriptSource()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fixture = await ActivityLogHttpFixture.CreateAsync(historyCount: 0, ct: ct);
        using var client = fixture.App.GetTestClient();
        client.DefaultRequestHeaders.Add("X-B1-User", "admin");
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("fr-FR");

        var menu = await client.GetStringAsync("/admin/menu", ct);
        Assert.Contains("x-text=\"editingIndex !== null ? $el.dataset.editTitle : $el.dataset.addTitle\"", menu);
        Assert.Contains("x-text=\"editingSubIndex !== null ? $el.dataset.editTitle : $el.dataset.addTitle\"", menu);
    }

    private static Dictionary<string, string> GetResourceEntries(string cultureName)
    {
        var resourceSet = Resources.GetResourceSet(CultureInfo.GetCultureInfo(cultureName), true, false);
        Assert.NotNull(resourceSet);
        return resourceSet.Cast<DictionaryEntry>().ToDictionary(
            entry => Assert.IsType<string>(entry.Key),
            entry => Assert.IsType<string>(entry.Value),
            StringComparer.Ordinal);
    }

    private static string[] GetArgumentIndices(string format) =>
        Regex.Matches(format, @"\{(\d+)(?:[,:][^}]*)?\}")
            .Select(match => match.Groups[1].Value)
            .Order(StringComparer.Ordinal)
            .ToArray();
}
