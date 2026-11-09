using Microsoft.AspNetCore.TestHost;
using Moonglade.Web.Tests.TestSupport;
using System.Collections;
using System.Globalization;
using System.Net;
using System.Resources;
using System.Text;
using System.Text.RegularExpressions;

namespace Moonglade.Web.Tests.Localization;

public class TraditionalChineseLocalizationTests
{
    private static readonly ResourceManager Resources =
        new("Moonglade.Web.Resources.Program", typeof(Program).Assembly);

    [Theory]
    [InlineData("zh-Hant")]
    [InlineData("zh-HK")]
    [InlineData("zh-TW")]
    public void HongKongTerminology_ResolvesForTraditionalChineseCultures(string cultureName)
    {
        var culture = CultureInfo.GetCultureInfo(cultureName);
        var expectedTranslations = new Dictionary<string, string>
        {
            ["Blog title"] = "網誌標題",
            ["Blog overview"] = "網誌概覽",
            ["Email"] = "電郵",
            ["Email Notifications"] = "電郵通知",
            ["Test email is queued."] = "測試電郵已加入隊列。",
            ["User Name"] = "用戶名稱",
            ["Account"] = "帳戶",
            ["Comments"] = "留言",
            ["Reply"] = "回覆",
            ["Save"] = "儲存",
            ["Search"] = "搜尋",
            ["Sign In"] = "登入",
            ["Default language"] = "預設語言",
            ["Menu Editor"] = "功能表編輯器",
            ["Add Menu Item"] = "新增功能表項目",
            ["Schedule Publish"] = "定時發佈",
            ["Scheduled Publish Time"] = "預定發佈時間",
            ["Post published"] = "文章已發佈",
            ["Post automatically published"] = "文章已自動發佈",
            ["Watermark"] = "水印",
            ["Watermark opacity"] = "水印不透明度",
            ["Show comment section on mobile screens"] = "在流動裝置上顯示留言區",
            ["Current Endpoint:"] = "目前端點：",
            ["Upload Image"] = "上傳圖片",
            ["Disabled"] = "已停用"
        };

        foreach (var (key, expected) in expectedTranslations)
        {
            Assert.Equal(expected, Resources.GetString(key, culture));
        }
    }

    [Fact]
    public void ResourceCatalog_ContainsAllKeysAndPreservesFormatArguments()
    {
        var reference = GetResourceEntries("zh-Hans");
        var traditional = GetResourceEntries("zh-Hant");

        Assert.NotEmpty(reference);
        Assert.Equal(reference.Keys.Order(StringComparer.Ordinal), traditional.Keys.Order(StringComparer.Ordinal));

        foreach (var (key, value) in traditional)
        {
            Assert.False(string.IsNullOrWhiteSpace(value), $"Empty Traditional Chinese translation: {key}");
            var sourceFormat = CompositeFormat.Parse(key);
            var translatedFormat = CompositeFormat.Parse(value);
            Assert.Equal(sourceFormat.MinimumArgumentCount, translatedFormat.MinimumArgumentCount);
            Assert.Equal(GetArgumentIndices(key), GetArgumentIndices(value));
        }
    }

    [Fact]
    public void FormattedMessages_RenderTranslatedTextWithoutLosingArguments()
    {
        var culture = CultureInfo.GetCultureInfo("zh-Hant");
        var scheduled = Resources.GetString("Scheduled for: {0}", culture);
        var abstractHint = Resources.GetString(
            "The first {0} character(s) will be used as abstract if you leave this field blank.", culture);
        var authenticationHint = Resources.GetString(
            "Local account is not secure. It's recommended to use {0} for authentication. Click {1} to configure.",
            culture);

        Assert.NotNull(scheduled);
        Assert.NotNull(abstractHint);
        Assert.NotNull(authenticationHint);
        Assert.Equal("預定發佈時間：2026-11-10 09:00", string.Format(culture, scheduled, "2026-11-10 09:00"));
        Assert.Equal("如留空，會自動選取文章首 200 個字元作為摘要。", string.Format(culture, abstractHint, 200));
        Assert.Equal("本機帳戶不夠安全。建議使用 OIDC 進行身份驗證。按下 settings 進行設定。",
            string.Format(culture, authenticationHint, "OIDC", "settings"));
    }

    [Fact]
    public async Task AdminPages_RenderHongKongTranslationsForSelectionAndPostCounts()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var fixture = await ActivityLogHttpFixture.CreateAsync(historyCount: 0, ct: ct);
        using var client = fixture.App.GetTestClient();
        client.DefaultRequestHeaders.Add("X-B1-User", "admin");
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("zh-Hant");

        using var mentionsResponse = await client.GetAsync("/admin/mention", ct);
        Assert.Equal(HttpStatusCode.OK, mentionsResponse.StatusCode);
        var mentions = WebUtility.HtmlDecode(await mentionsResponse.Content.ReadAsStringAsync(ct));
        Assert.Contains("`刪除已選取項目 (${selectedIds.length})`", mentions);
        Assert.Contains("<span x-text=\"mentionCount\"></span> 項", mentions);

        using var tagsResponse = await client.GetAsync("/admin/tags", ct);
        Assert.Equal(HttpStatusCode.OK, tagsResponse.StatusCode);
        var tags = WebUtility.HtmlDecode(await tagsResponse.Content.ReadAsStringAsync(ct));
        Assert.Contains(":title=\"`${tagItem.postCount} 篇文章`\"", tags);
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
