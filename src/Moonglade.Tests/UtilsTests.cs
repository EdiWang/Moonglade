using System;
using System.Linq;
using Moonglade.Core;
using NUnit.Framework;

namespace Moonglade.Tests
{
    [TestFixture]
    public class UtilsTests
    {
        [Test]
        public void TestAppVersion()
        {
            var ver = Utils.AppVersion;
            Assert.IsNotNull(ver);
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

        [TestCase("https://dot.net/955", ExpectedResult = true)]
        [TestCase("https://edi.wang", ExpectedResult = true)]
        [TestCase("http://javato.net", ExpectedResult = true)]
        [TestCase("http://996.icu", Utils.UrlScheme.Http, ExpectedResult = true)]
        [TestCase("http://996.icu", Utils.UrlScheme.Https, ExpectedResult = false)]
        [TestCase("a quick brown fox jumped over the lazy dog.", ExpectedResult = false)]
        [TestCase("http://a\\b", ExpectedResult = false)]
        public bool TestIsValidUrl(string str, Utils.UrlScheme urlScheme = Utils.UrlScheme.All)
        {
            return str.IsValidUrl(urlScheme);
        }

        [Test]
        public void TestIsValidUrlUnknownSchema()
        {
            Assert.Throws(typeof(ArgumentOutOfRangeException), () =>
            {
                "https://996.icu".IsValidUrl((Utils.UrlScheme)4);
            });
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

        [TestCase("", "")]
        [TestCase("", " ")]
        [TestCase(" ", "")]
        [TestCase(" ", " ")]
        public void TestCombineUrlEmptyOrWhitespace(string url, string path)
        {
            Assert.Throws(typeof(ArgumentNullException), () =>
            {
                var result = Utils.CombineUrl(url, path);
            });
        }

        [TestCase("https://edi.wang", null, ExpectedResult = "https://edi.wang/")]
        [TestCase("https://edi.wang", "", ExpectedResult = "https://edi.wang/")]
        [TestCase("https://edi.wang", " ", ExpectedResult = "https://edi.wang/")]
        [TestCase("https://edi.wang", "/", ExpectedResult = "https://edi.wang/")]
        [TestCase("https://edi.wang", "//", ExpectedResult = "")]
        [TestCase("https://edi.wang", "/996", ExpectedResult = "https://edi.wang/996")]
        [TestCase("https://edi.wang", "996", ExpectedResult = "https://edi.wang/996")]
        [TestCase("https://edi.wang", "996/007/251/404", ExpectedResult = "https://edi.wang/996/007/251/404")]
        [TestCase("https://edi.wang/dotnet", "1055", ExpectedResult = "https://edi.wang/1055")]
        [TestCase("", "", ExpectedResult = "")]
        public string TestResolveCanonicalUrl(string prefix, string path)
        {
            var result = Utils.ResolveCanonicalUrl(prefix, path);
            return result;
        }

        [Test]
        public void TestResolveCanonicalUrlInvalid()
        {
            Assert.Throws<UriFormatException>(() =>
            {
                Utils.ResolveCanonicalUrl("996ICU", "251");
            });
        }

        [TestCase("")]
        [TestCase("DC1")]
        [TestCase("DC1,DC2")]
        [TestCase("DC1, DC2")]
        [TestCase("DC1, DC2,DC3")]
        [TestCase("DC[1], DC-2, DC#3, DC@4, DC$5, DC(6), DC/7")]
        public void TestGetEnvironmentTagsValid(string tags)
        {
            Environment.SetEnvironmentVariable("MOONGLADE_TAGS", tags, EnvironmentVariableTarget.Process);
            var envTags = Utils.GetEnvironmentTags();
            Assert.IsNotNull(envTags);

            var list = tags.Split(',').Select(p => p.Trim());
            foreach (var tag in list)
            {
                Assert.IsTrue(envTags.Contains(tag));
            }
        }

        [TestCase("DC%1")]
        [TestCase("DC 1")]
        [TestCase("DC*1")]
        [TestCase("DC^1")]
        [TestCase("DC^1")]
        [TestCase("DC+1")]
        [TestCase("DC=1")]
        [TestCase("DC!1")]
        [TestCase("DC`1")]
        [TestCase("DC~1")]
        [TestCase("DC'1")]
        [TestCase("DC?1")]
        [TestCase("DC{1}")]
        public void TestGetEnvironmentTagsInvalid(string invalidTag)
        {
            Environment.SetEnvironmentVariable("MOONGLADE_TAGS", $"DC1, DC2, {invalidTag}", EnvironmentVariableTarget.Process);
            var envTags = Utils.GetEnvironmentTags();
            Assert.IsNotNull(envTags);

            Assert.IsTrue(envTags.Count() == 2);
            Assert.IsTrue(!envTags.Contains(invalidTag));
        }
    }
}
