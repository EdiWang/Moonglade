using System;
using System.Diagnostics.CodeAnalysis;
using Moonglade.Auditing;
using Moonglade.Comments;
using Moonglade.Configuration.Abstraction;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moq;
using NUnit.Framework;

namespace Moonglade.Tests
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class CommentServiceTests
    {
        private MockRepository _mockRepository;

        private Mock<IBlogConfig> _mockBlogConfig;
        private Mock<IBlogAudit> _mockBlogAudit;
        private Mock<IRepository<CommentEntity>> _mockRepositoryCommentEntity;
        private Mock<IRepository<CommentReplyEntity>> _mockRepositoryCommentReplyEntity;
        private Mock<IRepository<PostEntity>> _mockRepositoryPostEntity;
        private Mock<ICommentModerator> _mockCommentModerator;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Strict);

            _mockBlogConfig = _mockRepository.Create<IBlogConfig>();
            _mockBlogAudit = _mockRepository.Create<IBlogAudit>();
            _mockRepositoryCommentEntity = _mockRepository.Create<IRepository<CommentEntity>>();
            _mockRepositoryCommentReplyEntity = _mockRepository.Create<IRepository<CommentReplyEntity>>();
            _mockRepositoryPostEntity = _mockRepository.Create<IRepository<PostEntity>>();
            _mockCommentModerator = _mockRepository.Create<ICommentModerator>();
        }

        private CommentService CreateCommentService()
        {
            return new(
                _mockBlogConfig.Object,
                _mockBlogAudit.Object,
                _mockRepositoryCommentEntity.Object,
                _mockRepositoryCommentReplyEntity.Object,
                _mockRepositoryPostEntity.Object,
                _mockCommentModerator.Object);
        }

        [Test]
        public void Count_ExpectedBehavior()
        {
            _mockRepositoryCommentEntity.Setup(p => p.Count(t => true)).Returns(996);
            var service = CreateCommentService();

            var result = service.Count();
            Assert.AreEqual(996, result);
        }

        [Test]
        public void ToggleApprovalAsync_EmptyIds()
        {
            var service = CreateCommentService();

            Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                await service.ToggleApprovalAsync(Array.Empty<Guid>());
            });
        }

        [Test]
        public void DeleteAsync_EmptyIds()
        {
            var service = CreateCommentService();

            Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                await service.DeleteAsync(Array.Empty<Guid>());
            });
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void GetCommentsAsync_InvalidPageSize(int pageSize)
        {
            var service = CreateCommentService();

            Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            {
                await service.GetCommentsAsync(pageSize, 1);
            });
        }
    }
}
