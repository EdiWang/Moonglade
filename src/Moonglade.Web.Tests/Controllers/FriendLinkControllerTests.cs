using Microsoft.AspNetCore.Mvc;
using Moonglade.FriendLink;
using Moonglade.Web.Controllers;
using Moq;
using NUnit.Framework;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Moonglade.Web.Models;

namespace Moonglade.Web.Tests.Controllers
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class FriendLinkControllerTests
    {
        private MockRepository _mockRepository;

        private Mock<IFriendLinkService> _mockFriendLinkService;

        private static readonly Guid Uid = Guid.Parse("76169567-6ff3-42c0-b163-a883ff2ac4fb");
        private FriendLinkEditModel _friendlinkEditViewModel = new()
        {
            Id = Uid,
            LinkUrl = "https://996.icu",
            Title = "996 ICU"
        };

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);

            _mockFriendLinkService = _mockRepository.Create<IFriendLinkService>();
        }

        private FriendLinkController CreateFriendLinkController()
        {
            return new(_mockFriendLinkService.Object);
        }

        [Test]
        public async Task Create_InvalidModel()
        {
            var ctl = CreateFriendLinkController();
            ctl.ModelState.AddModelError("Title", "Title is required");

            var result = await ctl.Create(_friendlinkEditViewModel);
            Assert.IsInstanceOf<BadRequestObjectResult>(result);
        }

        [Test]
        public async Task Create_OK()
        {
            var ctl = CreateFriendLinkController();

            var result = await ctl.Create(_friendlinkEditViewModel);

            Assert.IsInstanceOf<OkObjectResult>(result);
            _mockFriendLinkService.Verify(p => p.AddAsync(It.IsAny<string>(), It.IsAny<string>()));
        }

        [Test]
        public async Task Get_LinkNull()
        {
            _mockFriendLinkService.Setup(p => p.GetAsync(Guid.Empty)).Returns(Task.FromResult((Link)null));
            var ctl = CreateFriendLinkController();
            var result = await ctl.Get(Guid.Empty);

            Assert.IsInstanceOf<NotFoundResult>(result);
        }

        [Test]
        public async Task Get_Exception()
        {
            _mockFriendLinkService.Setup(p => p.GetAsync(Guid.Empty)).Throws(new ArgumentOutOfRangeException());

            var ctl = CreateFriendLinkController();
            var result = await ctl.Get(Guid.Empty);

            Assert.IsInstanceOf<StatusCodeResult>(result);
            Assert.AreEqual(500, ((StatusCodeResult)result).StatusCode);
        }

        [Test]
        public async Task Get_OK()
        {
            _mockFriendLinkService.Setup(p => p.GetAsync(Uid)).Returns(Task.FromResult(new Link()));
            var ctl = CreateFriendLinkController();
            var result = await ctl.Get(Uid);

            Assert.IsInstanceOf<OkObjectResult>(result);
        }

        [Test]
        public async Task Edit_InvalidModel()
        {
            var ctl = CreateFriendLinkController();
            ctl.ModelState.AddModelError("Title", "Title is required");

            var result = await ctl.Edit(_friendlinkEditViewModel);
            Assert.IsInstanceOf<BadRequestObjectResult>(result);
        }

        [Test]
        public async Task Edit_OK()
        {
            var ctl = CreateFriendLinkController();

            var result = await ctl.Edit(_friendlinkEditViewModel);

            Assert.IsInstanceOf<OkObjectResult>(result);
            _mockFriendLinkService.Verify(p => p.UpdateAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()));
        }

        [Test]
        public async Task Delete_EmptyId()
        {
            var ctl = CreateFriendLinkController();
            var result = await ctl.Delete(Guid.Empty);

            Assert.IsInstanceOf<BadRequestObjectResult>(result);
        }

        [Test]
        public async Task Delete_OK()
        {
            var ctl = CreateFriendLinkController();
            var result = await ctl.Delete(Uid);

            Assert.IsInstanceOf<OkResult>(result);
            _mockFriendLinkService.Verify(p => p.DeleteAsync(It.IsAny<Guid>()));
        }
    }
}
