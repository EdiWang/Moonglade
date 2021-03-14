using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moonglade.Auditing;
using Moonglade.Configuration;
using Moonglade.Configuration.Abstraction;
using Moonglade.FriendLink;
using Moonglade.Notification.Client;
using Moonglade.Web.Controllers;
using Moonglade.Web.Models.Settings;
using Moq;
using NUnit.Framework;

namespace Moonglade.Web.Tests.Controllers
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class SettingsControllerTests
    {
        private MockRepository _mockRepository;

        private Mock<IFriendLinkService> _mockFriendLinkService;
        private Mock<IBlogConfig> _mockBlogConfig;
        private Mock<IBlogAudit> _mockBlogAudit;
        private Mock<ILogger<SettingsController>> _mockLogger;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);

            _mockFriendLinkService = _mockRepository.Create<IFriendLinkService>();
            _mockBlogConfig = _mockRepository.Create<IBlogConfig>();
            _mockBlogAudit = _mockRepository.Create<IBlogAudit>();
            _mockLogger = _mockRepository.Create<ILogger<SettingsController>>();
        }

        private SettingsController CreateSettingsController()
        {
            return new(
                _mockFriendLinkService.Object,
                _mockBlogConfig.Object,
                _mockBlogAudit.Object,
                _mockLogger.Object);
        }

        [Test]
        public void General_Get()
        {
            _mockBlogConfig.Setup(p => p.GeneralSettings).Returns(new GeneralSettings());
            var tZoneResolverMock = new Mock<ITZoneResolver>();

            var settingsController = CreateSettingsController();
            var result = settingsController.General(tZoneResolverMock.Object);

            Assert.IsInstanceOf<ViewResult>(result);
        }

        [Test]
        public async Task General_Post()
        {
            _mockBlogConfig.Setup(p => p.GeneralSettings).Returns(new GeneralSettings());
            var model = new GeneralSettingsViewModel
            {
                SideBarOption = "Right"
            };

            Mock<ITZoneResolver> tZoneResolverMock = new();

            var settingsController = CreateSettingsController();
            var result = await settingsController.General(model, tZoneResolverMock.Object);

            Assert.IsInstanceOf<OkResult>(result);
            _mockBlogConfig.Verify(p => p.SaveAsync(It.IsAny<GeneralSettings>()));
            _mockBlogAudit.Verify(p => p.AddAuditEntry(EventType.Settings, AuditEventId.SettingsSavedGeneral, It.IsAny<string>()));
        }

        [Test]
        public void Content_Get()
        {
            _mockBlogConfig.Setup(p => p.ContentSettings).Returns(new ContentSettings());

            var settingsController = CreateSettingsController();
            var result = settingsController.Content();

            Assert.IsInstanceOf<ViewResult>(result);
        }

        [Test]
        public async Task Content_Post()
        {
            _mockBlogConfig.Setup(p => p.ContentSettings).Returns(new ContentSettings());
            ContentSettingsViewModel model = new() { WordFilterMode = "Block" };

            var settingsController = CreateSettingsController();
            var result = await settingsController.Content(model);

            Assert.IsInstanceOf<OkResult>(result);
            _mockBlogConfig.Verify(p => p.SaveAsync(It.IsAny<ContentSettings>()));
            _mockBlogAudit.Verify(p => p.AddAuditEntry(EventType.Settings, AuditEventId.SettingsSavedContent, It.IsAny<string>()));
        }

        [Test]
        public void Notification_Get()
        {
            _mockBlogConfig.Setup(p => p.NotificationSettings).Returns(new NotificationSettings());

            var settingsController = CreateSettingsController();
            var result = settingsController.Notification();

            Assert.IsInstanceOf<ViewResult>(result);
        }

        [Test]
        public async Task Notification_Post()
        {
            _mockBlogConfig.Setup(p => p.NotificationSettings).Returns(new NotificationSettings());
            var settingsController = CreateSettingsController();
            NotificationSettingsViewModel model = new();

            var result = await settingsController.Notification(model);

            Assert.IsInstanceOf<OkResult>(result);
            _mockBlogConfig.Verify(p => p.SaveAsync(It.IsAny<NotificationSettings>()));
            _mockBlogAudit.Verify(p => p.AddAuditEntry(EventType.Settings, AuditEventId.SettingsSavedNotification, It.IsAny<string>()));
        }

        [Test]
        public async Task SendTestEmail_Post()
        {
            var settingsController = CreateSettingsController();
            Mock<IBlogNotificationClient> notificationClientMock = new();

            var result = await settingsController.SendTestEmail(notificationClientMock.Object);
            Assert.IsInstanceOf<JsonResult>(result);
        }

        [Test]
        public void Subscription_Get()
        {
            _mockBlogConfig.Setup(p => p.FeedSettings).Returns(new FeedSettings());
            var settingsController = CreateSettingsController();

            var result = settingsController.Subscription();

            Assert.IsInstanceOf<ViewResult>(result);
        }

        [Test]
        public async Task Subscription_Post()
        {
            _mockBlogConfig.Setup(p => p.FeedSettings).Returns(new FeedSettings());
            var settingsController = CreateSettingsController();
            SubscriptionSettingsViewModel model = new();

            var result = await settingsController.Subscription(model);

            Assert.IsInstanceOf<OkResult>(result);
            _mockBlogConfig.Verify(p => p.SaveAsync(It.IsAny<FeedSettings>()));
            _mockBlogAudit.Verify(p => p.AddAuditEntry(EventType.Settings, AuditEventId.SettingsSavedSubscription, It.IsAny<string>()));
        }

        [Test]
        public void Watermark_Get()
        {
            _mockBlogConfig.Setup(p => p.WatermarkSettings).Returns(new WatermarkSettings());
            var settingsController = CreateSettingsController();

            var result = settingsController.Watermark();
            Assert.IsInstanceOf<ViewResult>(result);
        }

        //[Test]
        //public async Task Watermark_StateUnderTest_ExpectedBehavior1()
        //{
        //    // Arrange
        //    var settingsController = CreateSettingsController();
        //    WatermarkSettingsViewModel model = null;

        //    // Act
        //    var result = await settingsController.Watermark(
        //        model);

        //    // Assert
        //    Assert.Fail();
        //    _mockRepository.VerifyAll();
        //}

        //[Test]
        //public async Task FriendLink_StateUnderTest_ExpectedBehavior()
        //{
        //    // Arrange
        //    var settingsController = CreateSettingsController();
        //    FriendLinkSettingsViewModelWrap model = null;

        //    // Act
        //    var result = await settingsController.FriendLink(
        //        model);

        //    // Assert
        //    Assert.Fail();
        //    _mockRepository.VerifyAll();
        //}

        //[Test]
        //public async Task SetBloggerAvatar_StateUnderTest_ExpectedBehavior()
        //{
        //    // Arrange
        //    var settingsController = CreateSettingsController();
        //    string base64Img = null;

        //    // Act
        //    var result = await settingsController.SetBloggerAvatar(
        //        base64Img);

        //    // Assert
        //    Assert.Fail();
        //    _mockRepository.VerifyAll();
        //}

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

        //[Test]
        //public void Advanced_StateUnderTest_ExpectedBehavior()
        //{
        //    // Arrange
        //    var settingsController = CreateSettingsController();

        //    // Act
        //    var result = settingsController.Advanced();

        //    // Assert
        //    Assert.Fail();
        //    _mockRepository.VerifyAll();
        //}

        //[Test]
        //public async Task Advanced_StateUnderTest_ExpectedBehavior1()
        //{
        //    // Arrange
        //    var settingsController = CreateSettingsController();
        //    AdvancedSettingsViewModel model = null;

        //    // Act
        //    var result = await settingsController.Advanced(
        //        model);

        //    // Assert
        //    Assert.Fail();
        //    _mockRepository.VerifyAll();
        //}

        //[Test]
        //public void Shutdown_StateUnderTest_ExpectedBehavior()
        //{
        //    // Arrange
        //    var settingsController = CreateSettingsController();
        //    IHostApplicationLifetime applicationLifetime = null;

        //    // Act
        //    var result = settingsController.Shutdown(
        //        applicationLifetime);

        //    // Assert
        //    Assert.Fail();
        //    _mockRepository.VerifyAll();
        //}

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

        //[Test]
        //public void Security_StateUnderTest_ExpectedBehavior()
        //{
        //    // Arrange
        //    var settingsController = CreateSettingsController();

        //    // Act
        //    var result = settingsController.Security();

        //    // Assert
        //    Assert.Fail();
        //    _mockRepository.VerifyAll();
        //}

        //[Test]
        //public async Task Security_StateUnderTest_ExpectedBehavior1()
        //{
        //    // Arrange
        //    var settingsController = CreateSettingsController();
        //    SecuritySettingsViewModel model = null;

        //    // Act
        //    var result = await settingsController.Security(
        //        model);

        //    // Assert
        //    Assert.Fail();
        //    _mockRepository.VerifyAll();
        //}

        //[Test]
        //public void CustomStyleSheet_StateUnderTest_ExpectedBehavior()
        //{
        //    // Arrange
        //    var settingsController = CreateSettingsController();

        //    // Act
        //    var result = settingsController.CustomStyleSheet();

        //    // Assert
        //    Assert.Fail();
        //    _mockRepository.VerifyAll();
        //}

        //[Test]
        //public async Task CustomStyleSheet_StateUnderTest_ExpectedBehavior1()
        //{
        //    // Arrange
        //    var settingsController = CreateSettingsController();
        //    CustomStyleSheetSettingsViewModel model = null;

        //    // Act
        //    var result = await settingsController.CustomStyleSheet(
        //        model);

        //    // Assert
        //    Assert.Fail();
        //    _mockRepository.VerifyAll();
        //}

        //[Test]
        //public void DataPorting_StateUnderTest_ExpectedBehavior()
        //{
        //    // Arrange
        //    var settingsController = CreateSettingsController();

        //    // Act
        //    var result = settingsController.DataPorting();

        //    // Assert
        //    Assert.Fail();
        //    _mockRepository.VerifyAll();
        //}

        //[Test]
        //public async Task ExportDownload_StateUnderTest_ExpectedBehavior()
        //{
        //    // Arrange
        //    var settingsController = CreateSettingsController();
        //    IExportManager expman = null;
        //    ExportDataType type = default(ExportDataType);

        //    // Act
        //    var result = await settingsController.ExportDownload(
        //        expman,
        //        type);

        //    // Assert
        //    Assert.Fail();
        //    _mockRepository.VerifyAll();
        //}

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
}
