using System.Net;

namespace Moonglade.Utils;

public static class UrlExtension
{
    public enum UrlScheme
    {
        Http,
        Https,
        All
    }

    public static bool IsValidUrl(this string url, UrlScheme urlScheme = UrlScheme.All)
    {
        var isValidUrl = Uri.TryCreate(url, UriKind.Absolute, out var uriResult);
        if (!isValidUrl) return false;

        isValidUrl &= urlScheme switch
        {
            UrlScheme.All => uriResult.Scheme == Uri.UriSchemeHttps || uriResult.Scheme == Uri.UriSchemeHttp,
            UrlScheme.Https => uriResult.Scheme == Uri.UriSchemeHttps,
            UrlScheme.Http => uriResult.Scheme == Uri.UriSchemeHttp,
            _ => throw new ArgumentOutOfRangeException(nameof(urlScheme), urlScheme, null)
        };
        return isValidUrl;
    }

    public static string CombineUrl(this string url, string path)
    {
        if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentNullException();
        }

        url = url.Trim();
        path = path.Trim();

        return url.TrimEnd('/') + "/" + path.TrimStart('/');
    }

    public static bool IsLocalhostUrl(this Uri uri)
    {
        try
        {
            if (uri.IsLoopback)
            {
                // localhost, 127.0.0.1, [::1]
                return true;
            }

            // Get the local host name and compare it with the URL host
            string localHostName = Dns.GetHostName();
            if (uri.Host.Equals(localHostName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Get local IP addresses and compare them with the URL host
            IPAddress[] localIPs = Dns.GetHostAddresses(Dns.GetHostName());
            return localIPs.Any(addr => uri.Host.Equals(addr.ToString()));
        }
        catch (UriFormatException)
        {
            return false;
        }
    }
}