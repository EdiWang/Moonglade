using System;
using System.Linq;
using System.Runtime.InteropServices;
using Moonglade.Core;
using NUnit.Framework;

namespace Moonglade.Tests
{
    [TestFixture]
    public class UtilsTests
    {
        [TestCase(1, ExpectedResult = "January")]
        [TestCase(2, ExpectedResult = "February")]
        [TestCase(3, ExpectedResult = "March")]
        [TestCase(4, ExpectedResult = "April")]
        [TestCase(5, ExpectedResult = "May")]
        [TestCase(6, ExpectedResult = "June")]
        [TestCase(7, ExpectedResult = "July")]
        [TestCase(8, ExpectedResult = "August")]
        [TestCase(9, ExpectedResult = "September")]
        [TestCase(10, ExpectedResult = "October")]
        [TestCase(11, ExpectedResult = "November")]
        [TestCase(12, ExpectedResult = "December")]
        [TestCase(-128, ExpectedResult = "")]
        [TestCase(128, ExpectedResult = "")]
        public string TestGetMonthNameByNumber(int number)
        {
            return Utils.GetMonthNameByNumber(number);
        }

        [TestCase(null, ExpectedResult = null)]
        [TestCase("", ExpectedResult = "")]
        [TestCase(" ", ExpectedResult = " ")]
        [TestCase("996", ExpectedResult = "996")]
        [TestCase("[c] 2020 edi.wang", ExpectedResult = "&copy; 2020 edi.wang")]
        public string TestFormatCopyright2Html(string copyrightCode)
        {
            return Utils.FormatCopyright2Html(copyrightCode);
        }

