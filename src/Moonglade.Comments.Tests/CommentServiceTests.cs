using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Moonglade.Auditing;
using Moonglade.Configuration.Abstraction;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;
using Moq;
using NUnit.Framework;

namespace Moonglade.Comments.Tests
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class CommentServiceTests
    {
        private MockRepository _mockRepository;

        private Mock<IBlogConfig> _mockBlogConfig;
        private Mock<IBlogAudit> _mockBlogAudit;
        private Mock<IRepository<CommentEntity>> _mockCommentEntityRepo;
        private Mock<IRepository<CommentReplyEntity>> _mockCommentReplyEntityRepo;
        private Mock<IRepository<PostEntity>> _mockPostEntityRepo;
        private Mock<ICommentModerator> _mockCommentModerator;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);

            _mockBlogConfig = _mockRepository.Create<IBlogConfig>();
            _mockBlogAudit = _mockRepository.Create<IBlogAudit>();
            _mockCommentEntityRepo = _mockRepository.Create<IRepository<CommentEntity>>();
            _mockCommentReplyEntityRepo = _mockRepository.Create<IRepository<CommentReplyEntity>>();
            _mockPostEntityRepo = _mockRepository.Create<IRepository<PostEntity>>();
            _mockCommentModerator = _mockRepository.Create<ICommentModerator>();
        }

        private CommentService CreateCommentService()
        {
            return new(
                _mockBlogConfig.Object,
                _mockBlogAudit.Object,
                _mockCommentEntityRepo.Object,
                _mockCommentReplyEntityRepo.Object,
                _mockPostEntityRepo.Object,
                _mockCommentModerator.Object);
        }

        [Test]
        public void Count_ExpectedBehavior()
        {
            _mockCommentEntityRepo.Setup(p => p.Count(t => true)).Returns(996);
            var service = CreateCommentService();

            var result = service.Count();
            Assert.AreEqual(996, result);
        }

        [Test]
        public async Task GetApprovedCommentsAsync_OK()
        {
            var service = CreateCommentService();
            await service.GetApprovedCommentsAsync(Guid.Empty);

            _mockCommentEntityRepo.Verify(p => p.SelectAsync(It.IsAny<ISpecification<CommentEntity>>(),
                It.IsAny<Expression<Func<CommentEntity, Comment>>>(), true));
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

            Assert.ThrowsAsync<ArgumentNullException>(async () => { await service.DeleteAsync(Array.Empty<Guid>()); });
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

        [Test]
        public async Task GetCommentsAsync_OK()
        {
            IReadOnlyList<CommentDetailedItem> details = new List<CommentDetailedItem>();

            _mockCommentEntityRepo.Setup(p => p.SelectAsync(It.IsAny<CommentSpec>(),
                It.IsAny<Expression<Func<CommentEntity, CommentDetailedItem>>>(), true))
                .Returns(Task.FromResult(details));

            var service = CreateCommentService();
            var result = await service.GetCommentsAsync(7, 996);

            Assert.IsNotNull(result);
            _mockCommentEntityRepo.Verify(p => p.SelectAsync(It.IsAny<CommentSpec>(),
                It.IsAny<Expression<Func<CommentEntity, CommentDetailedItem>>>(), true));
        }
    }
}
