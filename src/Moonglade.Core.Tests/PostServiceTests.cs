using System.Diagnostics.CodeAnalysis;
using Moonglade.Utils;
using NUnit.Framework;

namespace Moonglade.Core.Tests
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class PostServiceTests
    {
        [Test]
        public void LazyLoadToImgTag_ExistLoading()
        {
            const string html = @"<p>Work 996 and have some fu bao!</p><img loading=""lazy"" src=""icu.jpg"" /><video src=""java996.mp4""></video>";
            var result = ContentProcessor.AddLazyLoadToImgTag(html);
            Assert.IsTrue(result == @"<p>Work 996 and have some fu bao!</p><img loading=""lazy"" src=""icu.jpg"" /><video src=""java996.mp4""></video>");
        }

        [Test]
        public void LazyLoadToImgTag_Empty()
        {
            var result = ContentProcessor.AddLazyLoadToImgTag(string.Empty);
            Assert.IsTrue(result == string.Empty);
        }
    }
}
