using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Moonglade.Auditing;
using Moonglade.Core;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moq;
using NUnit.Framework;

namespace Moonglade.Tests.Core
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class FriendLinkServiceTests
    {
        private Mock<IBlogAudit> _auditMock;

        [SetUp]
        public void Setup()
        {
            _auditMock = new();
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

            var friendlinkRepositoryMock = new Mock<IRepository<FriendLinkEntity>>();
            friendlinkRepositoryMock.Setup(p => p.AddAsync(It.IsAny<FriendLinkEntity>())).Returns(tcs.Task);

            var svc = new FriendLinkService(friendlinkRepositoryMock.Object, _auditMock.Object);

            await svc.AddAsync("Choice of 955", "https://dot.net");
            Assert.Pass();
        }
    }
}
