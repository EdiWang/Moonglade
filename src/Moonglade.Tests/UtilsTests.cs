using System;
using System.Runtime.InteropServices;
using Moonglade.Core;
using NUnit.Framework;

namespace Moonglade.Tests
{
    public class UtilsTests
    {
        [Test]
        public void TestUtcToZoneTime()
        {
            var utc = new DateTime(2000, 1, 1, 0, 0, 0);
            var dt = Utils.UtcToZoneTime(utc, 8);
            Assert.IsTrue(dt == DateTime.Parse("2000/1/1 8:00:00"));
        }

        [Test]
        public void TestLeft()
        {
            var str = "Microsoft Rocks!";
            var left = Utils.Left(str, 9);
            Assert.IsTrue(left == "Microsoft");
        }

        [Test]
        public void TestRight()
        {
            var str = "996 Sucks!";
            var left = Utils.Right(str, 6);
            Assert.IsTrue(left == "Sucks!");
        }

        [Test]
        public void TestNormalizeTagName()
        {
            var tag1org = ".NET Core";
            var tag2org = "C#";
            var tag1 = Utils.NormalizeTagName(tag1org);
            var tag2 = Utils.NormalizeTagName(tag2org);
            Assert.IsTrue(tag1 == "dotnet-core");
            Assert.IsTrue(tag2 == "csharp");
        }

        [Test]
        public void TryParseBase64Success()
        {
            bool ok = Utils.TryParseBase64("xDgItVa0ujLKxGsoMV1+MmxBrpo997mXbeXngqIx13o=", out var base64);
            Assert.IsTrue(ok);
            Assert.IsNotNull(base64);
        }

        [Test]
        public void TryParseBase64Fail()
        {
            bool ok = Utils.TryParseBase64("Learn Java and work 996!", out var base64);
            Assert.IsFalse(ok);
            Assert.IsNull(base64);
        }

        [Test]
        public void TestReplaceImgSrc()
        {
            var html = @"<p>Work 996 and have some fu bao!</p><img src=""icu.jpg"" /><video src=""java996.mp4""></video>";
            var result = Utils.ReplaceImgSrc(html);
            Assert.IsTrue(result == @"<p>Work 996 and have some fu bao!</p><img data-src=""icu.jpg"" /><video src=""java996.mp4""></video>");
        }

        [Test]
        public void TestMdContentToHtml()
        {
            string md = "A quick brown **fox** jumped over the lazy dog.";
            string result = Utils.MdContentToHtml(md);

            Assert.IsTrue(result == "<p>A quick brown <strong>fox</strong> jumped over the lazy dog.</p>\n");
        }

        [Test]
        public void TestResolveImageStoragePathValidAbsolute()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var contentRootPath = @"C:\Moonglade";
                var path = @"C:\MoongladeData\Uploads";

