using MediatR;
using Moonglade.Data.Porting;
using Moq;
using NUnit.Framework;
using System.IO;

namespace Moonglade.Data.Tests
{
    [TestFixture]
    public class ExportManagerTests
    {
        private MockRepository _mockRepository;
        private Mock<IMediator> _mockMediator;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);
            _mockMediator = _mockRepository.Create<IMediator>();
        }

        private ExportManager CreateManager()
        {
            return new(_mockMediator.Object);
        }

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
        //    ExportDataType dataType = default(ExportDataType);

        //    // Act
        //    var result = await manager.ExportData(
        //        dataType);

        //    // Assert
        //    Assert.Fail();
        //    _mockRepository.VerifyAll();
        //}
    }
}
