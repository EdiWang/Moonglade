using Moonglade.Data.Porting;
using NUnit.Framework;

namespace Moonglade.Data.Tests
{
    [TestFixture]
    public class ExportManagerTests
    {
        [Test]
        public void CreateExportDirectory_StateUnderTest_ExpectedBehavior()
        {
            string directory = Path.GetTempPath();
            string subDirName = "996";

            var result = ExportManager.CreateExportDirectory(directory, subDirName);

            Assert.IsNotNull(result);
            Assert.AreEqual(Path.Join(directory, "export", subDirName), result);
        }

        //[Test]
        //public async Task ExportData_StateUnderTest_ExpectedBehavior()
        //{
        //    // Arrange
        //    var manager = CreateManager();
        //    ExportType dataType = default(ExportType);

        //    // Act
        //    var result = await manager.ExportData(
        //        dataType);

        //    // Assert
        //    Assert.Fail();
        //    _mockRepository.VerifyAll();
        //}
    }
}
