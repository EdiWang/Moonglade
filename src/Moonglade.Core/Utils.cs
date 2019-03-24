using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace Moonglade.Core
{
    public static class Utils
    {
        public static string AppVersion => 
            Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;

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
    }
}
