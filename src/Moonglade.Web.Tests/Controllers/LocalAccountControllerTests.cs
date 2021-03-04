using Moonglade.Auth;
using Moonglade.Web.Controllers;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
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

        [Test]
        public async Task Delete_EmptyId()
        {
            var ctl = CreateLocalAccountController();
            var result = await ctl.Delete(Guid.Empty);

            Assert.IsInstanceOf<BadRequestObjectResult>(result);
        }

        [Test]
        public async Task Delete_NullCurrentUser()
        {
            var ctl = CreateLocalAccountController();
            ctl.ControllerContext = new()
            {
                HttpContext = new DefaultHttpContext()
                {
                    User = new()
                }
            };

            var result = await ctl.Delete(Guid.Parse("76169567-6ff3-42c0-b163-a883ff2ac4fb"));
            Assert.IsInstanceOf<ObjectResult>(result);

            Assert.AreEqual(500, ((ObjectResult)result).StatusCode);
        }

        [Test]
        public async Task Delete_CurrentUser()
        {
            var claims = new List<Claim>
            {
                new (ClaimTypes.Name, "moonglade"),
                new (ClaimTypes.Role, "Administrator"),
                new ("uid", "76169567-6ff3-42c0-b163-a883ff2ac4fb")
            };
            var ci = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var p = new ClaimsPrincipal(ci);

            var ctl = CreateLocalAccountController();
            ctl.ControllerContext = new()
            {
                HttpContext = new DefaultHttpContext
                {
                    User = p
                }
            };

            var result = await ctl.Delete(Guid.Parse("76169567-6ff3-42c0-b163-a883ff2ac4fb"));
            Assert.IsInstanceOf<ConflictObjectResult>(result);
        }
    }
}
