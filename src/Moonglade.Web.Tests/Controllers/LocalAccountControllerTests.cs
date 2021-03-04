using Moonglade.Auth;
using Moonglade.Web.Controllers;
using Moq;
using NUnit.Framework;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
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
            _mockRepository = new(MockBehavior.Strict);
            _mockLocalAccountService = _mockRepository.Create<ILocalAccountService>();
        }

        private LocalAccountController CreateLocalAccountController()
        {
            return new(_mockLocalAccountService.Object);
        }


    }
}
