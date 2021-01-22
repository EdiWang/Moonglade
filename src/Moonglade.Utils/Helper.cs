using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace Moonglade.Utils
{
    public static class Helper
    {
        public static string AppVersion =>
            Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

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
                    var ubr = currentVersion.GetValue("UBR", string.Empty)?.ToString();
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

        public static async Task<string> GetThemeColorAsync(string webRootPath, string currentTheme)
        {
            var color = AppDomain.CurrentDomain.GetData("CurrentThemeColor")?.ToString();
            if (!string.IsNullOrWhiteSpace(color))
            {
                return color;
            }

            var cssPath = Path.Join(webRootPath, "css", "theme", currentTheme);
            if (File.Exists(cssPath))
            {
                var lines = await File.ReadAllLinesAsync(cssPath);
                var accentColorLine = lines.FirstOrDefault(l => l.Contains("accent-color1"));
                if (accentColorLine is not null)
                {
                    var regex = new Regex("#(?:[0-9a-f]{3}){1,2}");
                    var match = regex.Match(accentColorLine);
                    if (match.Success)
                    {
                        var colorHex = match.Captures[0].Value;
                        AppDomain.CurrentDomain.SetData("CurrentThemeColor", colorHex);
                        return colorHex;
                    }
                }
            }
            return "#FFFFFF";
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
                if (Helper.IsPrivateIP(uri.Host))
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
    }
}
