using Moonglade.Menus;
using Moonglade.Web.Controllers;
using Moq;
using NUnit.Framework;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moonglade.Web.Models;

namespace Moonglade.Web.Tests.Controllers
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class MenuControllerTests
    {
        private MockRepository _mockRepository;
        private Mock<IMenuService> _mockMenuService;

        private MenuEditViewModel _menuEditViewModel = new()
        {
            Id = Guid.Empty,
            DisplayOrder = 996,
            Icon = "work-996",
            IsOpenInNewTab = true,
            Title = "Work 996",
            Url = "/work/996"
        };

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

        [Test]
        public async Task Create_InvalidModel()
        {
            var ctl = CreateMenuController();
            ctl.ModelState.AddModelError("Title", "Title is required");

            var result = await ctl.Create(_menuEditViewModel);
            Assert.IsInstanceOf<BadRequestObjectResult>(result);
        }
    }
}
