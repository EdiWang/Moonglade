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
        private Guid _noneEmptyId = Guid.Parse("4ac8e62e-92f1-449d-8feb-ee42a99caa09");

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

        [Test]
        public async Task Create_OK()
        {
            var ctl = CreateMenuController();

            var result = await ctl.Create(_menuEditViewModel);
            Assert.IsInstanceOf<OkObjectResult>(result);

            _mockMenuService.Verify(p => p.CreateAsync(It.IsAny<UpdateMenuRequest>()));
        }

        [Test]
        public async Task Delete_EmptyId()
        {
            var ctl = CreateMenuController();
            var result = await ctl.Delete(Guid.Empty);
            Assert.IsInstanceOf<BadRequestResult>(result);
        }

        [Test]
        public async Task Delete_OK()
        {
            var ctl = CreateMenuController();
            var result = await ctl.Delete(_noneEmptyId);
            
            Assert.IsInstanceOf<OkResult>(result);
            _mockMenuService.Verify(p => p.DeleteAsync(It.IsAny<Guid>()));
        }

        [Test]
        public async Task Edit_EmptyId()
        {
            var ctl = CreateMenuController();
            var result = await ctl.Edit(Guid.Empty);
            Assert.IsInstanceOf<NotFoundResult>(result);
        }

        [Test]
        public async Task Edit_NullMenu()
        {
            _mockMenuService.Setup(p => p.GetAsync(_noneEmptyId))
                .Returns(Task.FromResult((Menu) null));

            var ctl = CreateMenuController();
            var result = await ctl.Edit(_noneEmptyId);
            Assert.IsInstanceOf<NotFoundResult>(result);
        }

        [Test]
        public async Task Edit_Get_OK()
        {
            _mockMenuService.Setup(p => p.GetAsync(_noneEmptyId))
                .Returns(Task.FromResult(new Menu
                {
                    Id = _noneEmptyId,
                    DisplayOrder = 996,
                    Icon = "jack-ma-pig",
                    Url = "/fuck/996",
                    IsOpenInNewTab = true,
                    Title = "Fubao"
                }));

            var ctl = CreateMenuController();
            var result = await ctl.Edit(_noneEmptyId);

            Assert.IsInstanceOf<OkObjectResult>(result);
        }
    }
}
