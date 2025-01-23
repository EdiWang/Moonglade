using Edi.ChinaDetector;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Configuration;
using Microsoft.Win32;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Moonglade.Utils;

public static class Helper
{
    public static string AppVersionBasic
    {
        get
        {
            var asm = Assembly.GetEntryAssembly();
            if (null == asm) return "N/A";

            var fileVersion = asm.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version;
            return fileVersion;
        }
    }

    public static string AppVersion
    {
        get
        {
            var asm = Assembly.GetEntryAssembly();
            if (null == asm) return "N/A";

            // e.g. 11.2.0.0
            var fileVersion = asm.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version;

            // e.g. 11.2-preview+e57ab0321ae44bd778c117646273a77123b6983f
            var version = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            if (!string.IsNullOrWhiteSpace(version) && version.IndexOf('+') > 0)
            {
                var gitHash = version[(version.IndexOf('+') + 1)..]; // e57ab0321ae44bd778c117646273a77123b6983f
                var prefix = version[..version.IndexOf('+')]; // 11.2-preview

                if (gitHash.Length <= 6) return version;

                // consider valid hash
                var gitHashShort = gitHash[..6];
                return !string.IsNullOrWhiteSpace(gitHashShort) ? $"{prefix} ({gitHashShort})" : fileVersion;
            }

            return version ?? fileVersion;
        }
    }

    public static void SetAppDomainData(string key, object value)
    {
        AppDomain.CurrentDomain.SetData(key, value);
    }

    public static T GetAppDomainData<T>(string key, T defaultValue = default(T))
    {
        object data = AppDomain.CurrentDomain.GetData(key);
        if (data == null)
        {
            return defaultValue;
        }

        return (T)data;
    }

    public static bool IsNonStableVersion()
    {
        string pattern = @"\b(preview|beta|rc|debug|alpha|test|canary|nightly)\b";
        Regex regex = new Regex(pattern, RegexOptions.IgnoreCase);

        return regex.IsMatch(AppVersion);
    }

    // Get `sec-ch-prefers-color-scheme` header value
    // This is to enhance user experience by stopping the screen from blinking when switching pages
    public static bool UseServerSideDarkMode(IConfiguration configuration, HttpContext context)
    {
        bool useServerSideDarkMode = false;
        bool usePrefersColorSchemeHeader = configuration.GetSection("PrefersColorSchemeHeader:Enabled").Get<bool>();
        var prefersColorScheme = context.Request.Headers[configuration["PrefersColorSchemeHeader:HeaderName"]!];
        if (usePrefersColorSchemeHeader && prefersColorScheme == "dark")
        {
            useServerSideDarkMode = true;
        }

        return useServerSideDarkMode;
    }

    public static async Task<bool> IsRunningInChina()
    {
        // Learn more at https://go.edi.wang/aka/os251
        var service = new OfflineChinaDetectService();
        var result = await service.Detect(DetectionMethod.TimeZone | DetectionMethod.Culture | DetectionMethod.Behavior);
        return result.Rank >= 1;
    }

    public static string GetRouteLinkFromUrl(string url)
    {
        var blogSlugRegex = new Regex(@"^https?:\/\/.*\/post\/(?<yyyy>\d{4})\/(?<MM>\d{1,12})\/(?<dd>\d{1,31})\/(?<slug>.*)$");
        Match match = blogSlugRegex.Match(url);
        if (!match.Success)
        {
            throw new FormatException("Invalid Slug Format");
        }

        string yyyy = match.Groups["yyyy"].Value;
        string mm = match.Groups["MM"].Value;
        string dd = match.Groups["dd"].Value;
        string slug = match.Groups["slug"].Value;

        return $"{yyyy}/{mm}/{dd}/{slug}".ToLower();
    }

    private static readonly Regex UrlsRegex = new(
        @"<a.*?href=[""'](?<url>.*?)[""'].*?>(?<name>.*?)</a>", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static IEnumerable<Uri> GetUrlsFromContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentNullException(content);
        }

