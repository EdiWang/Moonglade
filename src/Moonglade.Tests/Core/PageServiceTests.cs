using Moonglade.Core;
using NUnit.Framework;

namespace Moonglade.Tests.Core
{
    [TestFixture]
    public class PageServiceTests
    {
        [Test]
        public void TestRemoveScriptTagFromHtml()
        {
            var html = @"<p>Microsoft</p><p>Rocks!</p><p>Azure <br /><script>console.info('hey');</script><img src=""a.jpg"" /> The best <span>cloud</span>!</p>";
            var output = PageService.RemoveScriptTagFromHtml(html);

            Assert.IsTrue(output == @"<p>Microsoft</p><p>Rocks!</p><p>Azure <br /><img src=""a.jpg"" /> The best <span>cloud</span>!</p>");
        }
    }
}
