using System;
using System.Diagnostics.CodeAnalysis;
using Moonglade.ImageStorage.Providers;
using NUnit.Framework;

namespace Moonglade.Tests
{
    [TestFixture]
    [Platform(Include = "Win")]
    [ExcludeFromCodeCoverage]
    public class ImageStorageTests
    {
        [Test]
        public void TestResolveImageStoragePathValidAbsolute()
        {
            var contentRootPath = @"C:\Moonglade";
            var path = @"C:\MoongladeData\Uploads";

            var finalPath = FileSystemImageStorage.ResolveImageStoragePath(contentRootPath, path);
            Assert.IsTrue(finalPath == @"C:\MoongladeData\Uploads");
        }

        [Test]
        public void TestResolveImageStoragePathValidRelative()
        {
            var contentRootPath = @"C:\Moonglade";
            var path = @"${basedir}\Uploads";

            var finalPath = FileSystemImageStorage.ResolveImageStoragePath(contentRootPath, path);
            Assert.IsTrue(finalPath == @"C:\Moonglade\Uploads");
        }

        [Test]
        public void TestResolveImageStoragePathInvalidRelative()
        {
            var contentRootPath = @"C:\Moonglade";
            var path = @"..\${basedir}\Uploads";

            Assert.Catch<NotSupportedException>(() => { FileSystemImageStorage.ResolveImageStoragePath(contentRootPath, path); });
        }

        [Test]
        public void TestResolveImageStoragePathInvalidChar()
        {
            var contentRootPath = @"C:\Moonglade";
            var path = @"${basedir}\Uploads<>|foo";

            Assert.Catch<InvalidOperationException>(() => { FileSystemImageStorage.ResolveImageStoragePath(contentRootPath, path); });
        }

        [TestCase("")]
        [TestCase(" ")]
        [TestCase(null)]
        public void TestResolveImageStoragePathEmptyParameter(string path)
        {
            var contentRootPath = @"C:\Moonglade";
            Assert.Catch<ArgumentNullException>(() => { FileSystemImageStorage.ResolveImageStoragePath(contentRootPath, path); });
        }
    }
}
