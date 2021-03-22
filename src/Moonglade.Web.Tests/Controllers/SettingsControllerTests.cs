using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moonglade.Auditing;
using Moonglade.Configuration;
using Moonglade.Configuration.Abstraction;
using Moonglade.DataPorting;
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

        private Mock<IBlogConfig> _mockBlogConfig;
        private Mock<IBlogAudit> _mockBlogAudit;
        private Mock<ILogger<SettingsController>> _mockLogger;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);

            _mockBlogConfig = _mockRepository.Create<IBlogConfig>();
            _mockBlogAudit = _mockRepository.Create<IBlogAudit>();
            _mockLogger = _mockRepository.Create<ILogger<SettingsController>>();
        }

        private SettingsController CreateSettingsController()
        {
            return new(
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
        public async Task Content_Post()
        {
            _mockBlogConfig.Setup(p => p.ContentSettings).Returns(new ContentSettings());
            ContentSettingsViewModel model = new() { WordFilterMode = "Block" };

            var settingsController = CreateSettingsController();
            var result = await settingsController.Content(new(model));

            Assert.IsInstanceOf<OkResult>(result);
            _mockBlogConfig.Verify(p => p.SaveAsync(It.IsAny<ContentSettings>()));
            _mockBlogAudit.Verify(p => p.AddAuditEntry(EventType.Settings, AuditEventId.SettingsSavedContent, It.IsAny<string>()));
        }

        [Test]
        public async Task Notification_Post()
        {
            _mockBlogConfig.Setup(p => p.NotificationSettings).Returns(new NotificationSettings());
            var settingsController = CreateSettingsController();
            NotificationSettingsViewModel model = new();

            var result = await settingsController.Notification(new(model));

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
        public async Task Subscription_Post()
        {
            _mockBlogConfig.Setup(p => p.FeedSettings).Returns(new FeedSettings());
            var settingsController = CreateSettingsController();
            SubscriptionSettingsViewModel model = new();

            var result = await settingsController.Subscription(new(model));

            Assert.IsInstanceOf<OkResult>(result);
            _mockBlogConfig.Verify(p => p.SaveAsync(It.IsAny<FeedSettings>()));
            _mockBlogAudit.Verify(p => p.AddAuditEntry(EventType.Settings, AuditEventId.SettingsSavedSubscription, It.IsAny<string>()));
        }

        [Test]
        public async Task Watermark_Post()
        {
            _mockBlogConfig.Setup(p => p.WatermarkSettings).Returns(new WatermarkSettings());
            var settingsController = CreateSettingsController();
            WatermarkSettingsViewModel model = new();

            var result = await settingsController.Watermark(new(model));

            Assert.IsInstanceOf<OkResult>(result);
            _mockBlogConfig.Verify(p => p.SaveAsync(It.IsAny<WatermarkSettings>()));
            _mockBlogAudit.Verify(p => p.AddAuditEntry(EventType.Settings, AuditEventId.SettingsSavedWatermark, It.IsAny<string>()));
        }

        [Test]
        public async Task FriendLink_Post()
        {
            _mockBlogConfig.Setup(p => p.FriendLinksSettings).Returns(new FriendLinksSettings());
            var settingsController = CreateSettingsController();
            FriendLinksSettings model = new();

            var result = await settingsController.FriendLink(model);

            Assert.IsInstanceOf<OkResult>(result);
            _mockBlogConfig.Verify(p => p.SaveAsync(It.IsAny<FriendLinksSettings>()));
        }

        [Test]
        public async Task SetBloggerAvatar_Post_BadData()
        {
            var settingsController = CreateSettingsController();
            string base64Img = "996.icu";

            var result = await settingsController.SetBloggerAvatar(base64Img);
            Assert.IsInstanceOf<ConflictObjectResult>(result);
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
            AdvancedSettingsViewModel model = new();

            var result = await settingsController.Advanced(new(model));

            Assert.IsInstanceOf<OkResult>(result);
            _mockBlogConfig.Verify(p => p.SaveAsync(It.IsAny<AdvancedSettings>()));
            _mockBlogAudit.Verify(p => p.AddAuditEntry(EventType.Settings, AuditEventId.SettingsSavedAdvanced, It.IsAny<string>()));
        }

        [Test]
        public void Advanced_Post_EnableCDNRedirect_EmptyCDNEndpoint()
        {
            _mockBlogConfig.Setup(p => p.AdvancedSettings).Returns(new AdvancedSettings());
            var settingsController = CreateSettingsController();
            AdvancedSettingsViewModel model = new() { EnableCDNRedirect = true, CDNEndpoint = string.Empty };

            Assert.ThrowsAsync<ArgumentNullException>(async () => { await settingsController.Advanced(new(model)); });
        }

        [Test]
        public void Advanced_Post_EnableCDNRedirect_InvalidCDNEndpoint()
        {
            _mockBlogConfig.Setup(p => p.AdvancedSettings).Returns(new AdvancedSettings());
            var settingsController = CreateSettingsController();
            AdvancedSettingsViewModel model = new() { EnableCDNRedirect = true, CDNEndpoint = "996.icu" };

            Assert.ThrowsAsync<UriFormatException>(async () => { await settingsController.Advanced(new(model)); });
        }

        [Test]
        public async Task Advanced_Post_EnableCDNRedirect_ValidCDNEndpoint()
        {
            _mockBlogConfig.Setup(p => p.AdvancedSettings).Returns(new AdvancedSettings());
            var settingsController = CreateSettingsController();
            AdvancedSettingsViewModel model = new() { EnableCDNRedirect = true, CDNEndpoint = "https://cdn.996.icu/fubao" };

            var result = await settingsController.Advanced(new(model));

            Assert.IsInstanceOf<OkResult>(result);
            _mockBlogConfig.Verify(p => p.SaveAsync(It.IsAny<AdvancedSettings>()));
            _mockBlogAudit.Verify(p => p.AddAuditEntry(EventType.Settings, AuditEventId.SettingsSavedAdvanced, It.IsAny<string>()));
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
        public async Task Security_Post()
        {
            _mockBlogConfig.Setup(p => p.SecuritySettings).Returns(new SecuritySettings());

            var settingsController = CreateSettingsController();
            SecuritySettingsViewModel model = new();

            var result = await settingsController.Security(new(model));

            Assert.IsInstanceOf<OkResult>(result);
            _mockBlogConfig.Verify(p => p.SaveAsync(It.IsAny<SecuritySettings>()));
            _mockBlogAudit.Verify(p => p.AddAuditEntry(EventType.Settings, AuditEventId.SettingsSavedAdvanced, It.IsAny<string>()));
        }

        [Test]
        public async Task CustomStyleSheet_Post_Enabled_EmptyCSS()
        {
            _mockBlogConfig.Setup(p => p.CustomStyleSheetSettings).Returns(new CustomStyleSheetSettings());

            var settingsController = CreateSettingsController();
            CustomStyleSheetSettingsViewModel model = new()
            {
                EnableCustomCss = true,
                CssCode = string.Empty
            };

            var result = await settingsController.CustomStyleSheet(new(model));

            Assert.IsInstanceOf<BadRequestObjectResult>(result);
            _mockBlogConfig.Verify(p => p.SaveAsync(It.IsAny<CustomStyleSheetSettings>()), Times.Never);
            _mockBlogAudit.Verify(p => p.AddAuditEntry(EventType.Settings, AuditEventId.SettingsSavedAdvanced, It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task CustomStyleSheet_Post_Enabled_BadCSS()
        {
            _mockBlogConfig.Setup(p => p.CustomStyleSheetSettings).Returns(new CustomStyleSheetSettings());

            var settingsController = CreateSettingsController();
            CustomStyleSheetSettingsViewModel model = new()
            {
                EnableCustomCss = true,
                CssCode = ".996-{icu}"
            };

            var result = await settingsController.CustomStyleSheet(new(model));

            Assert.IsInstanceOf<BadRequestObjectResult>(result);
            _mockBlogConfig.Verify(p => p.SaveAsync(It.IsAny<CustomStyleSheetSettings>()), Times.Never);
            _mockBlogAudit.Verify(p => p.AddAuditEntry(EventType.Settings, AuditEventId.SettingsSavedAdvanced, It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task CustomStyleSheet_Post_OK()
        {
            _mockBlogConfig.Setup(p => p.CustomStyleSheetSettings).Returns(new CustomStyleSheetSettings());

            var settingsController = CreateSettingsController();
            CustomStyleSheetSettingsViewModel model = new()
            {
                EnableCustomCss = true,
                CssCode = ".icu { color: #996; }"
            };

            var result = await settingsController.CustomStyleSheet(new(model));

            Assert.IsInstanceOf<OkResult>(result);
            _mockBlogConfig.Verify(p => p.SaveAsync(It.IsAny<CustomStyleSheetSettings>()));
            _mockBlogAudit.Verify(p => p.AddAuditEntry(EventType.Settings, AuditEventId.SettingsSavedAdvanced, It.IsAny<string>()));
        }

        [Test]
        public async Task ExportDownload_SingleJsonFile()
        {
            Mock<IExportManager> mockExpman = new();
            mockExpman.Setup(p => p.ExportData(ExportDataType.Tags))
                .Returns(Task.FromResult(new ExportResult
                {
                    ExportFormat = ExportFormat.SingleJsonFile,
                    Content = Array.Empty<byte>()
                }));

            var settingsController = CreateSettingsController();
            ExportDataType type = ExportDataType.Tags;

            var result = await settingsController.ExportDownload(mockExpman.Object, type);
            Assert.IsInstanceOf<FileContentResult>(result);
        }

        [Test]
        public async Task ExportDownload_SingleCSVFile()
        {
            Mock<IExportManager> mockExpman = new();
            mockExpman.Setup(p => p.ExportData(ExportDataType.Categories))
                .Returns(Task.FromResult(new ExportResult
                {
                    ExportFormat = ExportFormat.SingleCSVFile,
                    FilePath = @"C:\996\icu.csv"
                }));

            var settingsController = CreateSettingsController();
            settingsController.ControllerContext = new()
            {
                HttpContext = new DefaultHttpContext()
            };

            ExportDataType type = ExportDataType.Categories;

            var result = await settingsController.ExportDownload(mockExpman.Object, type);
            Assert.IsInstanceOf<PhysicalFileResult>(result);
        }

        [Test]
        public async Task ExportDownload_ZippedJsonFiles()
        {
            Mock<IExportManager> mockExpman = new();
            mockExpman.Setup(p => p.ExportData(ExportDataType.Posts))
                .Returns(Task.FromResult(new ExportResult
                {
                    ExportFormat = ExportFormat.ZippedJsonFiles,
                    FilePath = @"C:\996\icu.zip"
                }));

            var settingsController = CreateSettingsController();
            settingsController.ControllerContext = new()
            {
                HttpContext = new DefaultHttpContext()
            };

            ExportDataType type = ExportDataType.Posts;

            var result = await settingsController.ExportDownload(mockExpman.Object, type);
            Assert.IsInstanceOf<PhysicalFileResult>(result);
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
}
