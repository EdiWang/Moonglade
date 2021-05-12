using Microsoft.Extensions.Logging;
using Moonglade.ImageStorage.Providers;
using Moq;
using NUnit.Framework;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Moonglade.ImageStorage.Tests.Providers
{
    [TestFixture]
    public class FileSystemImageStorageTests
    {
        private MockRepository _mockRepository;
        private Mock<ILogger<FileSystemImageStorage>> _mockLogger;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);
            _mockLogger = _mockRepository.Create<ILogger<FileSystemImageStorage>>();
        }

        private FileSystemImageStorage CreateFileSystemImageStorage()
        {
            var utTempPath = Path.Join(Path.GetTempPath(), "moonglade-ut", "images");
            var option = new FileSystemImageConfiguration(utTempPath);
            return new(
                _mockLogger.Object, option);
        }

        [Test]
        public async Task GetAsync_NotFound()
        {
            // Arrange
            var fileSystemImageStorage = CreateFileSystemImageStorage();
            string fileName = "996icu.png";

            // Act
            var result = await fileSystemImageStorage.GetAsync(fileName);

            Assert.IsNull(result);
        }

        //[Test]
        //public async Task DeleteAsync_StateUnderTest_ExpectedBehavior()
        //{
        //    // Arrange
        //    var fileSystemImageStorage = CreateFileSystemImageStorage();
        //    string fileName = null;

        //    // Act
        //    await fileSystemImageStorage.DeleteAsync(
        //        fileName);

        //    // Assert
        //    Assert.Fail();
        //    _mockRepository.VerifyAll();
        //}

        //[Test]
        //public async Task InsertAsync_StateUnderTest_ExpectedBehavior()
        //{
        //    // Arrange
        //    var fileSystemImageStorage = CreateFileSystemImageStorage();
        //    string fileName = null;
        //    byte[] imageBytes = null;

        //    // Act
        //    var result = await fileSystemImageStorage.InsertAsync(
        //        fileName,
        //        imageBytes);

        //    // Assert
        //    Assert.Fail();
        //    _mockRepository.VerifyAll();
        //}
    }
}
