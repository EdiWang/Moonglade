using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Moonglade.Web.Extensions;
using Moonglade.Web.Tests.TestSupport;
using System.Collections;
using System.Globalization;
using System.Net;
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

    [Fact]
    public void FrenchMessages_RenderTranslationsAndPreserveLiteralConfigurationTokens()
    {
        var culture = CultureInfo.GetCultureInfo("fr-FR");
        var expectedTranslations = new Dictionary<string, string>
        {
            ["Home"] = "Accueil",
            ["Blog title"] = "Titre du blog",
            ["Blog overview"] = "Vue d'ensemble du blog",
            ["Comments"] = "Commentaires",
            ["Tags"] = "Étiquettes",
            ["Search"] = "Rechercher",
            ["Save"] = "Enregistrer",
            ["Sign In"] = "Se connecter",
            ["Default language"] = "Langue par défaut",
            ["Menu Editor"] = "Éditeur de menus",
            ["Edit Menu Item"] = "Modifier l'élément du menu",
            ["Activity Logs"] = "Journal d'activité",
            ["System"] = "Système",
            ["Post automatically published"] = "Article publié automatiquement",
            ["Email Notifications"] = "Notifications par e-mail",
            ["Upload Image"] = "Téléverser une image",
            ["Watermark"] = "Filigrane",
            ["Disabled"] = "Désactivé"
        };

        foreach (var (key, expected) in expectedTranslations)
            Assert.Equal(expected, Resources.GetString(key, culture));

        Assert.Equal("Programmé pour : 10/11/2026 09:00",
            Format("Scheduled for: {0}", "10/11/2026 09:00"));
        Assert.Equal("Si vous laissez ce champ vide, les 200 premiers caractères serviront de résumé.",
            Format("The first {0} character(s) will be used as abstract if you leave this field blank.", 200));
        Assert.Equal("Le compte local n'est pas sécurisé. Il est recommandé d'utiliser OIDC pour l'authentification. Cliquez sur settings pour configurer cette option.",
            Format("Local account is not secure. It's recommended to use {0} for authentication. Click {1} to configure.",
                "OIDC", "settings"));
        Assert.Contains("`OriginalFileSystemPath`",
            Resources.GetString("Original images are stored in the private `OriginalFileSystemPath` location.", culture));
        Assert.Contains("appsettings.json",
            Resources.GetString("Please configure email provider settings in appsettings.json.", culture));
        var copyright = Resources.GetString("Use [c] for copyright mark: &copy;, [year] for current year.", culture);
        Assert.NotNull(copyright);
        Assert.Contains("[c]", copyright);
        Assert.Contains("&copy;", copyright);
        Assert.Contains("[year]", copyright);

        string Format(string key, params object[] arguments) =>
            string.Format(culture, Resources.GetString(key, culture) ?? throw new InvalidOperationException(key), arguments);
    }

    [Theory]
    [InlineData("header", "fr-FR|fr-FR|Enregistrer|12,50|09/11/2026")]
    [InlineData("cookie", "fr-FR|fr-FR|Enregistrer|12,50|09/11/2026")]
    [InlineData("query", "fr-FR|fr-FR|Enregistrer|12,50|09/11/2026")]
    [InlineData("default", "en-US|en-US|Save|12.50|11/9/2026")]
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
        await using var app = builder.Build();
        app.UseMoongladeRequestPipeline(cultures);
        app.Run(context =>
        {
            var localizer = context.RequestServices.GetRequiredService<IStringLocalizer<Program>>();
            return context.Response.WriteAsync(
                $"{CultureInfo.CurrentCulture.Name}|{CultureInfo.CurrentUICulture.Name}|{localizer["Save"]}|{12.5m:N2}|{new DateTime(2026, 11, 9):d}", ct);
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
    public async Task AdminPages_RenderFrenchAndKeepApostrophesOutOfMenuExpressions()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fixture = await ActivityLogHttpFixture.CreateAsync(historyCount: 0, ct: ct);
        using var client = fixture.App.GetTestClient();
        client.DefaultRequestHeaders.Add("X-B1-User", "admin");
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("fr-FR");

        var menu = WebUtility.HtmlDecode(await client.GetStringAsync("/admin/menu", ct));
        Assert.Contains("data-edit-title=\"Modifier l'élément du menu\"", menu);
        Assert.Contains("data-add-title=\"Ajouter un élément au menu\"", menu);
        Assert.Contains("x-text=\"editingIndex !== null ? $el.dataset.editTitle : $el.dataset.addTitle\"", menu);
        Assert.Contains("data-edit-title=\"Modifier l'élément du sous-menu\"", menu);
        Assert.Contains("data-add-title=\"Ajouter un élément au sous-menu\"", menu);
        Assert.Contains("x-text=\"editingSubIndex !== null ? $el.dataset.editTitle : $el.dataset.addTitle\"", menu);

        var mentions = WebUtility.HtmlDecode(await client.GetStringAsync("/admin/mention", ct));
        Assert.Contains("`Supprimer la sélection (${selectedIds.length})`", mentions);
        Assert.Contains("<span x-text=\"mentionCount\"></span> élément(s)", mentions);

        var tags = WebUtility.HtmlDecode(await client.GetStringAsync("/admin/tags", ct));
        Assert.Contains(":title=\"`${tagItem.postCount} articles`\"", tags);
        Assert.Contains(":aria-label=\"`Modifier l'étiquette`", tags);
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
