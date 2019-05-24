using System;
using System.Threading.Tasks;
using System.Web;
using System.Data.SqlClient;
using CommandLine;
using Dapper;
using System.Linq;

namespace PostAbstractReset
{
    class Options
    {
        [Option('c', Required = true, HelpText = "SQL Server Connection String")]
        public string ConnectionString { get; set; }

        [Option('w', Required = true, HelpText = "Word Count")]
        public int WordCount { get; set; }
    }

    class Program
    {
        public static Options Options { get; set; }

        static async Task Main(string[] args)
        {
            var parserResult = Parser.Default.ParseArguments<Options>(args);
            if (parserResult.Tag == ParserResultType.Parsed)
            {
                Options = ((Parsed<Options>)parserResult).Value;

                Console.WriteLine("Press any key to start.");
                Console.ReadKey();
                WriteMessage($"Connecting to database.", ConsoleColor.Gray);

                int itemsAffected = 0;
                using (var conn = new SqlConnection(Options.ConnectionString))
                {
                    WriteMessage($"Connected to database.", ConsoleColor.Gray);

                    var sqlSelect = "SELECT Id, PostContent FROM Post p";
                    var postInfoList = await conn.QueryAsync<(Guid Id, string PostContent)>(sqlSelect);
                    var dic = postInfoList.ToDictionary(c => c.Id, c => c.PostContent);
                    WriteMessage($"Found {dic.Count} post(s).", ConsoleColor.Yellow);

                    foreach (var item in dic)
                    {
                        var newAbstract = GetPostAbstract(HttpUtility.HtmlDecode(item.Value), Options.WordCount);
                        var sql = "UPDATE Post SET ContentAbstract = @contentAbstract WHERE Id = @id";
                        await conn.ExecuteAsync(sql, new { contentAbstract = newAbstract, id = item.Key });
                        WriteMessage($"Updated Post Id: {item.Key}.", ConsoleColor.Green);
                        itemsAffected++;
                    }
                }
                WriteMessage($"Done, {itemsAffected} item(s) affected.", ConsoleColor.Green);
                Console.ReadKey();
            }
        }

        static void WriteMessage(string message, ConsoleColor color = ConsoleColor.White, bool resetColor = true)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            if (resetColor)
            {
                Console.ResetColor();
            }
        }

        static string GetPostAbstract(string rawHtmlContent, int wordCount)
        {
            var plainText = RemoveTags(rawHtmlContent);
            var result = plainText.Ellipsize(wordCount);
            return result;
        }

        static string RemoveTags(string html, bool htmlDecode = false)
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

            if (htmlDecode)
            {
                stringResult = HttpUtility.HtmlDecode(stringResult);
            }

            return stringResult;
        }

    }

    public static class StringExtensions
    {
        public static string Ellipsize(this string text, int characterCount)
        {
            return text.Ellipsize(characterCount, "\u00A0\u2026");
        }

        public static string Ellipsize(this string text, int characterCount, string ellipsis, bool wordBoundary = false)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "";

            if (characterCount < 0 || text.Length <= characterCount)
                return text;

            // search beginning of word
            var backup = characterCount;
            while (characterCount > 0 && text[characterCount - 1].IsLetter())
            {
                characterCount--;
            }

            // search previous word
            while (characterCount > 0 && text[characterCount - 1].IsSpace())
            {
                characterCount--;
            }

            // if it was the last word, recover it, unless boundary is requested
            if (characterCount == 0 && !wordBoundary)
            {
                characterCount = backup;
            }

            var trimmed = text.Substring(0, characterCount);
            return trimmed + ellipsis;
        }

        public static bool IsLetter(this char c)
        {
            return ('A' <= c && c <= 'Z') || ('a' <= c && c <= 'z');
        }

        public static bool IsSpace(this char c)
        {
            return (c == '\r' || c == '\n' || c == '\t' || c == '\f' || c == ' ');
        }
    }
}
