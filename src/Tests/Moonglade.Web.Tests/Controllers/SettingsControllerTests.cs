using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moonglade.Configuration;
using Moonglade.Core;
using Moonglade.Web.Controllers;
using Moq;
using NUnit.Framework;
using System.ComponentModel.DataAnnotations;

namespace Moonglade.Web.Tests.Controllers;

[TestFixture]
public class SettingsControllerTests
{
    private MockRepository _mockRepository;

    private Mock<IBlogConfig> _mockBlogConfig;
    private Mock<ILogger<SettingsController>> _mockLogger;
    private Mock<IMediator> _mockMediator;

    [SetUp]
    public void SetUp()
    {
        _mockRepository = new(MockBehavior.Default);

        _mockBlogConfig = _mockRepository.Create<IBlogConfig>();
        _mockLogger = _mockRepository.Create<ILogger<SettingsController>>();
        _mockMediator = _mockRepository.Create<IMediator>();
    }

    private SettingsController CreateSettingsController()
    {
        return new(
            _mockBlogConfig.Object,
            _mockLogger.Object,
            _mockMediator.Object);
    }

    [Test]
    public async Task CheckNewRelease_HasNewVersion_NotPreRelease()
    {
        var mockReleaseCheckerClient = _mockRepository.Create<IReleaseCheckerClient>();
        mockReleaseCheckerClient.Setup(p => p.CheckNewReleaseAsync()).Returns(Task.FromResult(new ReleaseInfo
        {
            TagName = "v996.007.251.404",
            PreRelease = false,
            CreatedAt = DateTime.MaxValue,
            HtmlUrl = "https://996.icu",
            Name = "The 996 Involution Release"
        }));
        var ctl = CreateSettingsController();

        var result = await ctl.CheckNewRelease(mockReleaseCheckerClient.Object);
        Assert.IsInstanceOf<OkObjectResult>(result);

        var model = ((OkObjectResult)result).Value as SettingsController.CheckNewReleaseResult;
        Assert.IsTrue(model.HasNewRelease);
        Assert.IsNotNull(model.LatestReleaseInfo);
    }

