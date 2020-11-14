using System;
using Moonglade.ImageStorage.Providers;
using NUnit.Framework;

namespace Moonglade.Tests
{
    [TestFixture]
    public class ImageStorageTests
    {
        [Test]
        [Platform(Include = "Win")]
        public void TestResolveImageStoragePathValidAbsolute()
        {
            var contentRootPath = @"C:\Moonglade";
            var path = @"C:\MoongladeData\Uploads";

            var finalPath = FileSystemImageStorage.ResolveImageStoragePath(contentRootPath, path);
            Assert.IsTrue(finalPath == @"C:\MoongladeData\Uploads");
        }

        [Test]
        [Platform(Include = "Win")]
        public void TestResolveImageStoragePathValidRelative()
        {
            var contentRootPath = @"C:\Moonglade";
            var path = @"${basedir}\Uploads";

            var finalPath = FileSystemImageStorage.ResolveImageStoragePath(contentRootPath, path);
            Assert.IsTrue(finalPath == @"C:\Moonglade\Uploads");
        }

        [Test]
        [Platform(Include = "Win")]
        public void TestResolveImageStoragePathInvalidRelative()
        {
            var contentRootPath = @"C:\Moonglade";
            var path = @"..\${basedir}\Uploads";

            Assert.Catch<NotSupportedException>(() => { FileSystemImageStorage.ResolveImageStoragePath(contentRootPath, path); });
        }

        [Test]
        [Platform(Include = "Win")]
        public void TestResolveImageStoragePathInvalidChar()
        {
            var contentRootPath = @"C:\Moonglade";
            var path = @"${basedir}\Uploads<>|foo";

            Assert.Catch<InvalidOperationException>(() => { FileSystemImageStorage.ResolveImageStoragePath(contentRootPath, path); });
        }

        [TestCase("")]
        [TestCase(" ")]
        [TestCase(null)]
        [Platform(Include = "Win")]
        public void TestResolveImageStoragePathEmptyParameter(string path)
        {
            var contentRootPath = @"C:\Moonglade";
            Assert.Catch<ArgumentNullException>(() => { FileSystemImageStorage.ResolveImageStoragePath(contentRootPath, path); });
        }
    }
}
