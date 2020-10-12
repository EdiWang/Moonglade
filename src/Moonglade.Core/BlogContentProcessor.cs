using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Markdig;

namespace Moonglade.Core
{
    public class BlogContentProcessor
    {
        public static string AddLazyLoadToImgTag(string rawHtmlContent)
        {
            // Replace ONLY IMG tag's src to data-src
            // Otherwise embedded videos will blow up

            if (string.IsNullOrWhiteSpace(rawHtmlContent)) return rawHtmlContent;
            var imgSrcRegex = new Regex("<img.+?(src)=[\"'](.+?)[\"'].+?>");
            var newStr = imgSrcRegex.Replace(rawHtmlContent, match =>
            {
                if (!match.Value.Contains("loading"))
                {
                    return match.Value.Replace("src",
                        @"loading=""lazy"" src");
                }

                return match.Value;
            });
            return newStr;
        }

        public static string GetPostAbstract(string rawContent, int wordCount, bool useMarkdown = false)
        {
            var plainText = useMarkdown ?
                MarkdownToContent(rawContent, MarkdownConvertType.Text) :
                RemoveTags(rawContent);

            var result = plainText.Ellipsize(wordCount);
            return result;
        }

        public static string RemoveTags(string html)
        {
            if (string.IsNullOrEmpty(html))
            {
                return string.Empty;
            }

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
    }
}
