using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using MemoryCache.Testing.Moq;
using Moonglade.Caching;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;
using Moq;
using NUnit.Framework;

namespace Moonglade.Core.Tests
{
    [TestFixture]
    public class CategoryServiceTests
    {
        private MockRepository _mockRepository;

        private Mock<IRepository<CategoryEntity>> _mockCatRepo;
        private Mock<IRepository<PostCategoryEntity>> _mockRepositoryPostCategoryEntity;
        private Mock<IBlogAudit> _mockBlogAudit;
        private Mock<IBlogCache> _mockBlogCache;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);

            _mockCatRepo = _mockRepository.Create<IRepository<CategoryEntity>>();
            _mockRepositoryPostCategoryEntity = _mockRepository.Create<IRepository<PostCategoryEntity>>();
            _mockBlogAudit = _mockRepository.Create<IBlogAudit>();
            _mockBlogCache = _mockRepository.Create<IBlogCache>();
        }

        private CategoryService CreateService(IBlogCache cache = null)
        {
            return new(
                _mockCatRepo.Object,
                _mockRepositoryPostCategoryEntity.Object,
                _mockBlogAudit.Object,
                cache ?? _mockBlogCache.Object);
        }

        [Test]
        public async Task GetAll_OK()
        {
            var mockedCache = Create.MockedMemoryCache();
            var memBc = new BlogMemoryCache(mockedCache);

            var svc = CreateService(memBc);

            var result = await svc.GetAll();

            _mockCatRepo.Verify(p => p.SelectAsync(It.IsAny<Expression<Func<CategoryEntity, Category>>>()));
        }

        [Test]
        public async Task Get_ByName()
        {
            var svc = CreateService();
            await svc.Get("work996");

            _mockCatRepo.Verify(p => p.SelectFirstOrDefaultAsync(It.IsAny<CategorySpec>(), It.IsAny<Expression<Func<CategoryEntity, Category>>>()));
        }

        [Test]
        public async Task Get_ById()
        {
            var svc = CreateService();
            await svc.Get(Guid.Empty);

            _mockCatRepo.Verify(p => p.SelectFirstOrDefaultAsync(It.IsAny<CategorySpec>(), It.IsAny<Expression<Func<CategoryEntity, Category>>>()));
        }

        [Test]
        public async Task CreateAsync_Exists()
        {
            _mockCatRepo.Setup(p => p.Any(It.IsAny<Expression<Func<CategoryEntity, bool>>>())).Returns(true);

            var svc = CreateService();
            await svc.CreateAsync(new("Work 996", "work-996"));

            _mockCatRepo.Verify(p => p.AddAsync(It.IsAny<CategoryEntity>()), Times.Never);
        }

        [Test]
        public async Task CreateAsync_Success()
        {
            _mockCatRepo.Setup(p => p.Any(It.IsAny<Expression<Func<CategoryEntity, bool>>>())).Returns(false);

            var svc = CreateService();
            await svc.CreateAsync(new("Work 996", "work-996")
            {
                Note = "Fubao"
            });

            _mockCatRepo.Verify(p => p.AddAsync(It.IsAny<CategoryEntity>()));
            _mockBlogCache.Verify(p => p.Remove(CacheDivision.General, "allcats"));
        }

        [Test]
        public async Task DeleteAsync_NotExists()
        {
            _mockCatRepo.Setup(c => c.Any(It.IsAny<Expression<Func<CategoryEntity, bool>>>()))
                .Returns(false);

            var svc = CreateService();
            await svc.DeleteAsync(Guid.Empty);

            _mockCatRepo.Verify();
        }

        [Test]
        public async Task DeleteAsync_Success()
        {
            _mockCatRepo.Setup(c => c.Any(It.IsAny<Expression<Func<CategoryEntity, bool>>>()))
                .Returns(true);

            _mockCatRepo.Setup(p => p.GetAsync(It.IsAny<Expression<Func<CategoryEntity, bool>>>()))
                .Returns(Task.FromResult(new CategoryEntity()));

            var svc = CreateService();
            await svc.DeleteAsync(Guid.Empty);

            _mockCatRepo.Verify(p => p.DeleteAsync(It.IsAny<Guid>()));
            _mockBlogCache.Verify(p => p.Remove(CacheDivision.General, "allcats"));
        }

        [Test]
        public async Task UpdateAsync_NullCat()
        {
            _mockCatRepo.Setup(p => p.GetAsync(It.IsAny<Guid>()))
                .Returns(ValueTask.FromResult((CategoryEntity)null));

            var svc = CreateService();
            await svc.UpdateAsync(Guid.Empty, new(null, null));

            _mockCatRepo.Verify(p => p.UpdateAsync(It.IsAny<CategoryEntity>()), Times.Never);
        }

        [Test]
        public async Task UpdateAsync_OK()
        {
            _mockCatRepo.Setup(p => p.GetAsync(It.IsAny<Guid>()))
                .Returns(ValueTask.FromResult(new CategoryEntity()
                {
                    Id = Guid.Empty,
                    DisplayName = "Work 996",
                    Note = "Fubao",
                    RouteName = "work-996"
                }));

            var svc = CreateService();
            await svc.UpdateAsync(Guid.Empty, new("Fubao", "fubao")
            {
                Note = "ICU"
            });

            _mockCatRepo.Verify(p => p.UpdateAsync(It.IsAny<CategoryEntity>()));
        }
    }
}
