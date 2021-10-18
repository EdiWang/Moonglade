using MediatR;
using Moonglade.Menus;
using Moonglade.Web.Pages.Admin;
using Moq;
using NUnit.Framework;

namespace Moonglade.Web.Tests.Pages.Admin;

[TestFixture]

public class MenuModelTests
{
    private MockRepository _mockRepository;
    private Mock<IMediator> _mockMediator;

    [SetUp]
    public void SetUp()
    {
        _mockRepository = new(MockBehavior.Default);
        _mockMediator = _mockRepository.Create<IMediator>();
    }

    private MenuModel CreateMenuModel()
    {
        return new(_mockMediator.Object);
    }

    [Test]
    public async Task OnGet_StateUnderTest_ExpectedBehavior()
    {
        IReadOnlyList<Menu> menus = new List<Menu>();
        _mockMediator.Setup(p => p.Send(It.IsAny<GetAllMenusQuery>(), default)).Returns(Task.FromResult(menus));

        var menuModel = CreateMenuModel();
        await menuModel.OnGet();

        Assert.IsNotNull(menuModel.MenuItems);
    }
}