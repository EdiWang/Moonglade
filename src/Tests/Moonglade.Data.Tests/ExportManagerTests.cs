using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
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

        private Mock<IRepository<FriendLinkEntity>> _mockRepositoryFriendLinkEntity;
        private Mock<IRepository<PageEntity>> _mockRepositoryPageEntity;
        private Mock<IRepository<PostEntity>> _mockRepositoryPostEntity;
        private Mock<IMediator> _mockMediator;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);

            _mockRepositoryFriendLinkEntity = _mockRepository.Create<IRepository<FriendLinkEntity>>();
            _mockRepositoryPageEntity = _mockRepository.Create<IRepository<PageEntity>>();
            _mockRepositoryPostEntity = _mockRepository.Create<IRepository<PostEntity>>();
            _mockMediator = _mockRepository.Create<IMediator>();
        }

        private ExportManager CreateManager()
        {
            return new(
                _mockRepositoryFriendLinkEntity.Object,
                _mockRepositoryPageEntity.Object,
                _mockRepositoryPostEntity.Object,
                _mockMediator.Object);
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
