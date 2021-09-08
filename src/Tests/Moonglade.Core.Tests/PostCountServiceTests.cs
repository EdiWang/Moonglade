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

        private PostCountService CreateService()
        {
            return new(
                _mockPostEntityRepo.Object,
                _mockPostTagEntityRepo.Object,
                _mockPostCategoryRepo.Object);
        }

        [Test]
        public void CountPublic_OK()
        {
            var svc = CreateService();
            svc.CountPublic();

            _mockPostEntityRepo.Verify(p => p.Count(It.IsAny<Expression<Func<PostEntity, bool>>>()));
        }

        [Test]
        public void CountByCategory_OK()
        {
            var svc = CreateService();
            svc.CountByCategory(Uid);

            _mockPostCategoryRepo.Verify(p => p.Count(It.IsAny<Expression<Func<PostCategoryEntity, bool>>>()));
        }

        [Test]
        public void CountByTag_OK()
        {
            var svc = CreateService();
            svc.CountByTag(996);

            _mockPostTagEntityRepo.Verify(p => p.Count(It.IsAny<Expression<Func<PostTagEntity, bool>>>()));
        }

        [Test]
        public void CountByFeatured_OK()
        {
            var svc = CreateService();
            svc.CountByFeatured();

            _mockPostEntityRepo.Verify(p => p.Count(It.IsAny<Expression<Func<PostEntity, bool>>>()));
        }
    }
}
