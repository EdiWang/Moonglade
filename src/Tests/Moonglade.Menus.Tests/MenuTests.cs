using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moq;
using NUnit.Framework;
using System.Linq.Expressions;

namespace Moonglade.Menus.Tests;

[TestFixture]
public class MenuTests
{
    private MockRepository _mockRepository;

    private Mock<IRepository<MenuEntity>> _mockMenuRepository;

    private readonly MenuEntity _menu = new()
    {
        Id = Guid.Parse("478ff468-a0cc-4f05-a5d8-b1dacdc695dd"),
        DisplayOrder = 996,
        Icon = "work-996 ",
        IsOpenInNewTab = true,
        Title = "Work 996 ",
        Url = "/work/996",
        SubMenus = new List<SubMenuEntity>
        {
            new ()
            {
                Id = Guid.Parse("23ca73fd-a2ed-4671-84bb-16826189f4fb"),
                MenuId = Guid.Parse("478ff468-a0cc-4f05-a5d8-b1dacdc695dd"),
                IsOpenInNewTab = true,
                Title = "251 Today",
                Url = "https://251.today"
            }
        }
    };

    [SetUp]
    public void SetUp()
    {
        _mockRepository = new(MockBehavior.Default);

        _mockMenuRepository = _mockRepository.Create<IRepository<MenuEntity>>();
    }

    [Test]
    public async Task GetAsync_Null()
    {
        _mockMenuRepository.Setup(p => p.GetAsync(It.IsAny<Guid>()))
            .Returns(ValueTask.FromResult((MenuEntity)null));

        var handler = new GetMenuQueryHandler(_mockMenuRepository.Object);
        var result = await handler.Handle(new(Guid.Empty), default);

        Assert.IsNull(result);
    }

    [Test]
    public async Task GetAllAsync_OK()
    {
        var handler = new GetAllMenusQueryHandler(_mockMenuRepository.Object);
        var result = await handler.Handle(new(), default);

        _mockMenuRepository.Verify(p => p.SelectAsync(It.IsAny<Expression<Func<MenuEntity, Menu>>>()));
    }

    [Test]
    public async Task GetAsync_EntityToMenuModel()
    {
        _mockMenuRepository.Setup(p => p.GetAsync(It.IsAny<Guid>()))
            .Returns(ValueTask.FromResult(_menu));

        var handler = new GetMenuQueryHandler(_mockMenuRepository.Object);
        var result = await handler.Handle(new(Guid.Empty), default);

        Assert.IsNotNull(result);
        Assert.AreEqual("work-996", result.Icon);
        Assert.AreEqual("Work 996", result.Title);
    }

    [Test]
    public void DeleteAsync_Null()
    {
        _mockMenuRepository.Setup(p => p.GetAsync(It.IsAny<Guid>()))
            .Returns(ValueTask.FromResult((MenuEntity)null));

        var handler = new DeleteMenuCommandHandler(_mockMenuRepository.Object);
        Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await handler.Handle(new(Guid.Empty), default);
        });
    }

    [Test]
    public async Task DeleteAsync_OK()
    {
        _mockMenuRepository.Setup(p => p.GetAsync(It.IsAny<Guid>()))
            .Returns(ValueTask.FromResult(_menu));

        var handler = new DeleteMenuCommandHandler(_mockMenuRepository.Object);
        await handler.Handle(new(Guid.Empty), default);
    }

    [Test]
    public async Task CreateAsync_OK()
    {
        var handler = new CreateMenuCommandHandler(_mockMenuRepository.Object);
        var result = await handler.Handle(new(new()
        {
            DisplayOrder = 996,
            Icon = "work-996",
            Title = "Work 996",
            IsOpenInNewTab = true,
            Url = "work/996",
            SubMenus = new[]
            {
                new EditSubMenuRequest
                {
                    IsOpenInNewTab = true,
                    Title = "251 Today",
                    Url = "https://251.today"
                }
            }
        }), default);

        Assert.AreNotEqual(Guid.Empty, result);
    }

    [Test]
    public void UpdateAsync_NullMenu()
    {
        _mockMenuRepository.Setup(p => p.GetAsync(It.IsAny<Guid>()))
            .Returns(ValueTask.FromResult((MenuEntity)null));

        var handler = new UpdateMenuCommandHandler(_mockMenuRepository.Object);
        Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await handler.Handle(new(Guid.Empty, new()), default);
        });
    }

    [Test]
    public async Task UpdateAsync_OK()
    {
        _mockMenuRepository.Setup(p => p.GetAsync(It.IsAny<Guid>()))
            .Returns(ValueTask.FromResult(_menu));

        var handler = new UpdateMenuCommandHandler(_mockMenuRepository.Object);

        await handler.Handle(new(Guid.Empty, new()
        {
            DisplayOrder = 996,
            Icon = "work-996",
            Title = "Work 996",
            IsOpenInNewTab = true,
            Url = "work/996",
            SubMenus = new[]
            {
                new EditSubMenuRequest
                {
                    IsOpenInNewTab = true,
                    Title = "251 Today",
                    Url = "https://251.today"
                }
            }
        }), default);
    }
}