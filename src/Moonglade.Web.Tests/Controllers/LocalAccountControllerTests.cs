using Moonglade.Auth;
using Moonglade.Web.Controllers;
using Moq;
using NUnit.Framework;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moonglade.Web.Models;
using Moonglade.Web.Models.Settings;

namespace Moonglade.Web.Tests.Controllers
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class LocalAccountControllerTests
    {
        private MockRepository _mockRepository;

        private Mock<ILocalAccountService> _mockLocalAccountService;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);
            _mockLocalAccountService = _mockRepository.Create<ILocalAccountService>();
        }

        private LocalAccountController CreateLocalAccountController()
        {
            return new(_mockLocalAccountService.Object);
        }

        [Test]
        public async Task Create_InvalidModel()
        {
            var ctl = CreateLocalAccountController();
            ctl.ModelState.AddModelError("", "996");

            var result = await ctl.Create(new()
            {
                Username = "996",
                Password = "icu"
            });

            Assert.IsInstanceOf<BadRequestObjectResult>(result);
        }

        [Test]
        public async Task Create_AlreadyExists()
        {
            _mockLocalAccountService.Setup(p => p.Exist(It.IsAny<string>())).Returns(true);

            var ctl = CreateLocalAccountController();
            var result = await ctl.Create(new()
            {
                Username = "996",
                Password = "icu"
            });

            Assert.IsInstanceOf<ConflictObjectResult>(result);
        }

        [Test]
        public async Task Create_OK()
        {
            _mockLocalAccountService.Setup(p => p.Exist(It.IsAny<string>())).Returns(false);

            var ctl = CreateLocalAccountController();
            var result = await ctl.Create(new()
            {
                Username = "996",
                Password = "icu"
            });

            Assert.IsInstanceOf<OkResult>(result);
        }
    }
}
