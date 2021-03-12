using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Moonglade.Auditing;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moq;
using NUnit.Framework;

namespace Moonglade.Auth.Tests
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class LocalAccountServiceTests
    {
        private MockRepository _mockRepository;

        private Mock<IRepository<LocalAccountEntity>> _mockLocalAccountRepository;
        private Mock<IBlogAudit> _mockBlogAudit;

        private static readonly Guid Uid = Guid.Parse("76169567-6ff3-42c0-b163-a883ff2ac4fb");

        private readonly LocalAccountEntity _accountEntity = new()
        {
            Id = Uid,
            CreateTimeUtc = new DateTime(996, 9, 6),
            Username = "icuworker",
            LastLoginIp = "7.35.251.110",
            LastLoginTimeUtc = new DateTime(997, 3, 5)
        };

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);

            _mockLocalAccountRepository = _mockRepository.Create<IRepository<LocalAccountEntity>>();
            _mockBlogAudit = _mockRepository.Create<IBlogAudit>();

            _accountEntity.PasswordHash = "JAvlGPq9JyTdtvBO6x2llnRI1+gxwIyPqCKAn3THIKk=";
        }

        private LocalAccountService CreateService()
        {
            return new(
                _mockLocalAccountRepository.Object,
                _mockBlogAudit.Object);
        }

        [TestCase("", ExpectedResult = "")]
        [TestCase(null, ExpectedResult = "")]
        [TestCase(" ", ExpectedResult = "")]
        [TestCase("admin123", ExpectedResult = "JAvlGPq9JyTdtvBO6x2llnRI1+gxwIyPqCKAn3THIKk=")]
        public string HashPassword(string plainMessage)
        {
            return LocalAccountService.HashPassword(plainMessage);
        }

        [Test]
        public async Task GetAsync_OK()
        {
            _mockLocalAccountRepository.Setup(p => p.GetAsync(It.IsAny<Guid>()))
                .Returns(ValueTask.FromResult(_accountEntity));

            var svc = CreateService();
            var account = await svc.GetAsync(Uid);

            Assert.IsNotNull(account);
            Assert.AreEqual(_accountEntity.Username, account.Username);
            _mockLocalAccountRepository.Verify(p => p.GetAsync(Uid));
        }

        [Test]
        public async Task GetAllAsync_OK()
        {
            var svc = CreateService();
            var account = await svc.GetAllAsync();

            _mockLocalAccountRepository.Verify(p => p.SelectAsync(It.IsAny<Expression<Func<LocalAccountEntity, Account>>>(), true));
        }

        [TestCase(null, null)]
        [TestCase(null, "")]
        [TestCase(null, " ")]
        [TestCase("", null)]
        [TestCase(" ", null)]
        public void ValidateAsync_EmptyUsernameOrPassword(string username, string inputPassword)
        {
            var svc = CreateService();
            Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                await svc.ValidateAsync(username, inputPassword);
            });
        }

        [Test]
        public async Task ValidateAsync_AccountNull()
        {
            _mockLocalAccountRepository.Setup(p => p.GetAsync(It.IsAny<Expression<Func<LocalAccountEntity, bool>>>()))
                .Returns(Task.FromResult((LocalAccountEntity)null));

            var svc = CreateService();
            var result = await svc.ValidateAsync("work", "996");

            Assert.AreEqual(Guid.Empty, result);
        }

        [Test]
        public async Task ValidateAsync_InvalidHash()
        {
            _accountEntity.PasswordHash = "996";

            _mockLocalAccountRepository.Setup(p => p.GetAsync(It.IsAny<Expression<Func<LocalAccountEntity, bool>>>()))
                .Returns(Task.FromResult(_accountEntity));

            var svc = CreateService();
            var result = await svc.ValidateAsync("work", "996");

            Assert.AreEqual(Guid.Empty, result);
        }

        [Test]
        public async Task ValidateAsync_ValidHash()
        {
            _mockLocalAccountRepository.Setup(p => p.GetAsync(It.IsAny<Expression<Func<LocalAccountEntity, bool>>>()))
                .Returns(Task.FromResult(_accountEntity));

            var svc = CreateService();
            var result = await svc.ValidateAsync("work996", "admin123");

            Assert.AreEqual(Uid, result);
        }

        [Test]
        public async Task LogSuccessLoginAsync_EntityNull()
        {
            _mockLocalAccountRepository.Setup(p => p.GetAsync(It.IsAny<Guid>()))
                .Returns(ValueTask.FromResult((LocalAccountEntity)null));

            var svc = CreateService();
            await svc.LogSuccessLoginAsync(Guid.Empty, "1.1.1.1");

            _mockLocalAccountRepository.Verify(p => p.UpdateAsync(It.IsAny<LocalAccountEntity>()), Times.Never);
        }

        [Test]
        public async Task LogSuccessLoginAsync_OK()
        {
            _mockLocalAccountRepository.Setup(p => p.GetAsync(It.IsAny<Guid>()))
                .Returns(ValueTask.FromResult(_accountEntity));

            var svc = CreateService();
            await svc.LogSuccessLoginAsync(Uid, "1.1.1.1");

            _mockLocalAccountRepository.Verify(p => p.UpdateAsync(It.IsAny<LocalAccountEntity>()), Times.Once);
        }

        [Test]
        public void Exist_ToHaveBeenCalled()
        {
            var svc = CreateService();
            svc.Exist("work996");

            _mockLocalAccountRepository.Verify(p => p.Any(It.IsAny<Expression<Func<LocalAccountEntity, bool>>>()));
        }

        [TestCase(null, null)]
        [TestCase(null, "")]
        [TestCase(null, " ")]
        [TestCase("", null)]
        [TestCase(" ", null)]
        public void CreateAsync_EmptyUsernameOrPassword(string username, string clearPassword)
        {
            var svc = CreateService();
            Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                await svc.CreateAsync(username, clearPassword);
            });
        }

        [Test]
        public async Task CreateAsync_OK()
        {
            var svc = CreateService();
            var result = await svc.CreateAsync("work996", "&Get1n2icu");

            Assert.IsTrue(result != Guid.Empty);

            _mockLocalAccountRepository.Verify(p => p.AddAsync(It.IsAny<LocalAccountEntity>()));
            _mockBlogAudit.Verify(p => p.AddAuditEntry(EventType.Settings, AuditEventId.SettingsAccountCreated, It.IsAny<string>()));
        }
    }
}
