using System.Collections.Generic;
using System.Threading.Tasks;
using Moonglade.Configuration;
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
        private Mock<IBlogConfig> _mockBlogConfig;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);

            _mockFriendLinkService = _mockRepository.Create<IFriendLinkService>();
            _mockBlogConfig = _mockRepository.Create<IBlogConfig>();
        }

        private FriendLinkModel CreateFriendLinkModel()
        {
            return new(
                _mockFriendLinkService.Object,
                _mockBlogConfig.Object);
        }

        [Test]
        public async Task OnGet_StateUnderTest_ExpectedBehavior()
        {
            IReadOnlyList<Link> links = new List<Link>();
            _mockFriendLinkService.Setup(p => p.GetAllAsync()).Returns(Task.FromResult(links));
            _mockBlogConfig.Setup(p => p.FriendLinksSettings).Returns(new FriendLinksSettings());

            var friendLinkModel = CreateFriendLinkModel();
            await friendLinkModel.OnGet();

            Assert.IsNotNull(friendLinkModel.FriendLinks);
        }
    }
}
