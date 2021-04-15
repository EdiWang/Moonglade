using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Moonglade.Menus;
using Moonglade.Web.Pages.Admin;
using Moq;
using NUnit.Framework;

namespace Moonglade.Web.Tests.Pages.Admin
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class MenuModelTests
    {
        private MockRepository _mockRepository;
        private Mock<IMenuService> _mockMenuService;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new MockRepository(MockBehavior.Default);
            _mockMenuService = _mockRepository.Create<IMenuService>();
        }

        private MenuModel CreateMenuModel()
        {
            return new(_mockMenuService.Object);
        }

        [Test]
        public async Task OnGet_StateUnderTest_ExpectedBehavior()
        {
            IReadOnlyList<Menu> menus = new List<Menu>();
            _mockMenuService.Setup(p => p.GetAllAsync()).Returns(Task.FromResult(menus));

            var menuModel = CreateMenuModel();
            await menuModel.OnGet();

            Assert.IsNotNull(menuModel.MenuItems);
        }
    }
}
