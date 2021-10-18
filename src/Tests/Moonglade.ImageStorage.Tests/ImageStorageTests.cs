using Moonglade.ImageStorage.Providers;
using NUnit.Framework;

namespace Moonglade.ImageStorage.Tests
{
    [TestFixture]
    [Platform(Include = "Win")]
    public class ImageStorageTests
    {
        [Test]
        public void ResolveImageStoragePath_Valid_Absolute()
        {
            var contentRootPath = @"C:\Moonglade";
            var path = @"C:\MoongladeData\Uploads";

            var finalPath = FileSystemImageStorage.ResolveImageStoragePath(contentRootPath, path);
            Assert.IsTrue(finalPath == @"C:\MoongladeData\Uploads");

            CleanUpTestDirectory(contentRootPath, @"C:\MoongladeData");
        }

        [Test]
        public void ResolveImageStoragePath_Valid_Relative()
        {
            var contentRootPath = @"C:\Moonglade";
            var path = @"${basedir}\Uploads";

            var finalPath = FileSystemImageStorage.ResolveImageStoragePath(contentRootPath, path);
            Assert.IsTrue(finalPath == @"C:\Moonglade\Uploads");

            CleanUpTestDirectory(contentRootPath);
        }

        private void CleanUpTestDirectory(params string[] paths)
        {
            foreach (var path in paths)
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                }
            }
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
