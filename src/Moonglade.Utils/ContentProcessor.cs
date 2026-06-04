using Markdig;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml.Linq;

namespace Moonglade.Utils;

public static class ContentProcessor
{
    private static readonly Regex AnchorTagRegex = new("<a\\b(?<attrs>[^>]*)>", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    private static readonly Regex HrefAttributeRegex = new("\\s+href=\"(?<href>[^\"]*)\"", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    private static readonly Regex RelAttributeRegex = new("\\s+rel=\"[^\"]*\"", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    private static readonly Regex AttributeWhitespaceRegex = new("\\s+", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static string ReplaceCDNEndpointToImgTags(this string html, string endpoint)
    {
        if (string.IsNullOrWhiteSpace(html)) return html;

        endpoint = endpoint.TrimEnd('/');

        // Fast path: check if there are any potential matches
        if (!html.Contains("src=\"/image/", StringComparison.OrdinalIgnoreCase) &&
            !html.Contains("src='/image/", StringComparison.OrdinalIgnoreCase))
        {
            return html;
        }

        var span = html.AsSpan();
        var result = new StringBuilder(html.Length);
        var lastIndex = 0;

        // Process both double and single quotes
        var patterns = new[] { "src=\"/image/", "src='/image/" };
        var quotes = new[] { '"', '\'' };

        for (int i = 0; i < span.Length; i++)
        {
            for (int patternIndex = 0; patternIndex < patterns.Length; patternIndex++)
            {
                var pattern = patterns[patternIndex];
                var quote = quotes[patternIndex];

                if (i + pattern.Length <= span.Length &&
                    span.Slice(i, pattern.Length).SequenceEqual(pattern))
                {
                    var imgStart = span[..i].ToString().LastIndexOf("<img", StringComparison.OrdinalIgnoreCase);
                    if (imgStart == -1) continue;

                    var tagEnd = span[imgStart..].IndexOf('>');
                    if (tagEnd == -1 || imgStart + tagEnd <= i) continue;

                    // Add content up to this point
                    result.Append(span[lastIndex..i]);

                    // Add the CDN replacement
                    result.Append($"src={quote}{endpoint}/");

                    // Skip the original pattern
                    i += pattern.Length;
                    lastIndex = i;
                    break;
                }
            }
        }

        // Add remaining content
        if (lastIndex < span.Length)
        {
            result.Append(span[lastIndex..]);
        }

        return result.ToString();
    }

    // Fix #833 - umlauts like (ä,ö,ü). are not displayed correctly in the abstract
    public static string HtmlDecode(string content) => HttpUtility.HtmlDecode(content);

    public static string RemoveTags(string html)
    {
        if (string.IsNullOrEmpty(html))
        {
            return string.Empty;
        }

        try
        {
            var doc = XDocument.Parse(html);
            var result = doc.Root?.DescendantNodes().OfType<XText>().Aggregate("", (current, node) => current + node);

            return result?.Trim();
        }
        catch (Exception)
        {
            return RemoveTagsBackup(html);
        }
    }

    public static bool IsLetter(this char c) => c is >= 'a' and <= 'z' or >= 'A' and <= 'Z';

    public static bool IsSpace(this char c) => c is '\r' or '\n' or '\t' or '\f' or ' ';

    public static string MarkdownToContent(string markdown, MarkdownConvertType type, bool disableHtml = true)
    {
        var pipeline = new MarkdownPipelineBuilder()
            .UsePipeTables()
            .UseBootstrap();

        if (disableHtml)
        {
            pipeline.DisableHtml();
        }

        var result = type switch
        {
            MarkdownConvertType.None => markdown,
            MarkdownConvertType.Html => Markdown.ToHtml(markdown, pipeline.Build()),
            MarkdownConvertType.Text => Markdown.ToPlainText(markdown, pipeline.Build()),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

        return result;
    }

    public static string MarkdownToCommentHtml(string markdown)
    {
        var html = MarkdownToContent(markdown, MarkdownConvertType.Html);
        return SecureCommentLinks(html);
    }

    private static string SecureCommentLinks(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return html;
        }

        return AnchorTagRegex.Replace(html, match =>
        {
            var attributes = match.Groups["attrs"].Value;
            var hrefMatch = HrefAttributeRegex.Match(attributes);
            if (!hrefMatch.Success)
            {
                return match.Value;
            }

            var href = hrefMatch.Groups["href"].Value;
            var remainingAttributes = HrefAttributeRegex.Replace(attributes, "", 1);
            remainingAttributes = RelAttributeRegex.Replace(remainingAttributes, "");
            remainingAttributes = AttributeWhitespaceRegex.Replace(remainingAttributes, " ").Trim();

            return IsSafeCommentLink(href)
                ? BuildAnchorTag(href, remainingAttributes)
                : BuildAnchorTag(null, remainingAttributes);
        });
    }

    private static string BuildAnchorTag(string href, string attributes)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(href))
        {
            parts.Add($"href=\"{href}\"");
        }

        if (!string.IsNullOrWhiteSpace(attributes))
        {
            parts.Add(attributes);
        }

        if (!string.IsNullOrWhiteSpace(href))
        {
            parts.Add("rel=\"nofollow ugc noopener noreferrer\"");
        }

        return parts.Count == 0 ? "<a>" : $"<a {string.Join(' ', parts)}>";
    }

    private static bool IsSafeCommentLink(string href)
    {
        if (string.IsNullOrWhiteSpace(href))
        {
            return false;
        }

        var decodedHref = HttpUtility.HtmlDecode(href).Trim();
        if (decodedHref.StartsWith('/') || decodedHref.StartsWith('#'))
        {
            return true;
        }

        if (!Uri.TryCreate(decodedHref, UriKind.RelativeOrAbsolute, out var uri))
        {
            return false;
        }

        if (!uri.IsAbsoluteUri)
        {
            return true;
        }

        return uri.Scheme is "http" or "https" or "mailto";
    }

    public enum MarkdownConvertType
    {
        None = 0,
        Html = 1,
        Text = 2
    }

    public static string GetKeywords(string rawKeywords)
    {
        if (string.IsNullOrWhiteSpace(rawKeywords)) return null;

        var keywords = rawKeywords.Split(',')
            .Select(k => k.Trim())
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Distinct()
            .ToArray();

        if (keywords.Length == 0) return null;

        return string.Join(',', keywords);
    }

    #region Private Methods

    private static string RemoveTagsBackup(string html)
    {
        var result = new char[html.Length];

        var cursor = 0;
        var inside = false;
        foreach (var current in html)
        {
            switch (current)
            {
                case '<':
                    inside = true;
                    continue;
                case '>':
                    inside = false;
                    continue;
            }

            if (!inside)
            {
                result[cursor++] = current;
            }
        }

        var stringResult = new string(result, 0, cursor);

        return stringResult.Replace("&nbsp;", " ");
    }

    #endregion
}
