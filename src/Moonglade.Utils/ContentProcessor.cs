using Markdig;
using System.Text;
using System.Web;
using System.Xml.Linq;

namespace Moonglade.Utils;

public static class ContentProcessor
{
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

    public static string GetPostAbstract(string content, int wordCount, bool useMarkdown = false)
    {
        var plainText = useMarkdown ?
            MarkdownToContent(content, MarkdownConvertType.Text) :
            RemoveTags(content);

        var decodedText = HtmlDecode(plainText);
        var result = decodedText.Ellipsize(wordCount);
        return result;
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

    public static string Ellipsize(this string text, int characterCount)
    {
        return text.Ellipsize(characterCount, "\u00A0\u2026");
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

    private static string Ellipsize(this string text, int characterCount, string ellipsis, bool wordBoundary = false)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "";

        if (characterCount < 0 || text.Length <= characterCount)
            return text + ellipsis;

        // search beginning of word
        var backup = characterCount;
        while (characterCount > 0 && text[characterCount - 1].IsLetter())
        {
            characterCount--;
        }

        // search previous word, but preserve at least one space
        var spaceCount = 0;
        while (characterCount > 0 && text[characterCount - 1].IsSpace())
        {
            characterCount--;
            spaceCount++;
        }

        // if we found spaces, add one back to preserve word separation
        if (spaceCount > 0 && characterCount < backup)
        {
            characterCount++;
        }

        // if it was the last word, recover it, unless boundary is requested
        if (characterCount == 0 && !wordBoundary)
        {
            characterCount = backup;
        }

        var trimmed = text[..characterCount];
        return trimmed + ellipsis;
    }

    #endregion
}