using Edi.Captcha;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Auth;
using Moonglade.Web.Pages;
using Moq;
using NUnit.Framework;
using System.Linq.Expressions;

namespace Moonglade.Web.Tests.Pages;

[TestFixture]

public class SignInModelTests
{
    private MockRepository _mockRepository;

    private Mock<IOptions<AuthenticationSettings>> _mockOptions;
    private Mock<IMediator> _mockMediator;
    private Mock<ILogger<SignInModel>> _mockLogger;
    private Mock<ISessionBasedCaptcha> _mockSessionBasedCaptcha;

    [SetUp]
    public void SetUp()
    {
        _mockRepository = new(MockBehavior.Default);

        _mockOptions = _mockRepository.Create<IOptions<AuthenticationSettings>>();
        _mockMediator = _mockRepository.Create<IMediator>();
        _mockLogger = _mockRepository.Create<ILogger<SignInModel>>();
        _mockSessionBasedCaptcha = _mockRepository.Create<ISessionBasedCaptcha>();
    }

    private SignInModel CreateSignInModel()
    {
        var httpContext = new DefaultHttpContext();
        var modelState = new ModelStateDictionary();
        var actionContext = new ActionContext(httpContext, new(), new PageActionDescriptor(), modelState);
        var modelMetadataProvider = new EmptyModelMetadataProvider();
        var viewData = new ViewDataDictionary(modelMetadataProvider, modelState);
        var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        var pageContext = new PageContext(actionContext)
        {
            ViewData = viewData
        };

        httpContext.Session = new MockHttpSession();

        var model = new SignInModel(
            _mockOptions.Object,
            _mockMediator.Object,
            _mockLogger.Object,
            _mockSessionBasedCaptcha.Object)
        {
            PageContext = pageContext,
            TempData = tempData
        };

        return model;
    }

    [Test]
    public async Task OnGetAsync_AAD()
    {
        _mockOptions.Setup(p => p.Value).Returns(new AuthenticationSettings
        {
            Provider = AuthenticationProvider.AzureAD
        });

        var mockUrlHelper = new Mock<IUrlHelper>(MockBehavior.Strict);
        Expression<Func<IUrlHelper, string>> urlSetup
            = url => url.Action(It.Is<UrlActionContext>(uac => uac.Action == "Index"));
        mockUrlHelper.Setup(urlSetup).Returns("a/mock/url/for/testing").Verifiable();

        // Arrange
        var signInModel = CreateSignInModel();
        signInModel.Url = mockUrlHelper.Object;

        // Act
        var result = await signInModel.OnGetAsync();
        Assert.IsInstanceOf<ChallengeResult>(result);
    }

    //[Test]
    //public async Task SignIn_Local()
    //{
    //    _mockOptions.Setup(p => p.Value).Returns(new AuthenticationSettings
    //    {
    //        Provider = AuthenticationProvider.Local
    //    });

    //    var signInModel = CreateSignInModel();
    //    var result = await signInModel.OnGetAsync();
    //    Assert.IsInstanceOf<PageResult>(result);
    //}

    [Test]
    public async Task OnPostAsync_Exception()
    {
        _mockSessionBasedCaptcha.Setup(p => p.Validate(It.IsAny<string>(), It.IsAny<ISession>(), true, true)).Returns(true);

        _mockMediator.Setup(p => p.Send(It.IsAny<ValidateLoginCommand>(), default))
            .Throws(new(FakeData.ShortString2));

        // Arrange
        var signInModel = CreateSignInModel();
        signInModel.Username = "work";
        signInModel.Password = FakeData.ShortString2;

        // Act
        var result = await signInModel.OnPostAsync();

        Assert.IsInstanceOf<PageResult>(result);

        var modelState = signInModel.ViewData.ModelState;
        Assert.IsFalse(modelState.IsValid);
    }

    [Test]
    public async Task OnPostAsync_BadModelState()
    {
        _mockSessionBasedCaptcha.Setup(p => p.Validate(It.IsAny<string>(), It.IsAny<ISession>(), true, true)).Returns(true);

        var signInModel = CreateSignInModel();
        signInModel.Username = "";
        signInModel.Password = FakeData.ShortString2;

        signInModel.ModelState.AddModelError("", FakeData.ShortString2);
        var result = await signInModel.OnPostAsync();

        Assert.IsInstanceOf<PageResult>(result);

        var modelState = signInModel.ViewData.ModelState;
        Assert.IsFalse(modelState.IsValid);
    }


    [Test]
    public async Task OnPostAsync_InvalidCaptcha()
    {
        _mockSessionBasedCaptcha.Setup(p => p.Validate(It.IsAny<string>(), It.IsAny<ISession>(), true, true))
            .Returns(false);

        var signInModel = CreateSignInModel();
        signInModel.Username = FakeData.ShortString1;
        signInModel.Password = FakeData.ShortString2;

        var result = await signInModel.OnPostAsync();

        Assert.IsInstanceOf<PageResult>(result);

        var modelState = signInModel.ViewData.ModelState;
        Assert.IsFalse(modelState.IsValid);
    }

    [Test]
    public async Task OnPostAsync_InvalidCredential()
    {
        _mockSessionBasedCaptcha.Setup(p => p.Validate(It.IsAny<string>(), It.IsAny<ISession>(), true, true)).Returns(true);

        _mockMediator.Setup(p => p.Send(It.IsAny<ValidateLoginCommand>(), default)).Returns(Task.FromResult(Guid.Empty));

        var signInModel = CreateSignInModel();
        signInModel.Username = FakeData.ShortString1;
        signInModel.Password = FakeData.ShortString2;

        var result = await signInModel.OnPostAsync();

        Assert.IsInstanceOf<PageResult>(result);

        var modelState = signInModel.ViewData.ModelState;
        Assert.IsFalse(modelState.IsValid);
    }

    //    [Test]
    //    public async Task OnPostAsync_ValidCredential()
    //    {
    //        _mockSessionBasedCaptcha.Setup(p => p.Validate(It.IsAny<string>(), It.IsAny<ISession>(), true, true)).Returns(true);

    //        _mockLocalAccountService.Setup(p => p.ValidateAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult(FakeData.Uid1));

    //        var signInModel = CreateSignInModel();
    //        signInModel.Username = FakeData.ShortString1;
    //        signInModel.Password = FakeData.ShortString2;

    //        var result = await signInModel.OnPostAsync();

    //        Assert.IsInstanceOf<RedirectToPageResult>(result);
    //        var modelState = signInModel.ViewData.ModelState;
    //        Assert.IsTrue(modelState.IsValid);
    //        _mockLocalAccountService.Verify(p => p.LogSuccessLoginAsync(FakeData.Uid1, It.IsAny<string>()));
    //    }
}