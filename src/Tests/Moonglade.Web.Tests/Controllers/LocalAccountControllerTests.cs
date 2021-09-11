using MediatR;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moonglade.Auth;
using Moonglade.Web.Controllers;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Moonglade.Web.Tests.Controllers
{
    [TestFixture]
    public class LocalAccountControllerTests
    {
        private MockRepository _mockRepository;

        private Mock<ILocalAccountService> _mockLocalAccountService;
        private Mock<IMediator> _mockMediator;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);
            _mockLocalAccountService = _mockRepository.Create<ILocalAccountService>();
            _mockMediator = _mockRepository.Create<IMediator>();
        }

        private LocalAccountController CreateLocalAccountController()
        {
            return new(_mockLocalAccountService.Object, _mockMediator.Object);
        }

        [Test]
        public async Task Create_AlreadyExists()
        {
            _mockLocalAccountService.Setup(p => p.Exist(It.IsAny<string>())).Returns(true);

            var ctl = CreateLocalAccountController();
            var result = await ctl.Create(new()
            {
                Username = FakeData.ShortString2,
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
                Username = FakeData.ShortString2,
                Password = "icu"
            });

            Assert.IsInstanceOf<OkResult>(result);
        }

        [Test]
        public async Task Delete_NullCurrentUser()
        {
            var ctl = CreateLocalAccountController();
            ctl.ControllerContext = new()
            {
                HttpContext = new DefaultHttpContext
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
            var ctl = CreateLocalAccountController();
            ctl.ControllerContext = new()
            {
                HttpContext = new DefaultHttpContext
                {
                    User = GetClaimsPrincipal("76169567-6ff3-42c0-b163-a883ff2ac4fb")
                }
            };

            var result = await ctl.Delete(Guid.Parse("76169567-6ff3-42c0-b163-a883ff2ac4fb"));
            Assert.IsInstanceOf<ConflictObjectResult>(result);
        }

        [Test]
        public async Task Delete_LastUser()
        {
            _mockLocalAccountService.Setup(p => p.Count()).Returns(1);

            var ctl = CreateLocalAccountController();
            ctl.ControllerContext = new()
            {
                HttpContext = new DefaultHttpContext
                {
                    User = GetClaimsPrincipal(FakeData.ShortString2)
                }
            };

            var result = await ctl.Delete(Guid.Parse("76169567-6ff3-42c0-b163-a883ff2ac4fb"));
            Assert.IsInstanceOf<ConflictObjectResult>(result);
        }

        [Test]
        public async Task Delete_OK()
        {
            _mockLocalAccountService.Setup(p => p.Count()).Returns(996);
            var ctl = CreateLocalAccountController();
            ctl.ControllerContext = new()
            {
                HttpContext = new DefaultHttpContext
                {
                    User = GetClaimsPrincipal(FakeData.ShortString2)
                }
            };

            var result = await ctl.Delete(Guid.Parse("76169567-6ff3-42c0-b163-a883ff2ac4fb"));
            Assert.IsInstanceOf<NoContentResult>(result);
        }

        [Test]
        public async Task ResetPassword_WeakPassword()
        {
            var ctl = CreateLocalAccountController();
            var result = await ctl.ResetPassword(Guid.Parse("76169567-6ff3-42c0-b163-a883ff2ac4fb"), "996007251404");

            Assert.IsInstanceOf<ConflictObjectResult>(result);
        }

        [Test]
        public async Task ResetPassword_OK()
        {
            var ctl = CreateLocalAccountController();
            var result = await ctl.ResetPassword(Guid.Parse("76169567-6ff3-42c0-b163-a883ff2ac4fb"), "Admin@1234");

            Assert.IsInstanceOf<NoContentResult>(result);
            _mockLocalAccountService.Verify(p => p.UpdatePasswordAsync(It.IsAny<Guid>(), It.IsAny<string>()));
        }

        private ClaimsPrincipal GetClaimsPrincipal(string uid)
        {
            var claims = new List<Claim>
            {
                new (ClaimTypes.Name, "moonglade"),
                new (ClaimTypes.Role, "Administrator"),
                new ("uid", uid)
            };
            var ci = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var p = new ClaimsPrincipal(ci);

            return p;
        }
    }
}
