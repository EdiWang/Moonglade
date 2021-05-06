using System;
using System.Threading.Tasks;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moq;
using NUnit.Framework;

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

        [Test]
        public async Task UpdateStatisticAsync_Null()
        {
            // Arrange
            _mockPostExtensionRepo.Setup(p => p.GetAsync(It.IsAny<Guid>()))
                .Returns(ValueTask.FromResult((PostExtensionEntity)null));

            var blogStatistics = CreateBlogStatistics();
            Guid postId = Guid.Empty;

            // Act
            await blogStatistics.UpdateStatisticAsync(postId, true);

            // Assert
            _mockPostExtensionRepo.Verify(p => p.UpdateAsync(It.IsAny<PostExtensionEntity>()), Times.Never);
        }

        [Test]
        public async Task UpdateStatisticAsync_Like()
        {
            // Arrange
            var ett = new PostExtensionEntity
            {
                Hits = 996,
                Likes = 251
            };

            _mockPostExtensionRepo.Setup(p => p.GetAsync(It.IsAny<Guid>()))
                .Returns(ValueTask.FromResult(ett));
            var blogStatistics = CreateBlogStatistics();
            Guid postId = Guid.Empty;

            // Act
            await blogStatistics.UpdateStatisticAsync(postId, true);

            // Assert
            Assert.AreEqual(251 + 1, ett.Likes);
            _mockPostExtensionRepo.Verify(p => p.UpdateAsync(It.IsAny<PostExtensionEntity>()), Times.Once);
        }

        [Test]
        public async Task UpdateStatisticAsync_Like_Max()
        {
            // Arrange
            var ett = new PostExtensionEntity
            {
                Hits = 996,
                Likes = int.MaxValue
            };

            _mockPostExtensionRepo.Setup(p => p.GetAsync(It.IsAny<Guid>()))
                .Returns(ValueTask.FromResult(ett));
            var blogStatistics = CreateBlogStatistics();
            Guid postId = Guid.Empty;

            // Act
            await blogStatistics.UpdateStatisticAsync(postId, true);

            // Assert
            Assert.AreEqual(int.MaxValue, ett.Likes);
            _mockPostExtensionRepo.Verify(p => p.UpdateAsync(It.IsAny<PostExtensionEntity>()), Times.Never);
        }

        [Test]
        public async Task UpdateStatisticAsync_Hit()
        {
            // Arrange
            var ett = new PostExtensionEntity
            {
                Hits = 996,
                Likes = 251
            };

            _mockPostExtensionRepo.Setup(p => p.GetAsync(It.IsAny<Guid>()))
                .Returns(ValueTask.FromResult(ett));
            var blogStatistics = CreateBlogStatistics();
            Guid postId = Guid.Empty;

            // Act
            await blogStatistics.UpdateStatisticAsync(postId, false);

            // Assert
            Assert.AreEqual(996 + 1, ett.Hits);
            _mockPostExtensionRepo.Verify(p => p.UpdateAsync(It.IsAny<PostExtensionEntity>()), Times.Once);
        }

        [Test]
        public async Task UpdateStatisticAsync_Hit_Max()
        {
            // Arrange
            var ett = new PostExtensionEntity
            {
                Hits = int.MaxValue,
                Likes = 251
            };

            _mockPostExtensionRepo.Setup(p => p.GetAsync(It.IsAny<Guid>()))
                .Returns(ValueTask.FromResult(ett));
            var blogStatistics = CreateBlogStatistics();
            Guid postId = Guid.Empty;

            // Act
            await blogStatistics.UpdateStatisticAsync(postId, false);

            // Assert
            Assert.AreEqual(int.MaxValue, ett.Hits);
            _mockPostExtensionRepo.Verify(p => p.UpdateAsync(It.IsAny<PostExtensionEntity>()), Times.Never);
        }
    }
}
