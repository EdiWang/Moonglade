using Moonglade.Menus;
using Moonglade.Web.Controllers;
using Moq;
using NUnit.Framework;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Moonglade.Web.Tests.Controllers
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class MenuControllerTests
    {
        private MockRepository _mockRepository;

        private Mock<IMenuService> _mockMenuService;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);

            _mockMenuService = _mockRepository.Create<IMenuService>();
        }

        private MenuController CreateMenuController()
        {
            return new(_mockMenuService.Object);
        }
    }
}
