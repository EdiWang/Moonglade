using MemoryCache.Testing.Moq;
using Moonglade.Caching;
using Moonglade.Core.CategoryFeature;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;
using Moq;
using NUnit.Framework;
using System.Linq.Expressions;

namespace Moonglade.Core.Tests;

[TestFixture]
public class CategoryTests
{
    private MockRepository _mockRepository;

    private Mock<IRepository<CategoryEntity>> _mockCatRepo;
    private Mock<IRepository<PostCategoryEntity>> _mockRepositoryPostCategoryEntity;
    private Mock<IBlogCache> _mockBlogCache;

    [SetUp]
    public void SetUp()
    {
        _mockRepository = new(MockBehavior.Default);

        _mockCatRepo = _mockRepository.Create<IRepository<CategoryEntity>>();
        _mockRepositoryPostCategoryEntity = _mockRepository.Create<IRepository<PostCategoryEntity>>();
        _mockBlogCache = _mockRepository.Create<IBlogCache>();
    }

    [Test]
    public async Task GetAll_OK()
    {
        var mockedCache = Create.MockedMemoryCache();
        var memBc = new BlogMemoryCache(mockedCache);

        var handler = new GetCategoriesQueryHandler(_mockCatRepo.Object, memBc);
        var result = await handler.Handle(new(), default);

        _mockCatRepo.Verify(p => p.SelectAsync(It.IsAny<Expression<Func<CategoryEntity, Category>>>()));
    }

    [Test]
    public async Task Get_ByName()
    {
        var handler = new GetCategoryByRouteCommandHandler(_mockCatRepo.Object);
        await handler.Handle(new("work996"), default);

        _mockCatRepo.Verify(p => p.SelectFirstOrDefaultAsync(It.IsAny<CategorySpec>(), It.IsAny<Expression<Func<CategoryEntity, Category>>>()));
    }

    [Test]
    public async Task Get_ById()
    {
        var handler = new GetCategoryByIdCommandHandler(_mockCatRepo.Object);
        var result = await handler.Handle(new(Guid.Empty), default);

        _mockCatRepo.Verify(p => p.SelectFirstOrDefaultAsync(It.IsAny<CategorySpec>(), It.IsAny<Expression<Func<CategoryEntity, Category>>>()));
    }

    [Test]
    public async Task CreateAsync_Exists()
    {
        _mockCatRepo.Setup(p => p.Any(It.IsAny<Expression<Func<CategoryEntity, bool>>>())).Returns(true);

        var handler =
            new CreateCategoryCommandHandler(_mockCatRepo.Object, _mockBlogCache.Object);
        await handler.Handle(new(new()
        {
            DisplayName = "Work 996",
            RouteName = "work-996"
        }), default);

        _mockCatRepo.Verify(p => p.AddAsync(It.IsAny<CategoryEntity>()), Times.Never);
    }

    [Test]
    public async Task CreateAsync_Success()
    {
        _mockCatRepo.Setup(p => p.Any(It.IsAny<Expression<Func<CategoryEntity, bool>>>())).Returns(false);

        var handler =
            new CreateCategoryCommandHandler(_mockCatRepo.Object, _mockBlogCache.Object);
        await handler.Handle(new(new()
        {
            DisplayName = "Work 996",
            RouteName = "work-996",
            Note = "Fubao"
        }), default);

        _mockCatRepo.Verify(p => p.AddAsync(It.IsAny<CategoryEntity>()));
        _mockBlogCache.Verify(p => p.Remove(CacheDivision.General, "allcats"));
    }

    [Test]
    public async Task DeleteAsync_NotExists()
    {
        _mockCatRepo.Setup(c => c.Any(It.IsAny<Expression<Func<CategoryEntity, bool>>>()))
            .Returns(false);

        var handler = new DeleteCategoryCommandHandler(_mockCatRepo.Object,
            _mockRepositoryPostCategoryEntity.Object, _mockBlogCache.Object);
        await handler.Handle(new(Guid.Empty), default);

        _mockCatRepo.Verify();
    }

    [Test]
    public async Task DeleteAsync_Success()
    {
        _mockCatRepo.Setup(c => c.Any(It.IsAny<Expression<Func<CategoryEntity, bool>>>()))
            .Returns(true);

        _mockCatRepo.Setup(p => p.GetAsync(It.IsAny<Expression<Func<CategoryEntity, bool>>>()))
            .Returns(Task.FromResult(new CategoryEntity()));

        var handler = new DeleteCategoryCommandHandler(_mockCatRepo.Object,
            _mockRepositoryPostCategoryEntity.Object, _mockBlogCache.Object);
        await handler.Handle(new(Guid.Empty), default);

        _mockCatRepo.Verify(p => p.DeleteAsync(It.IsAny<Guid>()));
        _mockBlogCache.Verify(p => p.Remove(CacheDivision.General, "allcats"));
    }

    [Test]
    public async Task UpdateAsync_NullCat()
    {
        _mockCatRepo.Setup(p => p.GetAsync(It.IsAny<Guid>()))
            .Returns(ValueTask.FromResult((CategoryEntity)null));

        var handler =
            new UpdateCategoryCommandHandler(_mockCatRepo.Object, _mockBlogCache.Object);
        await handler.Handle(new(Guid.Empty, null), default);

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

        var handler =
            new UpdateCategoryCommandHandler(_mockCatRepo.Object, _mockBlogCache.Object);
        await handler.Handle(new(Guid.Empty, new()
        {
            Note = "Fubao",
            RouteName = "fubao",
            DisplayName = "ICU"
        }), default);

        _mockCatRepo.Verify(p => p.UpdateAsync(It.IsAny<CategoryEntity>()));
    }
}