        var urlsList = new List<Uri>();
        foreach (var url in
                 UrlsRegex.Matches(content).Select(myMatch => myMatch.Groups["url"].ToString().Trim()))
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                urlsList.Add(uri);
            }
        }

        return urlsList;
    }

    public static bool IsRunningOnAzureAppService() => !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME"));

    public static bool IsRunningInDocker() => Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";

    public static string GetClientIP(HttpContext context) => context?.Connection.RemoteIpAddress?.ToString();

    public static string TryGetFullOSVersion()
    {
        var osVer = Environment.OSVersion;
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return osVer.VersionString;

        try
        {
            var currentVersion = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            if (currentVersion != null)
            {
                var name = currentVersion.GetValue("ProductName", "Microsoft Windows NT");
                var ubr = currentVersion.GetValue("UBR", string.Empty).ToString();
                if (!string.IsNullOrWhiteSpace(ubr))
                {
                    return $"{name} {osVer.Version.Major}.{osVer.Version.Minor}.{osVer.Version.Build}.{ubr}";
                }
            }
        }
        catch
        {
            return osVer.VersionString;
        }

        return osVer.VersionString;
    }

    public static string GetDNSPrefetchUrl(string cdnEndpoint)
    {
        if (string.IsNullOrWhiteSpace(cdnEndpoint)) return string.Empty;

        var uri = new Uri(cdnEndpoint);
        return $"{uri.Scheme}://{uri.Host}/";
    }

    // https://docs.microsoft.com/en-us/aspnet/core/security/data-protection/consumer-apis/password-hashing?view=aspnetcore-6.0
    // This is not secure, but better than nothing.
    public static string HashPassword(string clearPassword, string saltBase64)
    {
        var salt = Convert.FromBase64String(saltBase64);

        // derive a 256-bit subkey (use HMACSHA256 with 100,000 iterations)
        string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
            password: clearPassword!,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 100000,
            numBytesRequested: 256 / 8));

        return hashed;
    }

    public static string GenerateSalt()
    {
        // Generate a 128-bit salt using a sequence of cryptographically strong random bytes.
        byte[] salt = RandomNumberGenerator.GetBytes(128 / 8); // divide by 8 to convert bits to bytes
        return Convert.ToBase64String(salt);
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

    public static string SterilizeLink(string rawUrl)
    {
        bool IsUnderLocalSlash()
        {
            // Allows "/" or "/foo" but not "//" or "/\".
            if (rawUrl[0] == '/')
            {
                // url is exactly "/"
                if (rawUrl.Length == 1)
                {
                    return true;
                }

                // url doesn't start with "//" or "/\"
                return rawUrl[1] is not '/' and not '\\';
            }

            return false;
        }

        string invalidReturn = "#";
        if (string.IsNullOrWhiteSpace(rawUrl))
        {
            return invalidReturn;
        }

        if (!rawUrl.IsValidUrl())
        {
            return IsUnderLocalSlash() ? rawUrl : invalidReturn;
        }

        var uri = new Uri(rawUrl);
        if (uri.IsLoopback)
        {
            // localhost, 127.0.0.1
            return invalidReturn;
        }

        if (uri.HostNameType == UriHostNameType.IPv4)
        {
            // Disallow LAN IP (e.g. 192.168.0.1, 10.0.0.1)
            if (IsPrivateIP(uri.Host))
            {
                return invalidReturn;
            }
        }

        return rawUrl;
    }

    public static string ResolveCanonicalUrl(string prefix, string path)
    {
        if (string.IsNullOrWhiteSpace(prefix)) return string.Empty;
        path ??= string.Empty;

        if (!prefix.IsValidUrl())
        {
            throw new UriFormatException($"Prefix '{prefix}' is not a valid URL.");
        }

        var prefixUri = new Uri(prefix);
        return Uri.TryCreate(baseUri: prefixUri, relativeUri: path, out var newUri) ?
            newUri.ToString() :
            string.Empty;
    }

    /// <summary>
    /// Test an IPv4 address is LAN or not.
    /// </summary>
    /// <param name="ip">IPv4 address</param>
    /// <returns>bool</returns>
    public static bool IsPrivateIP(string ip) => IPAddress.Parse(ip).GetAddressBytes() switch
    {
        // Regex.IsMatch(ip, @"(^127\.)|(^10\.)|(^172\.1[6-9]\.)|(^172\.2[0-9]\.)|(^172\.3[0-1]\.)|(^192\.168\.)")
        // Regex has bad performance, this is better

        var x when x[0] is 192 && x[1] is 168 => true,
        var x when x[0] is 10 => true,
        var x when x[0] is 127 => true,
        var x when x[0] is 172 && x[1] is >= 16 and <= 31 => true,
        _ => false
    };

    public static string FormatCopyright2Html(string copyrightCode)
    {
        if (string.IsNullOrWhiteSpace(copyrightCode))
        {
            return copyrightCode;
        }

        var result = copyrightCode.Replace("[c]", "&copy;")
            .Replace("[year]", DateTime.UtcNow.Year.ToString());
        return result;
    }

    public static bool TryParseBase64(string input, out byte[] base64Array)
    {
        base64Array = null;

        if (string.IsNullOrWhiteSpace(input) ||
            input.Length % 4 != 0 ||
            !Regex.IsMatch(input, @"^[a-zA-Z0-9\+/]*={0,3}$", RegexOptions.None))
        {
            return false;
        }

        try
        {
            base64Array = Convert.FromBase64String(input);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    /// <summary>
    /// Get values from `MOONGLADE_TAGS` Environment Variable
    /// </summary>
    /// <returns>string values</returns>
    public static IEnumerable<string> GetEnvironmentTags()
    {
        var tagsEnv = Environment.GetEnvironmentVariable("MOONGLADE_TAGS");
        if (string.IsNullOrWhiteSpace(tagsEnv))
        {
            yield return string.Empty;
            yield break;
        }

        var tagRegex = new Regex(@"^[a-zA-Z0-9-#@$()\[\]/]+$");
        var tags = tagsEnv.Split(',');
        foreach (string tag in tags)
        {
            var t = tag.Trim();
            if (tagRegex.IsMatch(t))
            {
                yield return t;
            }
        }
    }

    public static bool IsValidEmailAddress(string value)
    {
        if (value == null)
        {
            return true;
        }

        var regEx = CreateEmailRegEx();
        if (regEx != null)
        {
            return regEx.Match(value).Length > 0;
        }

        int atCount = value.Count(c => c == '@');

        return (atCount == 1
                && value[0] != '@'
                && value[^1] != '@');
    }

    private static Regex CreateEmailRegEx()
    {
        const string pattern = @"^((([a-z]|\d|[!#\$%&'\*\+\-\/=\?\^_`{\|}~]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])+(\.([a-z]|\d|[!#\$%&'\*\+\-\/=\?\^_`{\|}~]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])+)*)|((\x22)((((\x20|\x09)*(\x0d\x0a))?(\x20|\x09)+)?(([\x01-\x08\x0b\x0c\x0e-\x1f\x7f]|\x21|[\x23-\x5b]|[\x5d-\x7e]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(\\([\x01-\x09\x0b\x0c\x0d-\x7f]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF]))))*(((\x20|\x09)*(\x0d\x0a))?(\x20|\x09)+)?(\x22)))@((([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])([a-z]|\d|-|\.|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])*([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])))\.)+(([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])([a-z]|\d|-|\.|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])*([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])))\.?$";
        const RegexOptions options = RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture;

        // Set explicit regex match timeout, sufficient enough for email parsing
        // Unless the global REGEX_DEFAULT_MATCH_TIMEOUT is already set
        var matchTimeout = TimeSpan.FromSeconds(2);

        try
        {
            return new(pattern, options, matchTimeout);
        }
        catch
        {
            // Fallback on error
        }

        // Legacy fallback (without explicit match timeout)
        return new(pattern, options);
    }

    public static bool IsValidTagName(string tagDisplayName)
    {
        if (string.IsNullOrWhiteSpace(tagDisplayName)) return false;

        // Regex performance best practice
        // See https://docs.microsoft.com/en-us/dotnet/standard/base-types/best-practices

        const string pattern = @"^[a-zA-Z 0-9\.\-\+\#\s]*$";
        var isEng = Regex.IsMatch(tagDisplayName, pattern);
        if (isEng) return true;

        // https://docs.microsoft.com/en-us/dotnet/standard/base-types/character-classes-in-regular-expressions#supported-named-blocks
        const string chsPattern = @"\p{IsCJKUnifiedIdeographs}";
        var isChs = Regex.IsMatch(tagDisplayName, chsPattern);

        return isChs;
    }

    public static string CombineErrorMessages(this ModelStateDictionary modelStateDictionary, string sep = ", ")
    {
        var messages = GetErrorMessagesFromModelState(modelStateDictionary);
        var enumerable = messages as string[] ?? messages.ToArray();
        return enumerable.Any() ? string.Join(sep, enumerable) : string.Empty;
    }

    public static IEnumerable<string> GetErrorMessagesFromModelState(ModelStateDictionary modelStateDictionary)
    {
        if (modelStateDictionary is null) return null;
        if (modelStateDictionary.ErrorCount == 0) return null;

        return from modelState in modelStateDictionary.Values
               from error in modelState.Errors
               select error.ErrorMessage;
    }

    public static void ValidatePagingParameters(int pageSize, int pageIndex)
    {
        if (pageSize is < 1 or > 1024)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize),
                $"{nameof(pageSize)} out of range, current value: {pageSize}.");
        }

        if (pageIndex is < 1 or > 1024)
        {
            throw new ArgumentOutOfRangeException(nameof(pageIndex),
                $"{nameof(pageIndex)} out of range, current value: {pageIndex}.");
        }
    }

    public static Dictionary<string, string> TagNormalizationDictionary => new()
    {
        { ".", "-" },
        { "#", "-sharp" },
        { " ", "-" },
        { "+", "-plus" }
    };

    public static string NormalizeName(string orgTagName, IDictionary<string, string> normalizations)
    {
        var isEnglishName = Regex.IsMatch(orgTagName, @"^[a-zA-Z 0-9\.\-\+\#\s]*$");
        if (isEnglishName)
        {
            // special case
            if (orgTagName.Equals(".net", StringComparison.OrdinalIgnoreCase))
            {
                return "dot-net";
            }

            var result = new StringBuilder(orgTagName);
            foreach (var (key, value) in normalizations)
            {
                result.Replace(key, value);
            }
            return result.ToString().ToLower();
        }

        var bytes = Encoding.Unicode.GetBytes(orgTagName);
        var hexArray = bytes.Select(b => $"{b:x2}");
        var hexName = string.Join('-', hexArray);

        return hexName;
    }

    public static bool IsValidHeaderName(string headerName)
    {
        if (string.IsNullOrEmpty(headerName))
        {
            return false;
        }

        // Check if header name conforms to the standard which allows:
        // - Any ASCII character from 'a' to 'z' and 'A' to 'Z'
        // - Digits from '0' to '9'
        // - Special characters: '!', '#', '$', '%', '&', ''', '*', '+', '-', '.', '^', '_', '`', '|', '~'
        return headerName.All(c =>
            c is >= 'a' and <= 'z' ||
            c is >= 'A' and <= 'Z' ||
            c is >= '0' and <= '9' ||
            c == '!' || c == '#' || c == '$' || c == '%' || c == '&' || c == '\'' ||
            c == '*' || c == '+' || c == '-' || c == '.' || c == '^' || c == '_' ||
            c == '`' || c == '|' || c == '~');
    }

    public static string GetMagic(int value, int start, int end) =>
        Convert.ToBase64String(SHA256.HashData(BitConverter.GetBytes(value)))[start..end];
}