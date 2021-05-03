using Moonglade.Core;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moq;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace Moonglade.Core.Tests
{
    [TestFixture]
    public class BlogStatisticsTests
    {
        private MockRepository _mockRepository;
        private Mock<IRepository<PostExtensionEntity>> _mockPostExtensionRepo;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);
            _mockPostExtensionRepo = _mockRepository.Create<IRepository<PostExtensionEntity>>();
        }

        private BlogStatistics CreateBlogStatistics()
        {
            return new(_mockPostExtensionRepo.Object);
        }

        [Test]
        public async Task GetStatisticAsync_StateUnderTest_ExpectedBehavior()
        {
            _mockPostExtensionRepo.Setup(p => p.GetAsync(It.IsAny<Guid>()))
                .Returns(ValueTask.FromResult(new PostExtensionEntity()
                {
                    Hits = 996,
                    Likes = 251
                }));

            // Arrange
            var blogStatistics = CreateBlogStatistics();
            Guid postId = Guid.Empty;

            // Act
            var result = await blogStatistics.GetStatisticAsync(postId);

            // Assert
            Assert.AreEqual(996, result.Hits);
            Assert.AreEqual(251, result.Likes);
        }

        //[Test]
        //public async Task UpdateStatisticAsync_StateUnderTest_ExpectedBehavior()
        //{
        //    // Arrange
        //    var blogStatistics = CreateBlogStatistics();
        //    Guid postId = default(Guid);
        //    int likes = 0;

        //    // Act
        //    await blogStatistics.UpdateStatisticAsync(
        //        postId,
        //        likes);

        //    // Assert
        //    Assert.Fail();
        //    _mockRepository.VerifyAll();
        //}
    }
}
