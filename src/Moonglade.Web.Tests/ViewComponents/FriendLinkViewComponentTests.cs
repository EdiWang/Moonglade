using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Moonglade.FriendLink;
using Moonglade.Web.ViewComponents;
using Moq;
using NUnit.Framework;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ViewComponents;

namespace Moonglade.Web.Tests.ViewComponents
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class FriendLinkViewComponentTests
    {
        private MockRepository _mockRepository;

        private Mock<ILogger<FriendLinkViewComponent>> _mockLogger;
        private Mock<IFriendLinkService> _mockFriendLinkService;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);

            _mockLogger = _mockRepository.Create<ILogger<FriendLinkViewComponent>>();
            _mockFriendLinkService = _mockRepository.Create<IFriendLinkService>();
        }

        private FriendLinkViewComponent CreateComponent()
        {
            return new(
                _mockLogger.Object,
                _mockFriendLinkService.Object);
        }

        [Test]
        public async Task InvokeAsync_Exception()
        {
            _mockFriendLinkService.Setup(p => p.GetAllAsync()).Throws(new("996"));

            var component = CreateComponent();
            var result = await component.InvokeAsync();

            Assert.IsInstanceOf<ViewViewComponentResult>(result);

            var message = ((ViewViewComponentResult)result).ViewData["ComponentErrorMessage"];
            Assert.AreEqual("996", message);
        }
    }
}
