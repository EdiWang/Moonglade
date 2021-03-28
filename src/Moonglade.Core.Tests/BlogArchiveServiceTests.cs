using Microsoft.Extensions.Logging;
using Moonglade.Core;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moq;
using NUnit.Framework;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Moonglade.Core.Tests
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class BlogArchiveServiceTests
    {
        private MockRepository _mockRepository;

        private Mock<ILogger<BlogArchiveService>> _mockLogger;
        private Mock<IRepository<PostEntity>> _mockPostEntityRepository;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);

            _mockLogger = _mockRepository.Create<ILogger<BlogArchiveService>>();
            _mockPostEntityRepository = _mockRepository.Create<IRepository<PostEntity>>();
        }

        private BlogArchiveService CreateService()
        {
            return new(
                _mockLogger.Object,
                _mockPostEntityRepository.Object);
        }

        [Test]
        public async Task ListAsync_NoPosts()
        {
            _mockPostEntityRepository.Setup(p => p.Any(p => p.IsPublished && !p.IsDeleted)).Returns(false);

            // Arrange
            var service = CreateService();

            // Act
            var result = await service.ListAsync();
            
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        //[Test]
        //public async Task ListPostsAsync_StateUnderTest_ExpectedBehavior()
        //{
        //    // Arrange
        //    var service = this.CreateService();
        //    int year = 0;
        //    int month = 0;

        //    // Act
        //    var result = await service.ListPostsAsync(
        //        year,
        //        month);

        //    // Assert
        //    Assert.Fail();
        //    this.mockRepository.VerifyAll();
        //}
    }
}
