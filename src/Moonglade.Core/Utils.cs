using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Moonglade.Core
{
    public static class Utils
    {
        public static string AppVersion =>
            Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

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
                if (null != accentColorLine)
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

            var x when x[0] == 192 && x[1] == 168 => true,
            var x when x[0] == 10 => true,
            var x when x[0] == 127 => true,
            var x when x[0] == 172 && x[1] >= 16 && x[1] <= 31 => true,
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
    }
}
