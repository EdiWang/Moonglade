using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moq;
using NUnit.Framework;
using System.Linq.Expressions;

namespace Moonglade.Auth.Tests;

[TestFixture]
public class LocalAccountServiceTests
{
    private MockRepository _mockRepository;

    private Mock<IRepository<LocalAccountEntity>> _mockLocalAccountRepository;

    private static readonly Guid Uid = Guid.Parse("76169567-6ff3-42c0-b163-a883ff2ac4fb");

    private readonly LocalAccountEntity _accountEntity = new()
    {
        Id = Uid,
        CreateTimeUtc = new(996, 9, 6),
        Username = "icuworker",
        LastLoginIp = "7.35.251.110",
        LastLoginTimeUtc = new DateTime(997, 3, 5)
    };

    [SetUp]
    public void SetUp()
    {
        _mockRepository = new(MockBehavior.Default);

        _mockLocalAccountRepository = _mockRepository.Create<IRepository<LocalAccountEntity>>();

        _accountEntity.PasswordHash = "JAvlGPq9JyTdtvBO6x2llnRI1+gxwIyPqCKAn3THIKk=";
    }

    [Test]
    public async Task GetAsync_OK()
    {
        _mockLocalAccountRepository.Setup(p => p.GetAsync(It.IsAny<Guid>()))
            .Returns(ValueTask.FromResult(_accountEntity));

        var handler = new GetAccountQueryHandler(_mockLocalAccountRepository.Object);
        var account = await handler.Handle(new(Uid), default);

        Assert.IsNotNull(account);
        Assert.AreEqual(_accountEntity.Username, account.Username);
        _mockLocalAccountRepository.Verify(p => p.GetAsync(Uid));
    }

    [Test]
    public async Task GetAllAsync_OK()
    {
        var handler = new GetAccountsQueryHandler(_mockLocalAccountRepository.Object);
        var account = await handler.Handle(new(), default);

        _mockLocalAccountRepository.Verify(p => p.SelectAsync(It.IsAny<Expression<Func<LocalAccountEntity, Account>>>()));
    }

