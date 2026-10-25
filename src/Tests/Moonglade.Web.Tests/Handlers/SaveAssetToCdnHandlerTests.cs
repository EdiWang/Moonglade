using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moonglade.Configuration;
using Moonglade.Features.Asset;
using Moonglade.ImageStorage;
using Moonglade.Web.Handlers;
using Moq;
using Moonglade.Web.Tests.TestSupport;

namespace Moonglade.Web.Tests.Handlers;

public class SaveAssetToCdnHandlerTests : IDisposable
{
    private const string AvatarFileName = "avatar-0922e4.png";

    private readonly Mock<ILogger<SaveAssetToCdnHandler>> _logger = new();
    private readonly Mock<IBlogImageStorage> _imageStorage = new();
    private readonly ConfigurationCommandRecorder _commandMediator = new();

    [Fact]
    public async Task HandleAsync_WhenAssetIsNotAvatar_DoesNothing()
    {
        var handler = CreateHandler();
        var request = new SaveAssetEvent(Guid.NewGuid(), Convert.ToBase64String([1, 2, 3]));

        await handler.HandleAsync(request, TestContext.Current.CancellationToken);

        _imageStorage.Verify(x => x.DeleteAsync(It.IsAny<string>()), Times.Never);
        _imageStorage.Verify(x => x.InsertAsync(It.IsAny<string>(), It.IsAny<byte[]>()), Times.Never);
        Assert.Empty(_commandMediator.Commands);
    }

    [Fact]
    public async Task HandleAsync_WhenCdnRedirectIsDisabled_DoesNothing()
    {
        var blogConfig = CreateBlogConfig(enableCdnRedirect: false);
        var handler = CreateHandler(blogConfig);
        var request = new SaveAssetEvent(AssetId.AvatarBase64, Convert.ToBase64String([1, 2, 3]));

        await handler.HandleAsync(request, TestContext.Current.CancellationToken);

        _imageStorage.Verify(x => x.DeleteAsync(It.IsAny<string>()), Times.Never);
        _imageStorage.Verify(x => x.InsertAsync(It.IsAny<string>(), It.IsAny<byte[]>()), Times.Never);
        Assert.Empty(_commandMediator.Commands);
    }

    [Fact]
    public async Task HandleAsync_WhenAssetBase64IsEmpty_DoesNothing()
    {
        var handler = CreateHandler();
        var request = new SaveAssetEvent(AssetId.AvatarBase64, string.Empty);

        await handler.HandleAsync(request, TestContext.Current.CancellationToken);

        _imageStorage.Verify(x => x.DeleteAsync(It.IsAny<string>()), Times.Never);
        _imageStorage.Verify(x => x.InsertAsync(It.IsAny<string>(), It.IsAny<byte[]>()), Times.Never);
        Assert.Empty(_commandMediator.Commands);
    }

    [Fact]
    public async Task HandleAsync_WhenAvatarIsSaved_UpdatesAvatarUrlAndPersistsConfiguration()
    {
        var blogConfig = CreateBlogConfig();
        var handler = CreateHandler(blogConfig);
        var imageBytes = new byte[] { 1, 2, 3, 4 };
        var request = new SaveAssetEvent(AssetId.AvatarBase64, Convert.ToBase64String(imageBytes));

        _imageStorage
            .Setup(x => x.InsertAsync(AvatarFileName, It.IsAny<byte[]>()))
            .ReturnsAsync("avatar-cdn.png");

        await handler.HandleAsync(request, TestContext.Current.CancellationToken);

        _imageStorage.Verify(x => x.DeleteAsync(AvatarFileName), Times.Once);
        _imageStorage.Verify(x => x.InsertAsync(AvatarFileName, It.Is<byte[]>(bytes => bytes.SequenceEqual(imageBytes))), Times.Once);
        _imageStorage.Verify(x => x.InsertOriginalAsync(It.IsAny<string>(), It.IsAny<byte[]>()), Times.Never);

        var avatarUrl = blogConfig.GeneralSettings.AvatarUrl;
        Assert.NotNull(avatarUrl);
        Assert.StartsWith("https://cdn.example.com/images/avatar-cdn.png?", avatarUrl);

        var cacheBuster = avatarUrl.Split('?')[1];
        Assert.InRange(int.Parse(cacheBuster), 100, 999);

        var command = _commandMediator.Single<UpdateConfigurationCommand>();
        Assert.Single(command.Settings);

        var persistedSettings = command.Settings[nameof(GeneralSettings)].FromJson<GeneralSettings>();
        Assert.NotNull(persistedSettings);
        Assert.Equal(avatarUrl, persistedSettings!.AvatarUrl);
    }

    [Fact]
    public async Task HandleAsync_WhenConfigurationSaveFails_PreservesRuntimeAvatarUrl()
    {
        var config = CreateBlogConfig();
        config.GeneralSettings.AvatarUrl = "old-avatar.png";
        var original = config.GeneralSettings;
        var handler = CreateHandler(config);
        _imageStorage.Setup(p => p.InsertAsync(AvatarFileName, It.IsAny<byte[]>())).ReturnsAsync("new-avatar.png");
        await _commandMediator.Db.Database.ExecuteSqlRawAsync(
            "CREATE TRIGGER reject_avatar_settings BEFORE UPDATE ON BlogConfiguration BEGIN SELECT RAISE(ABORT, 'Save rejected'); END;",
            TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<DbUpdateException>(() => handler.HandleAsync(
            new SaveAssetEvent(AssetId.AvatarBase64, Convert.ToBase64String([1, 2, 3])), TestContext.Current.CancellationToken));

        Assert.Same(original, config.GeneralSettings);
        Assert.Equal("old-avatar.png", config.GeneralSettings.AvatarUrl);
        _commandMediator.Db.ChangeTracker.Clear();
        Assert.Equal(original.ToJson(), (await _commandMediator.Db.BlogConfiguration.SingleAsync(
            p => p.CfgKey == nameof(GeneralSettings), TestContext.Current.CancellationToken)).CfgValue);
    }

    private SaveAssetToCdnHandler CreateHandler(BlogConfig? blogConfig = null)
    {
        blogConfig ??= CreateBlogConfig();
        _commandMediator.Initialize(blogConfig);
        return new SaveAssetToCdnHandler(
            _logger.Object,
            _imageStorage.Object,
            blogConfig,
            _commandMediator);
    }

    private static BlogConfig CreateBlogConfig(bool enableCdnRedirect = true)
    {
        return new BlogConfig
        {
            GeneralSettings = GeneralSettings.DefaultValue,
            ImageSettings = new ImageSettings
            {
                EnableCDNRedirect = enableCdnRedirect,
                CDNEndpoint = "https://cdn.example.com/images"
            }
        };
    }

    public void Dispose() => _commandMediator.Dispose();
}
