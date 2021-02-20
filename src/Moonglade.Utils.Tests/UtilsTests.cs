using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace Moonglade.Utils.Tests
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class UtilsTests
    {
        [Test]
        public void AppVersion()
        {
            var ver = Helper.AppVersion;
            Assert.IsNotNull(ver);
        }

        [TestCase(null, ExpectedResult = null)]
        [TestCase("", ExpectedResult = "")]
        [TestCase(" ", ExpectedResult = " ")]
        [TestCase("996", ExpectedResult = "996")]
        [TestCase("[c] 2020 edi.wang", ExpectedResult = "&copy; 2020 edi.wang")]
        public string FormatCopyright2Html(string copyrightCode)
        {
            return Helper.FormatCopyright2Html(copyrightCode);
        }

        [TestCase("127.0.0.1", ExpectedResult = true)]
        [TestCase("192.168.0.1", ExpectedResult = true)]
        [TestCase("10.0.0.1", ExpectedResult = true)]
        [TestCase("172.16.0.1", ExpectedResult = true)]
        [TestCase("172.31.0.1", ExpectedResult = true)]
        [TestCase("172.22.0.1", ExpectedResult = true)]
        [TestCase("172.251.0.1", ExpectedResult = false)]
        [TestCase("4.2.2.1", ExpectedResult = false)]
        public bool IsPrivateIP(string ip)
        {
            return Helper.IsPrivateIP(ip);
        }

        [Test]
        public void FormatCopyright2Html_HappyPathWithYear()
        {
            string org = "[c] 2009 - [year] edi.wang";
            string exp = $"&copy; 2009 - {DateTime.UtcNow.Year} edi.wang";
            var result = Helper.FormatCopyright2Html(org);

            Assert.AreEqual(result, exp);
        }

        [Test]
        public void ParseBase64_Success()
        {
            var ok = Helper.TryParseBase64("xDgItVa0ujLKxGsoMV1+MmxBrpo997mXbeXngqIx13o=", out var base64);
            Assert.IsTrue(ok);
            Assert.IsNotNull(base64);
        }

        [Test]
        public void ParseBase64_Fail()
        {
            var ok = Helper.TryParseBase64("Learn Java and work 996!", out var base64);
            Assert.IsFalse(ok);
            Assert.IsNull(base64);
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
        public string ResolveCanonicalUrl(string prefix, string path)
        {
            var result = Helper.ResolveCanonicalUrl(prefix, path);
            return result;
        }

        [Test]
        public void ResolveCanonicalUrl_Invalid()
        {
            Assert.Throws<UriFormatException>(() =>
            {
                Helper.ResolveCanonicalUrl("996ICU", "251");
            });
        }

        [TestCase("")]
        [TestCase("DC1")]
        [TestCase("DC1,DC2")]
        [TestCase("DC1, DC2")]
        [TestCase("DC1, DC2,DC3")]
        [TestCase("DC[1], DC-2, DC#3, DC@4, DC$5, DC(6), DC/7")]
        public void GetEnvironmentTags_Valid(string tags)
        {
            Environment.SetEnvironmentVariable("MOONGLADE_TAGS", tags, EnvironmentVariableTarget.Process);
            var envTags = Helper.GetEnvironmentTags();
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
        public void GetEnvironmentTags_Invalid(string invalidTag)
        {
            Environment.SetEnvironmentVariable("MOONGLADE_TAGS", $"DC1, DC2, {invalidTag}", EnvironmentVariableTarget.Process);
            var envTags = Helper.GetEnvironmentTags();
            Assert.IsNotNull(envTags);

            Assert.IsTrue(envTags.Count() == 2);
            Assert.IsTrue(!envTags.Contains(invalidTag));
        }

        [Test]
        public void GetEnvironmentTags_Empty()
        {
            Environment.SetEnvironmentVariable("MOONGLADE_TAGS", string.Empty, EnvironmentVariableTarget.Process);
            var envTags = Helper.GetEnvironmentTags();
            Assert.IsNotNull(envTags);

            Assert.IsTrue(envTags.Count() == 1);
            Assert.AreEqual(envTags.First(), string.Empty);
        }

        [Test]
        public void GenerateSlug_English()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var input = "Work 996 and Get into ICU";
            var result = input.GenerateSlug();

            Assert.AreEqual("work-996-and-get-into-icu", result);
        }
    }
}