        // Valid Urls
        [TestCase("http://996.icu", ExpectedResult = "http://996.icu")]
        [TestCase("https://996.icu", ExpectedResult = "https://996.icu")]
        [TestCase("https://996.icu/", ExpectedResult = "https://996.icu/")]
        [TestCase("https://996.icu/work996", ExpectedResult = "https://996.icu/work996")]
        [TestCase("https://996.icu/#", ExpectedResult = "https://996.icu/#")]
        [TestCase("https://996.icu/#icu", ExpectedResult = "https://996.icu/#icu")]
        [TestCase("https://996.icu/996#icu", ExpectedResult = "https://996.icu/996#icu")]
        [TestCase("https://996.icu/why?reason=fubao", ExpectedResult = "https://996.icu/why?reason=fubao")]
        [TestCase("https://13.107.21.200", ExpectedResult = "https://13.107.21.200")]
        [TestCase("/", ExpectedResult = "/")]
        [TestCase("/996/icu", ExpectedResult = "/996/icu")]
        [TestCase("/996/icu?reason=fubao", ExpectedResult = "/996/icu?reason=fubao")]
        [TestCase("https://internalsystem/996/icu", ExpectedResult = "https://internalsystem/996/icu")]
        // Invalid Urls
        [TestCase("https://localhost", ExpectedResult = "#")]
        [TestCase("https://localhost:996", ExpectedResult = "#")]
        [TestCase("http://localhost/icu", ExpectedResult = "#")]
        [TestCase("https://192.168.0.1", ExpectedResult = "#")]
        [TestCase("https://127.0.0.1", ExpectedResult = "#")]
        [TestCase("https://10.0.0.1", ExpectedResult = "#")]
        [TestCase("192.168.0.1", ExpectedResult = "#")]
        [TestCase("../", ExpectedResult = "#")]
        [TestCase("./", ExpectedResult = "#")]
        [TestCase("//", ExpectedResult = "#")]
        [TestCase(@"/\", ExpectedResult = "#")]
        [TestCase("", ExpectedResult = "#")]
        [TestCase(" ", ExpectedResult = "#")]
        [TestCase(null, ExpectedResult = "#")]
        [TestCase("javascript:;", ExpectedResult = "#")]
        [TestCase("javascript:while(true){alert('fuck')};", ExpectedResult = "#")]
        [TestCase("blob:https://996.icu/fubao", ExpectedResult = "#")]
        public string TestSterilizeMenuLink(string rawUrl)
        {
            return Utils.SterilizeMenuLink(rawUrl);
        }

        [TestCase("127.0.0.1", ExpectedResult = true)]
        [TestCase("192.168.0.1", ExpectedResult = true)]
        [TestCase("10.0.0.1", ExpectedResult = true)]
        [TestCase("172.16.0.1", ExpectedResult = true)]
        [TestCase("172.31.0.1", ExpectedResult = true)]
        [TestCase("172.22.0.1", ExpectedResult = true)]
        [TestCase("172.251.0.1", ExpectedResult = false)]
        [TestCase("4.2.2.1", ExpectedResult = false)]
        public bool TestIsPrivateIP(string ip)
        {
            return Utils.IsPrivateIP(ip);
        }

        [Test]
        public void TestFormatCopyright2HtmlHappyPathWithYear()
        {
            string org = "[c] 2009 - [year] edi.wang";
            string exp = $"&copy; 2009 - {DateTime.UtcNow.Year} edi.wang";
            var result = Utils.FormatCopyright2Html(org);

            Assert.AreEqual(result, exp);
        }

        [Test]
        public void TestGetThemes()
        {
            var themes = Utils.GetThemes();
            Assert.IsTrue(themes.All(t => t.Value.EndsWith(".css")));
        }

        [Test]
        public void TestLeft()
        {
            const string str = "Microsoft Rocks!";
            var left = Utils.Left(str, 9);
            Assert.IsTrue(left == "Microsoft");
        }

        [Test]
        public void TestRight()
        {
            var str = "996 Sucks!";
            var left = Utils.Right(str, 6);
            Assert.IsTrue(left == "Sucks!");
        }

        [TestCase(".NET Core", ExpectedResult = "dotnet-core")]
        [TestCase("C#", ExpectedResult = "csharp")]
        [TestCase("955", ExpectedResult = "955")]
        public string TestNormalizeTagName(string str)
        {
            return Utils.NormalizeTagName(str);
        }

        [TestCase("C", ExpectedResult = true)]
        [TestCase("C++", ExpectedResult = true)]
        [TestCase("C#", ExpectedResult = true)]
        [TestCase("Java", ExpectedResult = true)]
        [TestCase("996", ExpectedResult = true)]
        [TestCase(".NET", ExpectedResult = true)]
        [TestCase("C Sharp", ExpectedResult = true)]
        [TestCase("Cup<T>", ExpectedResult = false)]
        [TestCase("(1)", ExpectedResult = false)]
        [TestCase("usr/bin", ExpectedResult = false)]
        public bool TestValidateTagName(string tagDisplayName)
        {
            return Utils.ValidateTagName(tagDisplayName);
        }

        [Test]
        public void TryParseBase64Success()
        {
            var ok = Utils.TryParseBase64("xDgItVa0ujLKxGsoMV1+MmxBrpo997mXbeXngqIx13o=", out var base64);
            Assert.IsTrue(ok);
            Assert.IsNotNull(base64);
        }

        [Test]
        public void TryParseBase64Fail()
        {
            var ok = Utils.TryParseBase64("Learn Java and work 996!", out var base64);
            Assert.IsFalse(ok);
            Assert.IsNull(base64);
        }

        [Test]
        public void TestLazyLoadToImgTag()
        {
            const string html = @"<p>Work 996 and have some fu bao!</p><img src=""icu.jpg"" /><video src=""java996.mp4""></video>";
            var result = Utils.AddLazyLoadToImgTag(html);
            Assert.IsTrue(result == @"<p>Work 996 and have some fu bao!</p><img loading=""lazy"" src=""icu.jpg"" /><video src=""java996.mp4""></video>");
        }

        [Test]
        public void TestLazyLoadToImgTagExistLoading()
        {
            const string html = @"<p>Work 996 and have some fu bao!</p><img loading=""lazy"" src=""icu.jpg"" /><video src=""java996.mp4""></video>";
            var result = Utils.AddLazyLoadToImgTag(html);
            Assert.IsTrue(result == @"<p>Work 996 and have some fu bao!</p><img loading=""lazy"" src=""icu.jpg"" /><video src=""java996.mp4""></video>");
        }

        [Test]
        public void TestMdContentToHtml()
        {
            var md = "A quick brown **fox** jumped over the lazy dog.";
            var result = Utils.ConvertMarkdownContent(md, Utils.MarkdownConvertType.Html);

            Assert.IsTrue(result == "<p>A quick brown <strong>fox</strong> jumped over the lazy dog.</p>\n");
        }

        [Test]
        [Platform(Include = "Win")]
        public void TestResolveImageStoragePathValidAbsolute()
        {
            var contentRootPath = @"C:\Moonglade";
            var path = @"C:\MoongladeData\Uploads";

            var finalPath = Utils.ResolveImageStoragePath(contentRootPath, path);
            Assert.IsTrue(finalPath == @"C:\MoongladeData\Uploads");
        }

        [Test]
        [Platform(Include = "Win")]
        public void TestResolveImageStoragePathValidRelative()
        {
            var contentRootPath = @"C:\Moonglade";
            var path = @"${basedir}\Uploads";

            var finalPath = Utils.ResolveImageStoragePath(contentRootPath, path);
            Assert.IsTrue(finalPath == @"C:\Moonglade\Uploads");
        }

        [Test]
        [Platform(Include = "Win")]
        public void TestResolveImageStoragePathInvalidRelative()
        {
            var contentRootPath = @"C:\Moonglade";
            var path = @"..\${basedir}\Uploads";

            Assert.Catch<NotSupportedException>(() => { Utils.ResolveImageStoragePath(contentRootPath, path); });
        }

        [Test]
        [Platform(Include = "Win")]
        public void TestResolveImageStoragePathInvalidChar()
        {
            var contentRootPath = @"C:\Moonglade";
            var path = @"${basedir}\Uploads<>|foo";

            Assert.Catch<InvalidOperationException>(() => { Utils.ResolveImageStoragePath(contentRootPath, path); });
        }

        [TestCase("")]
        [TestCase(" ")]
        [TestCase(null)]
        [Platform(Include = "Win")]
        public void TestResolveImageStoragePathEmptyParameter(string path)
        {
            var contentRootPath = @"C:\Moonglade";
            Assert.Catch<ArgumentNullException>(() => { Utils.ResolveImageStoragePath(contentRootPath, path); });
        }

        [Test]
        public void TestRemoveTags()
        {
            var html = @"<p>Microsoft</p><p>Rocks!</p><p>Azure <br /><img src=""a.jpg"" /> The best <span>cloud</span>!</p>";
            var output = Utils.RemoveTags(html);

            Assert.IsTrue(output == "MicrosoftRocks!Azure  The best cloud!");
        }

        [Test]
        public void TestRemoveScriptTagFromHtml()
        {
            var html = @"<p>Microsoft</p><p>Rocks!</p><p>Azure <br /><script>console.info('hey');</script><img src=""a.jpg"" /> The best <span>cloud</span>!</p>";
            var output = Utils.RemoveScriptTagFromHtml(html);

            Assert.IsTrue(output == @"<p>Microsoft</p><p>Rocks!</p><p>Azure <br /><img src=""a.jpg"" /> The best <span>cloud</span>!</p>");
        }

        [Test]
        public void TestRemoveWhiteSpaceFromStylesheets()
        {
            var css = @"h1 {
                            color: red;
                        }";
            var output = Utils.RemoveWhiteSpaceFromStylesheets(css);
            Assert.IsTrue(output == "h1{color:red}");
        }

        [TestCase('f', ExpectedResult = true)]
        [TestCase('0', ExpectedResult = false)]
        public bool TestIsLetter(char c)
        {
            return c.IsLetter();
        }

        [TestCase(' ', ExpectedResult = true)]
        [TestCase('0', ExpectedResult = false)]
        [TestCase('a', ExpectedResult = false)]
        [TestCase('A', ExpectedResult = false)]
        public bool TestIsSpace(char c)
        {
            return c.IsSpace();
        }

        [TestCase("A 996 programmer went to heaven.", ExpectedResult = "A 996" + "\u00A0\u2026")]
        [TestCase("Fu bao", ExpectedResult = "Fu bao")]
        public string TestEllipsize(string str)
        {
            return str.Ellipsize(10);
        }

        [Test]
        public void TestGetPostAbstract()
        {
            var html = @"<p>Microsoft</p> <p>Rocks!</p><p>Azure <br /><img src=""a.jpg"" /> The best <span>cloud</span>!</p>";
            var result = Utils.GetPostAbstract(html, 16);
            var expected = "Microsoft Rocks!" + "\u00A0\u2026";
            Assert.IsTrue(result == expected);
        }

        [Test]
        public void TestGetSlugInfoFromPostUrlSuccess()
        {
            var url = "https://edi.wang/post/2075/4/4/happy-birthday-to-microsoft";
            var result = Utils.GetSlugInfoFromPostUrl(url);
            Assert.IsTrue(result.IsSuccess);
            Assert.IsNotNull(result.Item);
            Assert.IsTrue(result.Item.Slug == "happy-birthday-to-microsoft");
            Assert.IsTrue(result.Item.PubDate == new DateTime(2075, 4, 4));
        }

        [Test]
        public void TestGetSlugInfoFromPostUrlFail()
        {
            var url = "https://edi.wang/996-workers-ganked-alibaba";
            var result = Utils.GetSlugInfoFromPostUrl(url);
            Assert.IsFalse(result.IsSuccess);
            Assert.IsNull(result.Item.Slug);
            Assert.IsTrue(result.Item.PubDate == DateTime.MinValue);
        }

        [TestCase("https://dot.net/955", ExpectedResult = true)]
        [TestCase("https://edi.wang", ExpectedResult = true)]
        [TestCase("http://javato.net", ExpectedResult = true)]
        [TestCase("a quick brown fox jumped over the lazy dog.", ExpectedResult = false)]
        [TestCase("http://a\\b", ExpectedResult = false)]
        public bool TestIsValidUrl(string str)
        {
            return str.IsValidUrl();
        }

        [TestCase("https://996.icu", ExpectedResult = true)]
        [TestCase("http://996.rip", ExpectedResult = false)]
        public bool TestIsValidUrlHttps(string str)
        {
            return str.IsValidUrl(Utils.UrlScheme.Https);
        }

        [TestCase("https://996.icu", ExpectedResult = false)]
        [TestCase("http://996.rip", ExpectedResult = true)]
        public bool TestIsValidUrlHttp(string str)
        {
            return str.IsValidUrl(Utils.UrlScheme.Http);
        }

        [TestCase("http://usejava.com/996/", "icu.png", ExpectedResult = "http://usejava.com/996/icu.png")]
        [TestCase("https://dot.net/", "955.png", ExpectedResult = "https://dot.net/955.png")]
        [TestCase("https://mayun.lie", "fubao.png", ExpectedResult = "https://mayun.lie/fubao.png")]
        [TestCase("http://996.icu/", "/reject/fubao", ExpectedResult = "http://996.icu/reject/fubao")]
        [TestCase("http://996.rip", "/dont-use-java", ExpectedResult = "http://996.rip/dont-use-java")]
        public string TestCombineUrl(string url, string path)
        {
            var result = Utils.CombineUrl(url, path);
            return result;
        }
    }
}