    [TestCase(null, null)]
    [TestCase(null, "")]
    [TestCase(null, " ")]
    [TestCase("", null)]
    [TestCase(" ", null)]
    public void ValidateAsync_EmptyUsernameOrPassword(string username, string inputPassword)
    {
        var handler = new ValidateLoginCommandHandler(_mockLocalAccountRepository.Object);
        Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            await handler.Handle(new(username, inputPassword), default);
        });
    }

    [Test]
    public async Task ValidateAsync_AccountNull()
    {
        _mockLocalAccountRepository.Setup(p => p.GetAsync(It.IsAny<Expression<Func<LocalAccountEntity, bool>>>()))
            .Returns(Task.FromResult((LocalAccountEntity)null));

        var handler = new ValidateLoginCommandHandler(_mockLocalAccountRepository.Object);
        var result = await handler.Handle(new("work", "996"), default);

        Assert.AreEqual(Guid.Empty, result);
    }

    [Test]
    public async Task ValidateAsync_InvalidHash()
    {
        _accountEntity.PasswordHash = "996";

        _mockLocalAccountRepository.Setup(p => p.GetAsync(It.IsAny<Expression<Func<LocalAccountEntity, bool>>>()))
            .Returns(Task.FromResult(_accountEntity));

        var handler = new ValidateLoginCommandHandler(_mockLocalAccountRepository.Object);
        var result = await handler.Handle(new("work", "996"), default);

        Assert.AreEqual(Guid.Empty, result);
    }

    [Test]
    public async Task ValidateAsync_ValidHash()
    {
        _mockLocalAccountRepository.Setup(p => p.GetAsync(It.IsAny<Expression<Func<LocalAccountEntity, bool>>>()))
            .Returns(Task.FromResult(_accountEntity));

        var handler = new ValidateLoginCommandHandler(_mockLocalAccountRepository.Object);
        var result = await handler.Handle(new("work996", "admin123"), default);

        Assert.AreEqual(Uid, result);
    }

    [Test]
    public async Task LogSuccessLoginAsync_EntityNull()
    {
        _mockLocalAccountRepository.Setup(p => p.GetAsync(It.IsAny<Guid>()))
            .Returns(ValueTask.FromResult((LocalAccountEntity)null));

        var handler = new LogSuccessLoginCommandHandler(_mockLocalAccountRepository.Object);
        await handler.Handle(new(Guid.Empty, "1.1.1.1"), default);

        _mockLocalAccountRepository.Verify(p => p.UpdateAsync(It.IsAny<LocalAccountEntity>()), Times.Never);
    }

    [Test]
    public async Task LogSuccessLoginAsync_OK()
    {
        _mockLocalAccountRepository.Setup(p => p.GetAsync(It.IsAny<Guid>()))
            .Returns(ValueTask.FromResult(_accountEntity));

        var handler = new LogSuccessLoginCommandHandler(_mockLocalAccountRepository.Object);
        await handler.Handle(new(Uid, "1.1.1.1"), default);

        _mockLocalAccountRepository.Verify(p => p.UpdateAsync(It.IsAny<LocalAccountEntity>()), Times.Once);
    }

    [Test]
    public void Exist_ToHaveBeenCalled()
    {
        var handler = new AccountExistsQueryHandler(_mockLocalAccountRepository.Object);
        handler.Handle(new("work996"), default);

        _mockLocalAccountRepository.Verify(p => p.Any(It.IsAny<Expression<Func<LocalAccountEntity, bool>>>()));
    }

    [TestCase(null, null)]
    [TestCase(null, "")]
    [TestCase(null, " ")]
    [TestCase("", null)]
    [TestCase(" ", null)]
    public void CreateAsync_EmptyUsernameOrPassword(string username, string clearPassword)
    {
        var handler = new CreateAccountCommandHandler(_mockLocalAccountRepository.Object);
        Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            await handler.Handle(new(new()
            {
                Username = username,
                Password = clearPassword
            }), default);
        });
    }

    [Test]
    public async Task CreateAsync_OK()
    {
        var handler = new CreateAccountCommandHandler(_mockLocalAccountRepository.Object);
        var result = await handler.Handle(new(new()
        {
            Username = "work996",
            Password = "&Get1n2icu"
        }), default);

        Assert.IsTrue(result != Guid.Empty);

        _mockLocalAccountRepository.Verify(p => p.AddAsync(It.IsAny<LocalAccountEntity>()));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    public void UpdatePasswordAsync_EmptyPassword(string clearPassword)
    {
        var handler = new UpdatePasswordCommandHandler(_mockLocalAccountRepository.Object);
        Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            await handler.Handle(new(Uid, clearPassword), default);
        });
    }

    [Test]
    public void UpdatePasswordAsync_AccountNull()
    {
        _mockLocalAccountRepository.Setup(p => p.GetAsync(It.IsAny<Guid>()))
            .Returns(ValueTask.FromResult((LocalAccountEntity)null));

        var handler = new UpdatePasswordCommandHandler(_mockLocalAccountRepository.Object);
        Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await handler.Handle(new(Uid, "Work996"), default);
        });
    }

    [Test]
    public async Task UpdatePasswordAsync_OK()
    {
        _mockLocalAccountRepository.Setup(p => p.GetAsync(It.IsAny<Guid>()))
            .Returns(ValueTask.FromResult(_accountEntity));

        var handler = new UpdatePasswordCommandHandler(_mockLocalAccountRepository.Object);
        await handler.Handle(new(Uid, "Work996andGetintoICU"), default);

        _mockLocalAccountRepository.Verify(p => p.UpdateAsync(It.IsAny<LocalAccountEntity>()));
    }

    [Test]
    public void DeleteAsync_AccountNull()
    {
        _mockLocalAccountRepository.Setup(p => p.GetAsync(It.IsAny<Guid>()))
            .Returns(ValueTask.FromResult((LocalAccountEntity)null));

        var handler = new DeleteAccountQueryHandler(_mockLocalAccountRepository.Object);
        Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await handler.Handle(new(Uid), default);
        });
    }

    [Test]
    public async Task DeleteAsync_OK()
    {
        _mockLocalAccountRepository.Setup(p => p.GetAsync(It.IsAny<Guid>()))
            .Returns(ValueTask.FromResult(_accountEntity));

        var handler = new DeleteAccountQueryHandler(_mockLocalAccountRepository.Object);
        await handler.Handle(new(Uid), default);

        _mockLocalAccountRepository.Verify(p => p.DeleteAsync(It.IsAny<Guid>()));
    }
}