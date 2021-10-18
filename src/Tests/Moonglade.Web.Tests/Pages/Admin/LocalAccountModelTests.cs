using MediatR;
using Moonglade.Auth;
using Moonglade.Web.Pages.Admin;
using Moq;
using NUnit.Framework;

namespace Moonglade.Web.Tests.Pages.Admin;

[TestFixture]

public class LocalAccountModelTests
{
    private MockRepository _mockRepository;
    private Mock<IMediator> _mockMediator;

    [SetUp]
    public void SetUp()
    {
        _mockRepository = new(MockBehavior.Default);
        _mockMediator = _mockRepository.Create<IMediator>();
    }

    private LocalAccountModel CreateLocalAccountModel()
    {
        return new(_mockMediator.Object);
    }

    [Test]
    public async Task OnGet_StateUnderTest_ExpectedBehavior()
    {
        IReadOnlyList<Account> accounts = new List<Account>();
        _mockMediator.Setup(p => p.Send(It.IsAny<GetAccountsQuery>(), default)).Returns(Task.FromResult(accounts));

        var localAccountModel = CreateLocalAccountModel();
        await localAccountModel.OnGet();

        Assert.IsNotNull(localAccountModel.Accounts);
    }
}