                var finalPath = Utils.ResolveImageStoragePath(contentRootPath, path);
                Assert.IsTrue(finalPath == @"C:\MoongladeData\Uploads");
            }
        }

        [Test]
        public void TestResolveImageStoragePathValidRelative()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var contentRootPath = @"C:\Moonglade";
                var path = @"${basedir}\Uploads";

                var finalPath = Utils.ResolveImageStoragePath(contentRootPath, path);
                Assert.IsTrue(finalPath == @"C:\Moonglade\Uploads");
            }
        }

        [Test]
        public void TestResolveImageStoragePathInvalidRelative()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var contentRootPath = @"C:\Moonglade";
                var path = @"..\${basedir}\Uploads";

                Assert.Catch<NotSupportedException>(() =>
                {
                    var finalPath = Utils.ResolveImageStoragePath(contentRootPath, path);
                });
            }
        }

        [Test]
        public void TestResolveImageStoragePathInvalidChar()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var contentRootPath = @"C:\Moonglade";
                var path = @"${basedir}\Uploads<>|foo";

                Assert.Catch<InvalidOperationException>(() =>
                {
                    var finalPath = Utils.ResolveImageStoragePath(contentRootPath, path);
                });
            }
        }

        [Test]
        public void TestResolveImageStoragePathEmptyParameter()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var contentRootPath = @"C:\Moonglade";
                Assert.Catch<ArgumentNullException>(() =>
                {
                    var finalPath = Utils.ResolveImageStoragePath(contentRootPath, string.Empty);
                });
                Assert.Catch<ArgumentNullException>(() =>
                {
                    var finalPath = Utils.ResolveImageStoragePath(contentRootPath, " ");
                });
                Assert.Catch<ArgumentNullException>(() =>
                {
                    var finalPath = Utils.ResolveImageStoragePath(contentRootPath, null);
                });
            }
        }

        [Test]
        public void TestRemoveTags()
        {
            var html = @"<p>Microsoft</p><p>Rocks!</p><p>Azure <br /><img src=""a.jpg"" /> The best <span>cloud</span>!</p>";
            var output = Utils.RemoveTags(html);

            Assert.IsTrue(output == "MicrosoftRocks!Azure  The best cloud!");
        }

        [Test]
        public void TestIsLetter()
        {
            char c1 = 'f';
            Assert.IsTrue(c1.IsLetter());

            char c2 = '0';
            Assert.IsFalse(c2.IsLetter());
        }

        [Test]
        public void TestIsSpace()
        {
            char c1 = ' ';
            Assert.IsTrue(c1.IsSpace());

            char c2 = '0';
            Assert.IsFalse(c2.IsSpace());
        }

        [Test]
        public void TestEllipsize()
        {
            var text1 = "A 996 programmer went to heaven.";
            var result1 = text1.Ellipsize(10);
            var expected1 = "A 996" + "\u00A0\u2026";
            Assert.IsTrue(result1 == expected1);

            var text2 = "Fu bao";
            var result2 = text2.Ellipsize(10);
            Assert.IsTrue(result2 == "Fu bao");
        }

        [Test]
        public void TestGetPostAbstract()
        {
            var html = @"<p>Microsoft</p> <p>Rocks!</p><p>Azure <br /><img src=""a.jpg"" /> The best <span>cloud</span>!</p>";
            var result = Utils.GetPostAbstract(html, 16);
            var expected = "Microsoft Rocks!" + "\u00A0\u2026";
            Assert.IsTrue(result == expected);
        }

        [Test]
        public void TestGetSlugInfoFromPostUrlSuccess()
        {
            var url = "https://edi.wang/post/2075/4/4/happy-birthday-to-microsoft";
            var result = Utils.GetSlugInfoFromPostUrl(url);
            Assert.IsTrue(result.IsSuccess);
            Assert.IsNotNull(result.Item);
            Assert.IsTrue(result.Item.Slug == "happy-birthday-to-microsoft");
            Assert.IsTrue(result.Item.PubDate == new DateTime(2075,4,4));
        }

        [Test]
        public void TestGetSlugInfoFromPostUrlFail()
        {
            var url = "https://edi.wang/996-workers-ganked-alibaba";
            var result = Utils.GetSlugInfoFromPostUrl(url);
            Assert.IsFalse(result.IsSuccess);
            Assert.IsNull(result.Item.Slug);
            Assert.IsTrue(result.Item.PubDate == DateTime.MinValue);
        }

        [Test]
        public void TestIsValidUrlDefault()
        {
            string url = "https://ews.azureedge.net/ediwang-images";
            Assert.IsTrue(url.IsValidUrl());
        }

        [Test]
        public void TestIsValidUrlDefaultFail()
        {
            string url = "a quick brown fox jumped over the lazy dog.";
            Assert.IsFalse(url.IsValidUrl());
        }

        [Test]
        public void TestIsValidUrlHttps()
        {
            string url = "https://ews.azureedge.net/ediwang-images";
            Assert.IsTrue(url.IsValidUrl(Utils.UrlScheme.Https));
        }

        [Test]
        public void TestIsValidUrlHttpFail()
        {
            string url = "https://ews.azureedge.net/ediwang-images";
            Assert.IsTrue(url.IsValidUrl(Utils.UrlScheme.Http));
        }

        [Test]
        public void TestIsValidUrlHttpsFail()
        {
            string url = "http://ews.azureedge.net/ediwang-images";
            Assert.IsTrue(url.IsValidUrl(Utils.UrlScheme.Https));
        }

        [Test]
        // TODO: Use TestCase to test multiple scenarios
        public void TestCombineUrl()
        {
            string url = "http://ews.azureedge.net/ediwang-images";
            string path = "996.png";

            var result = Utils.CombineUrl(url, path);
            Assert.IsTrue(result == url + "/" + path);
        }
    }
}
