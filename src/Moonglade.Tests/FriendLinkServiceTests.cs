using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Moonglade.Auditing;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.FriendLink;
using Moq;
using NUnit.Framework;

namespace Moonglade.Tests
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class FriendLinkServiceTests
    {
        private MockRepository _mockRepository;
        private Mock<IBlogAudit> _mockBlogAudit;
        private Mock<IRepository<FriendLinkEntity>> _mockFriendlinkRepo;

        [SetUp]
        public void Setup()
        {
            _mockRepository = new(MockBehavior.Default);
            _mockBlogAudit = _mockRepository.Create<IBlogAudit>();
            _mockFriendlinkRepo = _mockRepository.Create<IRepository<FriendLinkEntity>>();
        }

        private FriendLinkService CreateService()
        {
            return new(_mockFriendlinkRepo.Object, _mockBlogAudit.Object);
        }

        [Test]
        public async Task AddFriendLinkAsync_Valid()
        {
            var uid = Guid.NewGuid();
            var friendLinkEntity = new FriendLinkEntity
            {
                Id = uid,
                LinkUrl = "https://dot.net",
                Title = "Choice of 955"
            };
            var tcs = new TaskCompletionSource<FriendLinkEntity>();
            tcs.SetResult(friendLinkEntity);

            _mockFriendlinkRepo.Setup(p => p.AddAsync(It.IsAny<FriendLinkEntity>())).Returns(tcs.Task);

            var svc = CreateService();
            await svc.AddAsync("Choice of 955", "https://dot.net");
            Assert.Pass();
        }

        [Test]
        public async Task DeleteAsync_OK()
        {
            var svc = CreateService();
            await svc.DeleteAsync(Guid.Empty);

            _mockBlogAudit.Verify();
            Assert.Pass();
        }
    }
}
