using System;
using System.Collections.Generic;
using System.Text;
using Moonglade.Core;
using NUnit.Framework;

namespace Moonglade.Tests
{
    [TestFixture]
    public class BlogContentProcessorTests
    {

        [Test]
        public void TestRemoveTags()
        {
            var html = @"<p>Microsoft</p><p>Rocks!</p><p>Azure <br /><img src=""a.jpg"" /> The best <span>cloud</span>!</p>";
            var output = BlogContentProcessor.RemoveTags(html);

            Assert.IsTrue(output == "MicrosoftRocks!Azure  The best cloud!");
        }

        [Test]
        public void TestRemoveTagsEmpty()
        {
            var output = BlogContentProcessor.RemoveTags(string.Empty);
            Assert.AreEqual(string.Empty, output);
        }

        [Test]
        public void TestGetPostAbstract()
        {
            var html = @"<p>Microsoft</p> <p>Rocks!</p><p>Azure <br /><img src=""a.jpg"" /> The best <span>cloud</span>!</p>";
            var result = BlogContentProcessor.GetPostAbstract(html, 16);
            var expected = "Microsoft Rocks!" + "\u00A0\u2026";
            Assert.IsTrue(result == expected);
        }

        [Test]
        public void TestLazyLoadToImgTag()
        {
            const string html = @"<p>Work 996 and have some fu bao!</p><img src=""icu.jpg"" /><video src=""java996.mp4""></video>";
            var result = BlogContentProcessor.AddLazyLoadToImgTag(html);
            Assert.IsTrue(result == @"<p>Work 996 and have some fu bao!</p><img loading=""lazy"" src=""icu.jpg"" /><video src=""java996.mp4""></video>");
        }

        [Test]
        public void TestMdContentToHtml()
        {
            var md = "A quick brown **fox** jumped over the lazy dog.";
            var result = BlogContentProcessor.MarkdownToContent(md, BlogContentProcessor.MarkdownConvertType.Html);

            Assert.IsTrue(result == "<p>A quick brown <strong>fox</strong> jumped over the lazy dog.</p>\n");
        }
    }
}
