using System.Diagnostics.CodeAnalysis;
using Moonglade.Core;
using Moonglade.Utils;
using NUnit.Framework;

namespace Moonglade.Tests.Core
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class MenuServiceTests
    {
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
    }
}
