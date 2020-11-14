using Moonglade.Core;
using NUnit.Framework;

namespace Moonglade.Tests
{
    [TestFixture]
    public class ContentProcessorTests
    {
        [TestCase('f', ExpectedResult = true)]
        [TestCase('0', ExpectedResult = false)]
        [TestCase('`', ExpectedResult = false)]
        [TestCase('#', ExpectedResult = false)]
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
        [TestCase("", ExpectedResult = "")]
        public string TestEllipsize(string str)
        {
            return str.Ellipsize(10);
        }

        [Test]
        public void TestRemoveTags()
        {
            var html = @"<p>Microsoft</p><p>Rocks!</p><p>Azure&nbsp;<br /><img src=""a.jpg"" /> The best <span>cloud</span>!</p>";
            var output = ContentProcessor.RemoveTags(html);

            Assert.IsTrue(output == "Microsoft Rocks! Azure  The best cloud!");
        }

        [Test]
        public void TestRemoveTagsEmpty()
        {
            var output = ContentProcessor.RemoveTags(string.Empty);
            Assert.AreEqual(string.Empty, output);
        }

        [Test]
        public void TestGetPostAbstract()
        {
            var html = @"<p>Microsoft</p> <p>Rocks!</p><p>Azure <br /><img src=""a.jpg"" /> The best <span>cloud</span>!</p>";
            var result = ContentProcessor.GetPostAbstract(html, 16);
            var expected = "Microsoft Rocks!" + "\u00A0\u2026";
            Assert.IsTrue(result == expected);
        }

        [Test]
        public void TestLazyLoadToImgTag()
        {
            const string html = @"<p>Work 996 and have some fu bao!</p><img src=""icu.jpg"" /><video src=""java996.mp4""></video>";
            var result = ContentProcessor.AddLazyLoadToImgTag(html);
            Assert.IsTrue(result == @"<p>Work 996 and have some fu bao!</p><img loading=""lazy"" src=""icu.jpg"" /><video src=""java996.mp4""></video>");
        }

        [Test]
        public void TestMdContentToHtml()
        {
            var md = "A quick brown **fox** jumped over the lazy dog.";
            var result = ContentProcessor.MarkdownToContent(md, ContentProcessor.MarkdownConvertType.Html);

            Assert.IsTrue(result == "<p>A quick brown <strong>fox</strong> jumped over the lazy dog.</p>\n");
        }
    }
}
