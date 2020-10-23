using System;
using System.Collections.Generic;
using System.Text;
using Moonglade.Core;
using NUnit.Framework;

namespace Moonglade.Tests.Core
{
    [TestFixture]
    public class PostServiceTests
    {
        [Test]
        public void TestLazyLoadToImgTagExistLoading()
        {
            const string html = @"<p>Work 996 and have some fu bao!</p><img loading=""lazy"" src=""icu.jpg"" /><video src=""java996.mp4""></video>";
            var result = ContentProcessor.AddLazyLoadToImgTag(html);
            Assert.IsTrue(result == @"<p>Work 996 and have some fu bao!</p><img loading=""lazy"" src=""icu.jpg"" /><video src=""java996.mp4""></video>");
        }

        [Test]
        public void TestLazyLoadToImgTagEmpty()
        {
            var result = ContentProcessor.AddLazyLoadToImgTag(string.Empty);
            Assert.IsTrue(result == string.Empty);
        }
    }
}
