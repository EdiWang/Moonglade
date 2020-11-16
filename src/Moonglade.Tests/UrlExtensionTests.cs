using System;
using System.Diagnostics.CodeAnalysis;
using Moonglade.Core;
using NUnit.Framework;

namespace Moonglade.Tests
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class UrlExtensionTests
    {
        [TestCase("https://dot.net/955", ExpectedResult = true)]
        [TestCase("https://edi.wang", ExpectedResult = true)]
        [TestCase("http://javato.net", ExpectedResult = true)]
        [TestCase("http://996.icu", UrlExtension.UrlScheme.Http, ExpectedResult = true)]
        [TestCase("http://996.icu", UrlExtension.UrlScheme.Https, ExpectedResult = false)]
        [TestCase("a quick brown fox jumped over the lazy dog.", ExpectedResult = false)]
        [TestCase("http://a\\b", ExpectedResult = false)]
        public bool TestIsValidUrl(string str, UrlExtension.UrlScheme urlScheme = UrlExtension.UrlScheme.All)
        {
            return str.IsValidUrl(urlScheme);
        }

        [Test]
        public void TestIsValidUrlUnknownSchema()
        {
            Assert.Throws(typeof(ArgumentOutOfRangeException), () =>
            {
                "https://996.icu".IsValidUrl((UrlExtension.UrlScheme)4);
            });
        }

        [TestCase("https://996.icu", ExpectedResult = true)]
        [TestCase("http://996.rip", ExpectedResult = false)]
        public bool TestIsValidUrlHttps(string str)
        {
            return str.IsValidUrl(UrlExtension.UrlScheme.Https);
        }

        [TestCase("https://996.icu", ExpectedResult = false)]
        [TestCase("http://996.rip", ExpectedResult = true)]
        public bool TestIsValidUrlHttp(string str)
        {
            return str.IsValidUrl(UrlExtension.UrlScheme.Http);
        }

        [TestCase("https://996.icu", ExpectedResult = true)]
        [TestCase("http://996.rip", ExpectedResult = true)]
        public bool IsValidUrl_All(string str)
        {
            return str.IsValidUrl(Utils.UrlScheme.All);
        }

        [TestCase("http://usejava.com/996/", "icu.png", ExpectedResult = "http://usejava.com/996/icu.png")]
        [TestCase("https://dot.net/", "955.png", ExpectedResult = "https://dot.net/955.png")]
        [TestCase("https://mayun.lie", "fubao.png", ExpectedResult = "https://mayun.lie/fubao.png")]
        [TestCase("http://996.icu/", "/reject/fubao", ExpectedResult = "http://996.icu/reject/fubao")]
        [TestCase("http://996.rip", "/dont-use-java", ExpectedResult = "http://996.rip/dont-use-java")]
        public string TestCombineUrl(string url, string path)
        {
            var result = url.CombineUrl(path);
            return result;
        }

        [TestCase("", "")]
        [TestCase("", " ")]
        [TestCase(" ", "")]
        [TestCase(" ", " ")]
        public void TestCombineUrlEmptyOrWhitespace(string url, string path)
        {
            Assert.Throws(typeof(ArgumentNullException), () =>
            {
                var result = url.CombineUrl(path);
            });
        }
    }
}
