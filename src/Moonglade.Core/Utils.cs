using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Markdig;

namespace Moonglade.Core
{
    public static class Utils
    {
        public static string AppVersion =>
            Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;

        public static string ResolveImageStoragePath(string contentRootPath, string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            var basedirStr = "${basedir}"; // Do not use "." because there could be "." in path.
            if (path.IndexOf(basedirStr, StringComparison.Ordinal) > 0)
            {
                throw new NotSupportedException($"{basedirStr} can only be at the beginning.");
            }
            if (path.IndexOf(basedirStr, StringComparison.Ordinal) == 0)
            {
                // Use relative path
                // Warning: Write data under application directory may blow up on Azure App Services when WEBSITE_RUN_FROM_PACKAGE = 1, which set the directory read-only.
                path = path.Replace(basedirStr, contentRootPath);
            }

            // IsPathFullyQualified can't check if path is valid, e.g.:
            // Path: C:\Documents<>|foo
            //   Rooted: True
            //   Fully qualified: True
            //   Full path: C:\Documents<>|foo
            var invalidChars = Path.GetInvalidPathChars();
            if (path.IndexOfAny(invalidChars) >= 0)
            {
                throw new InvalidOperationException("Path can not contain invalid chars.");
            }
            if (!Path.IsPathFullyQualified(path))
            {
                throw new InvalidOperationException("Path is not fully qualified.");
            }

            var fullPath = Path.GetFullPath(path);
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
            }
            return fullPath;
        }

        public static DateTime UtcToZoneTime(DateTime utcTime, int timeZone)
        {
            return utcTime.AddHours(timeZone);
        }

        public static string GetPostAbstract(string rawHtmlContent, int wordCount)
        {
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(rawHtmlContent);
            var plainText = htmlDoc.DocumentNode.InnerText;
            var result = Left(plainText, wordCount);
            return result;
        }

        public static string Left(string sSource, int iLength)
        {
            return sSource.Substring(0, iLength > sSource.Length ? sSource.Length : iLength);
        }

        public static string Right(string sSource, int iLength)
        {
            return sSource.Substring(iLength > sSource.Length ? 0 : sSource.Length - iLength);
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

        private static readonly Tuple<string, string>[] TagNormalizeSourceTable =
        {
            Tuple.Create(".", "dot"),
            Tuple.Create("#", "sharp"),
            Tuple.Create("<", "lt"),
            Tuple.Create(">", "gt"),
            Tuple.Create("@", "at"),
            Tuple.Create("$", "dollar"),
            Tuple.Create("*", "asterisk"),
            Tuple.Create("(", "lbrackets"),
            Tuple.Create(")", "rbrackets"),
            Tuple.Create("{", "lbraces"),
            Tuple.Create("}", "rbraces"),
            Tuple.Create(" ", "-"),
            Tuple.Create("+", "-and-"),
            Tuple.Create("=", "-equals-")
        };

        public static string NormalizeTagName(string orgTagName)
        {
            return ReplaceWithStringBuilder(orgTagName, TagNormalizeSourceTable).ToLower();
        }

        private static string ReplaceWithStringBuilder(string value, IEnumerable<Tuple<string, string>> toReplace)
        {
            var result = new StringBuilder(value);
            foreach (var (item1, item2) in toReplace)
            {
                result.Replace(item1, item2);
            }
            return result.ToString();
        }

        public static string ReplaceImgSrc(string rawHtmlContent)
        {
            // Replace ONLY IMG tag's src to data-src
            // Otherwise embedded videos will blow up

            if (string.IsNullOrWhiteSpace(rawHtmlContent)) return rawHtmlContent;
            var imgSrcRegex = new Regex("<img.+?(src)=[\"'](.+?)[\"'].+?>");
            var newStr = imgSrcRegex.Replace(rawHtmlContent, match => match.Value.Replace("src", "data-src"));
            return newStr;
        }

        public static string MdContentToHtml(string markdown)
        {
            var pipeline = new MarkdownPipelineBuilder().DisableHtml().Build();
            var result = Markdown.ToHtml(markdown, pipeline);
            return result;
        }
    }
}
