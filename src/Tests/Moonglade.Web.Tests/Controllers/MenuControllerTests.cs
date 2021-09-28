using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moonglade.Menus;
using Moonglade.Web.Controllers;
using Moq;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace Moonglade.Web.Tests.Controllers
{
    [TestFixture]
    public class MenuControllerTests
    {
        private MockRepository _mockRepository;
        private Mock<IMediator> _mockMediator;

        private readonly Guid _noneEmptyId = Guid.Parse("4ac8e62e-92f1-449d-8feb-ee42a99caa09");

        private readonly EditMenuRequest _editMenuRequest = new()
        {
            Id = FakeData.Uid1,
            DisplayOrder = FakeData.Int2,
            Icon = FakeData.Slug2,
            IsOpenInNewTab = true,
            Title = FakeData.Title3,
            Url = "/work/996",
            SubMenus = new EditSubMenuRequest[]
            {
                new ()
                {
                    Id = FakeData.Uid2, IsOpenInNewTab = true, Title = "SM", Url = "https://996.icu"
                }
            }
        };

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);
            _mockMediator = _mockRepository.Create<IMediator>();
        }

        private MenuController CreateMenuController()
        {
            return new(_mockMediator.Object);
        }

        [Test]
        public async Task Create_OK()
        {
            var ctl = CreateMenuController();

            var result = await ctl.Create(_editMenuRequest);
            Assert.IsInstanceOf<OkObjectResult>(result);

            _mockMediator.Verify(p => p.Send(It.IsAny<CreateMenuCommand>(), default));
        }

        [Test]
        public async Task Delete_OK()
        {
            var ctl = CreateMenuController();
            var result = await ctl.Delete(_noneEmptyId);

            Assert.IsInstanceOf<NoContentResult>(result);
            _mockMediator.Verify(p => p.Send(It.IsAny<DeleteMenuCommand>(), default));
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
            _mockMediator.Setup(p => p.Send(It.IsAny<GetMenuQuery>(), default))
                .Returns(Task.FromResult((Menu)null));

            var ctl = CreateMenuController();
            var result = await ctl.Edit(_noneEmptyId);
            Assert.IsInstanceOf<NotFoundResult>(result);
        }

        [Test]
        public async Task Edit_Get_OK()
        {
            _mockMediator.Setup(p => p.Send(It.IsAny<GetMenuQuery>(), default))
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

        [Test]
        public async Task Edit_Post_ok()
        {
            var ctl = CreateMenuController();
            var result = await ctl.Edit(_editMenuRequest);

            Assert.IsInstanceOf<NoContentResult>(result);
            _mockMediator.Verify(p => p.Send(It.IsAny<UpdateMenuCommand>(), default));
        }
    }
}
