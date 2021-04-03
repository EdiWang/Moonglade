using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using NUnit.Framework;

namespace Moonglade.Utils.Tests
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class HelperTests
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

        [TestCase(null, ExpectedResult = true)]
        [TestCase("someName@someDomain.com", ExpectedResult = true)] // Simple valid value
        [TestCase("1234@someDomain.com", ExpectedResult = true)] // numbers are valid
        [TestCase("firstName.lastName@someDomain.com", ExpectedResult = true)] // With dot in name
        [TestCase("\u00A0@someDomain.com", ExpectedResult = true)] // With valid \u character
        [TestCase("!#$%&'*+-/=?^_`|~@someDomain.com", ExpectedResult = true)] // With valid (but unusual) characters
        [TestCase("\"firstName.lastName\"@someDomain.com", ExpectedResult = true)] // quotes around whole local part
        [TestCase("someName@some~domain.com", ExpectedResult = true)] // With tilde
        [TestCase("someName@some_domain.com", ExpectedResult = true)] // With underscore
        [TestCase("someName@1234.com", ExpectedResult = true)] // numbers are valid
        [TestCase("someName@someDomain\uFFEF.com", ExpectedResult = true)] // With valid \u character
        [TestCase("@someDomain.com", ExpectedResult = false)] // no local part
        [TestCase("\0@someDomain.com", ExpectedResult = false)] // illegal character
        [TestCase(".someName@someDomain.com", ExpectedResult = false)] // initial dot not allowed
        [TestCase("someName.@someDomain.com", ExpectedResult = false)] // final dot not allowed
        [TestCase("firstName..lastName@someDomain.com", ExpectedResult = false)] // two adjacent dots not allowed
        [TestCase("firstName(comment)lastName@someDomain.com", ExpectedResult = false)] // parens not allowed
        [TestCase("firstName\"middleName\"lastName@someDomain.com", ExpectedResult = false)] // quotes in middle not allowed
        [TestCase("@someDomain@abc.com", ExpectedResult = false)] // multiple @'s
        [TestCase("someName", ExpectedResult = false)] // no domain
        [TestCase("someName@", ExpectedResult = false)] // no domain
        [TestCase("someName@someDomain", ExpectedResult = false)] // Domain must have at least 1 dot
        [TestCase("someName@a@b.com", ExpectedResult = false)] // multiple @'s
        [TestCase("someName@\0.com", ExpectedResult = false)] // illegal character
        [TestCase("someName@someDomain..com", ExpectedResult = false)] // two adjacent dots not allowed
        public bool IsValidEmailAddress_Full(string email)
        {
            return Helper.IsValidEmailAddress(email);
        }

        [Test]
        [Platform(Include = "Win")]
        public void TryGetFullOSVersion_Windows()
        {
            var osversion = Helper.TryGetFullOSVersion();
            Assert.IsTrue(osversion.StartsWith("Windows"));
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
        public string SterilizeMenuLink(string rawUrl)
        {
            return Helper.SterilizeLink(rawUrl);
        }

        [Test]
        public void GetErrorMessagesFromModelState_NullModelStateDictionary()
        {
            var result = Helper.GetErrorMessagesFromModelState(null);
            Assert.IsNull(result);
        }

        [Test]
        public void GetErrorMessagesFromModelState_NoModelErrors()
        {
            var msd = new ModelStateDictionary();
            var result = Helper.GetErrorMessagesFromModelState(msd);
            Assert.IsNull(result);
        }

        [Test]
        public void GetErrorMessagesFromModelState_HasModelErrors()
        {
            var msd = new ModelStateDictionary
            {
                MaxAllowedErrors = 35
            };

            msd.AddModelError("996", "Jack Ma is an asshole");
            msd.AddModelError("251", "Use HW and be a patriot");

            var result = Helper.GetErrorMessagesFromModelState(msd);

            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count());
        }

        [Test]
        public void CombineErrorMessages_Default_HasModelErrors()
        {
            var msd = new ModelStateDictionary
            {
                MaxAllowedErrors = 35
            };

            msd.AddModelError("996", "Jack Ma is an asshole");
            msd.AddModelError("251", "Use HW and be a patriot");

            var result = msd.CombineErrorMessages();

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Contains("Jack Ma is an asshole"));
            Assert.IsTrue(result.Contains("Use HW and be a patriot"));
        }

        [TestCase(0, 0)]
        [TestCase(251, 0)]
        [TestCase(10, -1)]
        [TestCase(10, 11)]
        public void GeneratePassword_BadParameterRange(int length, int numberOfNonAlphanumericCharacters)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                Helper.GeneratePassword(length, numberOfNonAlphanumericCharacters);
            });
        }

        [Test]
        public void GeneratePassword_OK()
        {
            var password = Helper.GeneratePassword(10, 3);

            Assert.IsNotNull(password);
            Assert.AreEqual(10, password.Length);
        }
    }
}
