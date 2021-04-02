using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Moonglade.Auditing;
using Moonglade.Caching;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;
using Moq;
using NUnit.Framework;

namespace Moonglade.Core.Tests
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class CategoryServiceTests
    {
        private MockRepository _mockRepository;

        private Mock<IRepository<CategoryEntity>> _mockRepositoryCategoryEntity;
        private Mock<IRepository<PostCategoryEntity>> _mockRepositoryPostCategoryEntity;
        private Mock<IBlogAudit> _mockBlogAudit;
        private Mock<IBlogCache> _mockBlogCache;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);

            _mockRepositoryCategoryEntity = _mockRepository.Create<IRepository<CategoryEntity>>();
            _mockRepositoryPostCategoryEntity = _mockRepository.Create<IRepository<PostCategoryEntity>>();
            _mockBlogAudit = _mockRepository.Create<IBlogAudit>();
            _mockBlogCache = _mockRepository.Create<IBlogCache>();
        }

        private CategoryService CreateService()
        {
            return new(
                _mockRepositoryCategoryEntity.Object,
                _mockRepositoryPostCategoryEntity.Object,
                _mockBlogAudit.Object,
                _mockBlogCache.Object);
        }

        [Test]
        public async Task Get_ByName()
        {
            var svc = CreateService();
            await svc.Get("work996");

            _mockRepositoryCategoryEntity.Verify(p => p.SelectFirstOrDefaultAsync(It.IsAny<CategorySpec>(), It.IsAny<Expression<Func<CategoryEntity, Category>>>(), true));
        }

        [Test]
        public async Task Get_ById()
        {
            var svc = CreateService();
            await svc.Get(Guid.Empty);

            _mockRepositoryCategoryEntity.Verify(p => p.SelectFirstOrDefaultAsync(It.IsAny<CategorySpec>(), It.IsAny<Expression<Func<CategoryEntity, Category>>>(), true));
        }

        [Test]
        public async Task DeleteAsync_NotExists()
        {
            _mockRepositoryCategoryEntity.Setup(c => c.Any(It.IsAny<Expression<Func<CategoryEntity, bool>>>()))
                .Returns(false);

            var svc = CreateService();
            await svc.DeleteAsync(Guid.Empty);

            _mockRepositoryCategoryEntity.Verify();
        }
    }
}
