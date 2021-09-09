using Moonglade.Core.PostFeature;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moq;
using NUnit.Framework;
using System;
using System.Linq.Expressions;

namespace Moonglade.Core.Tests
{
    [TestFixture]
    public class PostCountServiceTests
    {
        private MockRepository _mockRepository;

        private Mock<IRepository<PostEntity>> _mockPostEntityRepo;
        private Mock<IRepository<PostTagEntity>> _mockPostTagEntityRepo;
        private Mock<IRepository<PostCategoryEntity>> _mockPostCategoryRepo;

        private static readonly Guid Uid = Guid.Parse("76169567-6ff3-42c0-b163-a883ff2ac4fb");

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);

            _mockPostEntityRepo = _mockRepository.Create<IRepository<PostEntity>>();
            _mockPostTagEntityRepo = _mockRepository.Create<IRepository<PostTagEntity>>();
            _mockPostCategoryRepo = _mockRepository.Create<IRepository<PostCategoryEntity>>();
        }

        [Test]
        public void CountPublic_OK()
        {
            var handler = new CountPostQueryHandler(_mockPostEntityRepo.Object, _mockPostTagEntityRepo.Object,
                _mockPostCategoryRepo.Object);
            handler.Handle(new(CountType.Public), default);

            _mockPostEntityRepo.Verify(p => p.Count(It.IsAny<Expression<Func<PostEntity, bool>>>()));
        }

        [Test]
        public void CountByCategory_OK()
        {
            var handler = new CountPostQueryHandler(_mockPostEntityRepo.Object, _mockPostTagEntityRepo.Object,
                _mockPostCategoryRepo.Object);
            handler.Handle(new(CountType.Category, Uid), default);

            _mockPostCategoryRepo.Verify(p => p.Count(It.IsAny<Expression<Func<PostCategoryEntity, bool>>>()));
        }

        [Test]
        public void CountByTag_OK()
        {
            var handler = new CountPostQueryHandler(_mockPostEntityRepo.Object, _mockPostTagEntityRepo.Object,
                _mockPostCategoryRepo.Object);
            handler.Handle(new(CountType.Tag, tagId: 996), default);

            _mockPostTagEntityRepo.Verify(p => p.Count(It.IsAny<Expression<Func<PostTagEntity, bool>>>()));
        }

        [Test]
        public void CountByFeatured_OK()
        {
            var handler = new CountPostQueryHandler(_mockPostEntityRepo.Object, _mockPostTagEntityRepo.Object,
                _mockPostCategoryRepo.Object);
            handler.Handle(new(CountType.Featured), default);

            _mockPostEntityRepo.Verify(p => p.Count(It.IsAny<Expression<Func<PostEntity, bool>>>()));
        }
    }
}
