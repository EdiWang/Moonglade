using Moonglade.ImageStorage.Providers;
using NUnit.Framework;

namespace Moonglade.ImageStorage.Tests;

[TestFixture]
[Platform(Include = "Win")]
public class ImageStorageTests
{
    [Test]
    public void ResolveImageStoragePath_Valid_Absolute()
    {
        var contentRootPath = @"C:\Moonglade";
        var path = @"C:\MoongladeData\Uploads";

        var finalPath = FileSystemImageStorage.ResolveImageStoragePath(path);
        Assert.IsTrue(finalPath == @"C:\MoongladeData\Uploads");

        CleanUpTestDirectory(contentRootPath, @"C:\MoongladeData");
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

    [TestCase("")]
    [TestCase(" ")]
    [TestCase(null)]
    public void TestResolveImageStoragePath_EmptyParameter(string path)
    {
        Assert.Catch<ArgumentNullException>(() => { FileSystemImageStorage.ResolveImageStoragePath(path); });
    }
}