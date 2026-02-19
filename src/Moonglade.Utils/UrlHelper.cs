using Edi.AspNetCore.Utils;
using Microsoft.AspNetCore.Http;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Moonglade.Utils;

public static partial class UrlHelper
{
    public static string GetRouteLinkFromUrl(string url)
    {
        Match match = BlogSlugRegex().Match(url);
        if (!match.Success)
        {
            throw new FormatException("Invalid Slug Format");
        }

        string yyyy = match.Groups["yyyy"].Value;
        string mm = match.Groups["MM"].Value;
        string dd = match.Groups["dd"].Value;
        string slug = match.Groups["slug"].Value;

        // validate month and day
        if (!int.TryParse(mm, out int month) || month < 1 || month > 12)
        {
            throw new FormatException("Invalid Slug Format");
        }

        if (!int.TryParse(dd, out int day) || day < 1 || day > DateTime.DaysInMonth(int.Parse(yyyy), month))
        {
            throw new FormatException("Invalid Slug Format");
        }

        return $"{yyyy}/{mm}/{dd}/{slug}".ToLower();
    }

    [GeneratedRegex(@"^https?:\/\/.*\/post\/(?<yyyy>\d{4})\/(?<MM>\d{1,12})\/(?<dd>\d{1,31})\/(?<slug>.*)$")]
    private static partial Regex BlogSlugRegex();

    [GeneratedRegex(@"<a.*?href=[""'](?<url>.*?)[""'].*?>(?<name>.*?)</a>", RegexOptions.IgnoreCase)]
    private static partial Regex UrlsRegex();

    public static IEnumerable<Uri> GetUrlsFromContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentNullException(content);
        }

        var urlsList = new List<Uri>();
        foreach (var url in
                 UrlsRegex().Matches(content).Select(myMatch => myMatch.Groups["url"].ToString().Trim()))
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                if (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                {
                    urlsList.Add(uri);
                }
            }
        }

        return urlsList;
    }

    public static string GenerateRouteLink(DateTime publishDate, string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            throw new ArgumentNullException(nameof(slug), "Slug must not be null or empty.");
        }

        return $"{publishDate.ToString("yyyy/M/d", CultureInfo.InvariantCulture)}/{slug.ToLower()}";
    }

    public static string GetDNSPrefetchUrl(string cdnEndpoint)
    {
        if (string.IsNullOrWhiteSpace(cdnEndpoint)) return string.Empty;

        var uri = new Uri(cdnEndpoint);

        if (!uri.IsAbsoluteUri || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return string.Empty;
        }

        if (uri.Scheme == Uri.UriSchemeHttp && uri.Port != 80 ||
            uri.Scheme == Uri.UriSchemeHttps && uri.Port != 443)
        {
            return $"{uri.Scheme}://{uri.Host}:{uri.Port}/";
        }

        return $"{uri.Scheme}://{uri.Host}/";
    }

    public static string ResolveRootUrl(HttpContext ctx, string canonicalPrefix, bool preferCanonical = false, bool removeTailSlash = false)
    {
        if (ctx is null && !preferCanonical)
        {
            throw new ArgumentNullException(nameof(ctx), "HttpContext must not be null when preferCanonical is 'false'");
        }

        var url = preferCanonical ?
            ResolveCanonicalUrl(canonicalPrefix, string.Empty) :
            $"{ctx.Request.Scheme}://{ctx.Request.Host}";

        if (removeTailSlash && url.EndsWith('/'))
        {
            return url.TrimEnd('/');
        }

        return url;
    }

    public static string ResolveCanonicalUrl(string prefix, string path)
    {
        if (string.IsNullOrWhiteSpace(prefix)) return string.Empty;
        path ??= string.Empty;

        // Check if path contains space or invalid characters
        if (path.IndexOfAny([' ', '\t', '\n', '\r']) >= 0 || path.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
        {
            return string.Empty;
        }

        if (!prefix.IsValidUrl())
        {
            throw new UriFormatException($"Prefix '{prefix}' is not a valid URL.");
        }

        var prefixUri = new Uri(prefix);
        return Uri.TryCreate(baseUri: prefixUri, relativeUri: path, out var newUri) ?
            newUri.ToString() :
            string.Empty;
    }
}
