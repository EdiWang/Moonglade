using System.Collections.Generic;
using System.Threading.Tasks;
using Moonglade.FriendLink;
using Moonglade.Web.Pages.Admin;
using Moq;
using NUnit.Framework;

namespace Moonglade.Web.Tests.Pages.Admin
{
    [TestFixture]

    public class FriendLinkModelTests
    {
        private MockRepository _mockRepository;

        private Mock<IFriendLinkService> _mockFriendLinkService;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);
            _mockFriendLinkService = _mockRepository.Create<IFriendLinkService>();
        }

        private FriendLinkModel CreateFriendLinkModel()
        {
            return new(_mockFriendLinkService.Object);
        }

        [Test]
        public async Task OnGet_StateUnderTest_ExpectedBehavior()
        {
            IReadOnlyList<Link> links = new List<Link>();
            _mockFriendLinkService.Setup(p => p.GetAllAsync()).Returns(Task.FromResult(links));

            var friendLinkModel = CreateFriendLinkModel();
            await friendLinkModel.OnGet();

            Assert.IsNotNull(friendLinkModel.FriendLinks);
        }
    }
}