    [Test]
    public async Task CheckNewRelease_HasNewVersion_PreRelease()
    {
        var mockReleaseCheckerClient = _mockRepository.Create<IReleaseCheckerClient>();
        mockReleaseCheckerClient.Setup(p => p.CheckNewReleaseAsync()).Returns(Task.FromResult(new ReleaseInfo
        {
            TagName = "v996.007.251.404",
            PreRelease = true,
            CreatedAt = DateTime.MaxValue,
            HtmlUrl = "https://996.icu",
            Name = "The 996 Involution Release"
        }));
        var ctl = CreateSettingsController();

        var result = await ctl.CheckNewRelease(mockReleaseCheckerClient.Object);
        Assert.IsInstanceOf<OkObjectResult>(result);

        var model = ((OkObjectResult)result).Value as SettingsController.CheckNewReleaseResult;
        Assert.IsFalse(model.HasNewRelease);
        Assert.IsNotNull(model.LatestReleaseInfo);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    public void SetLanguage_EmptyCulture(string culture)
    {
        var ctl = CreateSettingsController();
        var result = ctl.SetLanguage(culture, null);

        Assert.IsInstanceOf<BadRequestResult>(result);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    [TestCase(" \"<script>bad();<script>")]
    public void SetLanguage_EmptyUrl(string url)
    {
        var ctl = CreateSettingsController();
        var result = ctl.SetLanguage("en-US", url);

        Assert.IsInstanceOf<LocalRedirectResult>(result);
        Assert.AreEqual("~/", (result as LocalRedirectResult).Url);
    }

    [Test]
    public void SetLanguage_Cookie()
    {
        var ctl = CreateSettingsController();
        var result = ctl.SetLanguage("en-US", "/996/icu");

        Assert.IsInstanceOf<LocalRedirectResult>(result);
    }


    [Test]
    public async Task General_Post()
    {
        _mockBlogConfig.Setup(p => p.GeneralSettings).Returns(new GeneralSettings());
        var model = new GeneralSettings
        {
            SideBarOption = SideBarOption.Right
        };

        Mock<ITimeZoneResolver> tZoneResolverMock = new();

        var settingsController = CreateSettingsController();
        var result = await settingsController.General(new(model), tZoneResolverMock.Object);

        Assert.IsInstanceOf<NoContentResult>(result);
        _mockBlogConfig.Verify(p => p.SaveAsync(It.IsAny<GeneralSettings>()));
    }

    [Test]
    public async Task Content_Post()
    {
        _mockBlogConfig.Setup(p => p.ContentSettings).Returns(new ContentSettings());
        ContentSettings model = new() { WordFilterMode = WordFilterMode.Block };

        var settingsController = CreateSettingsController();
        var result = await settingsController.Content(new(model));

        Assert.IsInstanceOf<NoContentResult>(result);
        _mockBlogConfig.Verify(p => p.SaveAsync(It.IsAny<ContentSettings>()));
    }

    [Test]
    public async Task Notification_Post()
    {
        _mockBlogConfig.Setup(p => p.NotificationSettings).Returns(new NotificationSettings());
        var settingsController = CreateSettingsController();
        NotificationSettings model = new();

        var result = await settingsController.Notification(new(model));

        Assert.IsInstanceOf<NoContentResult>(result);
        _mockBlogConfig.Verify(p => p.SaveAsync(It.IsAny<NotificationSettings>()));
    }

    [Test]
    public async Task SendTestEmail_Post()
    {
        var settingsController = CreateSettingsController();
        var result = await settingsController.TestEmail();
        Assert.IsInstanceOf<OkObjectResult>(result);
    }

    [Test]
    public async Task Subscription_Post()
    {
        _mockBlogConfig.Setup(p => p.FeedSettings).Returns(new FeedSettings());
        var settingsController = CreateSettingsController();
        FeedSettings model = new();

        var result = await settingsController.Subscription(new(model));

        Assert.IsInstanceOf<NoContentResult>(result);
        _mockBlogConfig.Verify(p => p.SaveAsync(It.IsAny<FeedSettings>()));
    }

    [Test]
    public async Task Image_Post()
    {
        _mockBlogConfig.Setup(p => p.ImageSettings).Returns(new ImageSettings());
        var settingsController = CreateSettingsController();
        ImageSettings model = new();

        var result = await settingsController.Image(new(model));

        Assert.IsInstanceOf<NoContentResult>(result);
        _mockBlogConfig.Verify(p => p.SaveAsync(It.IsAny<ImageSettings>()));
    }

    //[Test]
    //public async Task SetSiteIcon_StateUnderTest_ExpectedBehavior()
    //{
    //    // Arrange
    //    var settingsController = CreateSettingsController();
    //    string base64Img = null;

    //    // Act
    //    var result = await settingsController.SetSiteIcon(
    //        base64Img);

    //    // Assert
    //    Assert.Fail();
    //    _mockRepository.VerifyAll();
    //}

    [Test]
    public async Task Advanced_Post()
    {
        _mockBlogConfig.Setup(p => p.AdvancedSettings).Returns(new AdvancedSettings());
        var settingsController = CreateSettingsController();
        AdvancedSettings model = new();

        var result = await settingsController.Advanced(new(model));

        Assert.IsInstanceOf<NoContentResult>(result);
        _mockBlogConfig.Verify(p => p.SaveAsync(It.IsAny<AdvancedSettings>()));
    }

    [Test]
    public void Image_Post_EnableCDNRedirect_EmptyCDNEndpoint()
    {
        _mockBlogConfig.Setup(p => p.ImageSettings).Returns(new ImageSettings());
        ImageSettings model = new() { EnableCDNRedirect = true, CDNEndpoint = string.Empty };

        var validationContext = new ValidationContext(model);
        var results = model.Validate(validationContext);
        Assert.AreEqual(results.Count(), 2);
    }

    [Test]
    public void Image_Post_EnableCDNRedirect_InvalidCDNEndpoint()
    {
        _mockBlogConfig.Setup(p => p.ImageSettings).Returns(new ImageSettings());
        ImageSettings model = new() { EnableCDNRedirect = true, CDNEndpoint = "996.icu" };

        var validationContext = new ValidationContext(model);
        var results = model.Validate(validationContext);
        Assert.AreEqual(results.Count(), 1);
    }

    [Test]
    public async Task Image_Post_EnableCDNRedirect_ValidCDNEndpoint()
    {
        _mockBlogConfig.Setup(p => p.ImageSettings).Returns(new ImageSettings());
        var settingsController = CreateSettingsController();
        ImageSettings model = new() { EnableCDNRedirect = true, CDNEndpoint = "https://cdn.996.icu/fubao" };

        var result = await settingsController.Image(new(model));

        Assert.IsInstanceOf<NoContentResult>(result);
        _mockBlogConfig.Verify(p => p.SaveAsync(It.IsAny<ImageSettings>()));
    }

    [Test]
    public void Shutdown_Post()
    {
        var settingsController = CreateSettingsController();
        settingsController.ControllerContext = new()
        {
            HttpContext = new DefaultHttpContext()
        };

        Mock<IHostApplicationLifetime> applicationLifetimeMock = new();

        var result = settingsController.Shutdown(applicationLifetimeMock.Object);
        Assert.IsInstanceOf<AcceptedResult>(result);
    }

    //[Test]
    //public async Task Reset_StateUnderTest_ExpectedBehavior()
    //{
    //    // Arrange
    //    var settingsController = CreateSettingsController();
    //    IDbConnection dbConnection = null;
    //    IHostApplicationLifetime applicationLifetime = null;

    //    // Act
    //    var result = await settingsController.Reset(
    //        dbConnection,
    //        applicationLifetime);

    //    // Assert
    //    Assert.Fail();
    //    _mockRepository.VerifyAll();
    //}

    [Test]
    public async Task CustomStyleSheet_Post_Enabled_EmptyCSS()
    {
        _mockBlogConfig.Setup(p => p.CustomStyleSheetSettings).Returns(new CustomStyleSheetSettings());

        var settingsController = CreateSettingsController();
        CustomStyleSheetSettings model = new()
        {
            EnableCustomCss = true,
            CssCode = string.Empty
        };

        var result = await settingsController.CustomStyleSheet(new(model));

        Assert.IsInstanceOf<BadRequestObjectResult>(result);
        _mockBlogConfig.Verify(p => p.SaveAsync(It.IsAny<CustomStyleSheetSettings>()), Times.Never);
    }

    [Test]
    public async Task CustomStyleSheet_Post_Enabled_BadCSS()
    {
        _mockBlogConfig.Setup(p => p.CustomStyleSheetSettings).Returns(new CustomStyleSheetSettings());

        var settingsController = CreateSettingsController();
        CustomStyleSheetSettings model = new()
        {
            EnableCustomCss = true,
            CssCode = ".996-{icu}"
        };

        var result = await settingsController.CustomStyleSheet(new(model));

        Assert.IsInstanceOf<BadRequestObjectResult>(result);
        _mockBlogConfig.Verify(p => p.SaveAsync(It.IsAny<CustomStyleSheetSettings>()), Times.Never);
    }

    [Test]
    public async Task CustomStyleSheet_Post_OK()
    {
        _mockBlogConfig.Setup(p => p.CustomStyleSheetSettings).Returns(new CustomStyleSheetSettings());

        var settingsController = CreateSettingsController();
        CustomStyleSheetSettings model = new()
        {
            EnableCustomCss = true,
            CssCode = ".icu { color: #996; }"
        };

        var result = await settingsController.CustomStyleSheet(new(model));

        Assert.IsInstanceOf<NoContentResult>(result);
        _mockBlogConfig.Verify(p => p.SaveAsync(It.IsAny<CustomStyleSheetSettings>()));
    }

    [Test]
    public void GeneratePassword_OK()
    {
        var settingsController = CreateSettingsController();
        var result = settingsController.GeneratePassword();

        Assert.IsInstanceOf<OkObjectResult>(result);
    }

    //[Test]
    //public void ClearDataCache_StateUnderTest_ExpectedBehavior()
    //{
    //    // Arrange
    //    var settingsController = CreateSettingsController();
    //    string[] cachedObjectValues = null;
    //    IBlogCache cache = null;

    //    // Act
    //    var result = settingsController.ClearDataCache(
    //        cachedObjectValues,
    //        cache);

    //    // Assert
    //    Assert.Fail();
    //    _mockRepository.VerifyAll();
    //}
}