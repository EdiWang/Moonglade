using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Core;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Model.Settings;
using Moq;
using NUnit.Framework;

namespace Moonglade.Tests.Core
{
    [TestFixture]
    public class FriendLinkServiceTests
    {
        private Mock<ILogger<FriendLinkService>> _loggerMock;
        private Mock<IOptions<AppSettings>> _appSettingsMock;

        [SetUp]
        public void Setup()
        {
            _loggerMock = new Mock<ILogger<FriendLinkService>>();
            _appSettingsMock = new Mock<IOptions<AppSettings>>();
        }

        [TestCase("", "", ExpectedResult = false)]
        [TestCase("996", "", ExpectedResult = false)]
        [TestCase("", "icu", ExpectedResult = false)]
        [TestCase("dotnet", "955", ExpectedResult = false)]
        public async Task<bool> TestAddFriendLinkAsyncInvalidParameter(string title, string linkUrl)
        {
            var friendlinkRepositoryMock = new Mock<IRepository<FriendLinkEntity>>();
            var svc = new FriendLinkService(_loggerMock.Object, _appSettingsMock.Object, friendlinkRepositoryMock.Object);

            var fdLinkResponse = await svc.AddFriendLinkAsync(title, linkUrl);
            return fdLinkResponse.IsSuccess;
        }

        [Test]
        public async Task TestAddFriendLinkAsyncValid()
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

            var svc = new FriendLinkService(_loggerMock.Object, _appSettingsMock.Object, friendlinkRepositoryMock.Object);

            var fdLinkResponse = await svc.AddFriendLinkAsync("Choice of 955", "https://dot.net");
            Assert.IsTrue(fdLinkResponse.IsSuccess);
        }

        [TestCase("", "", ExpectedResult = false)]
        [TestCase("Java", "", ExpectedResult = false)]
        [TestCase("", "ICU", ExpectedResult = false)]
        [TestCase("dotnet", "955", ExpectedResult = false)]
        public async Task<bool> UpdateFriendLinkAsyncInvalidParameter(string title, string linkUrl)
        {
            var friendlinkRepositoryMock = new Mock<IRepository<FriendLinkEntity>>();
            var svc = new FriendLinkService(_loggerMock.Object, _appSettingsMock.Object, friendlinkRepositoryMock.Object);

            var fdLinkResponse = await svc.UpdateFriendLinkAsync(Guid.NewGuid(), title, linkUrl);
            return fdLinkResponse.IsSuccess;
        }
    }
}
