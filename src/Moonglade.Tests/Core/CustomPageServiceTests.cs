using System;
using System.Collections.Generic;
using System.Text;
using Moonglade.Core;
using NUnit.Framework;

namespace Moonglade.Tests.Core
{
    [TestFixture]
    public class CustomPageServiceTests
    {
        [Test]
        public void TestRemoveScriptTagFromHtml()
        {
            var html = @"<p>Microsoft</p><p>Rocks!</p><p>Azure <br /><script>console.info('hey');</script><img src=""a.jpg"" /> The best <span>cloud</span>!</p>";
            var output = CustomPageService.RemoveScriptTagFromHtml(html);

            Assert.IsTrue(output == @"<p>Microsoft</p><p>Rocks!</p><p>Azure <br /><img src=""a.jpg"" /> The best <span>cloud</span>!</p>");
        }

        [Test]
        public void TestRemoveWhiteSpaceFromStylesheets()
        {
            var css = @"h1 {
                            color: red;
                        }";
            var output = CustomPageService.RemoveWhiteSpaceFromStylesheets(css);
            Assert.IsTrue(output == "h1{color:red}");
        }
    }
}
