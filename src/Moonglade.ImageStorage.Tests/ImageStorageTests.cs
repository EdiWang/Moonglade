using System;
using System.Diagnostics.CodeAnalysis;
using Moonglade.ImageStorage.Providers;
using NUnit.Framework;

namespace Moonglade.ImageStorage.Tests
{
    [TestFixture]
    [Platform(Include = "Win")]
    [ExcludeFromCodeCoverage]
    public class ImageStorageTests
    {
        [Test]
        public void ResolveImageStoragePath_Valid_Absolute()
        {
            var contentRootPath = @"C:\Moonglade";
            var path = @"C:\MoongladeData\Uploads";

            var finalPath = FileSystemImageStorage.ResolveImageStoragePath(contentRootPath, path);
            Assert.IsTrue(finalPath == @"C:\MoongladeData\Uploads");
        }

        [Test]
        public void ResolveImageStoragePath_Valid_Relative()
        {
            var contentRootPath = @"C:\Moonglade";
            var path = @"${basedir}\Uploads";

            var finalPath = FileSystemImageStorage.ResolveImageStoragePath(contentRootPath, path);
            Assert.IsTrue(finalPath == @"C:\Moonglade\Uploads");
        }

        [Test]
        public void TestResolveImageStoragePath_Invalid_Relative()
        {
            var contentRootPath = @"C:\Moonglade";
            var path = @"..\${basedir}\Uploads";

            Assert.Catch<NotSupportedException>(() => { FileSystemImageStorage.ResolveImageStoragePath(contentRootPath, path); });
        }

        [Test]
        public void TestResolveImageStoragePath_Invalid_Char()
        {
            var contentRootPath = @"C:\Moonglade";
            var path = @"${basedir}\Uploads<>|foo";

            Assert.Catch<InvalidOperationException>(() => { FileSystemImageStorage.ResolveImageStoragePath(contentRootPath, path); });
        }

        [TestCase("")]
        [TestCase(" ")]
        [TestCase(null)]
        public void TestResolveImageStoragePath_EmptyParameter(string path)
        {
            var contentRootPath = @"C:\Moonglade";
            Assert.Catch<ArgumentNullException>(() => { FileSystemImageStorage.ResolveImageStoragePath(contentRootPath, path); });
        }
    }
}
