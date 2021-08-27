using MediatR;
using Moonglade.FriendLink;
using Moonglade.Web.Pages.Admin;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Moonglade.Web.Tests.Pages.Admin
{
    [TestFixture]

    public class FriendLinkModelTests
    {
        private MockRepository _mockRepository;

        private Mock<IMediator> _mockMediator;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);
            _mockMediator = _mockRepository.Create<IMediator>();
        }

        private FriendLinkModel CreateFriendLinkModel()
        {
            return new(_mockMediator.Object);
        }

        [Test]
        public async Task OnGet_StateUnderTest_ExpectedBehavior()
        {
            IReadOnlyList<Link> links = new List<Link>();
            _mockMediator.Setup(p => p.Send(It.IsAny<GetAllLinksQuery>(), default)).Returns(Task.FromResult(links));

            var friendLinkModel = CreateFriendLinkModel();
            await friendLinkModel.OnGet();

            Assert.IsNotNull(friendLinkModel.FriendLinks);
        }
    }
}
