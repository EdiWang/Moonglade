using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.Extensions.Logging;
using Moonglade.FriendLink;
using Moonglade.Web.ViewComponents;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediatR;

namespace Moonglade.Web.Tests.ViewComponents
{
    [TestFixture]
    public class FriendLinkViewComponentTests
    {
        private MockRepository _mockRepository;

        private Mock<ILogger<FriendLinkViewComponent>> _mockLogger;
        private Mock<IMediator> _mockMediator;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);

            _mockLogger = _mockRepository.Create<ILogger<FriendLinkViewComponent>>();
            _mockMediator = _mockRepository.Create<IMediator>();
        }

        private FriendLinkViewComponent CreateComponent()
        {
            return new(
                _mockLogger.Object,
                _mockMediator.Object);
        }

        [Test]
        public async Task InvokeAsync_Exception()
        {
            _mockMediator.Setup(p => p.Send(It.IsAny<GetAllLinksQuery>(), default)).Throws(new(FakeData.ShortString2));

            var component = CreateComponent();
            var result = await component.InvokeAsync();

            Assert.IsInstanceOf<ContentViewComponentResult>(result);
        }

        [Test]
        public async Task InvokeAsync_View()
        {
            IReadOnlyList<Link> links = new List<Link>();

            _mockMediator.Setup(p => p.Send(It.IsAny<GetAllLinksQuery>(), default)).Returns(Task.FromResult(links));

            var component = CreateComponent();
            var result = await component.InvokeAsync();

            Assert.IsInstanceOf<ViewViewComponentResult>(result);
        }
    }
}
