using System.Collections;
using System.Globalization;
using System.Resources;
using System.Text;
using System.Text.RegularExpressions;

namespace Moonglade.Web.Tests.Localization;

public class TraditionalChineseLocalizationTests
{
    private static readonly ResourceManager Resources =
        new("Moonglade.Web.Resources.Program", typeof(Program).Assembly);

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
